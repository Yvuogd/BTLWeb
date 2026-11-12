// Run with Node.js 22+ and an Edge/Chromium debugging endpoint.
import assert from "node:assert/strict";
import { setTimeout as delay } from "node:timers/promises";

const [app, debuggerUrl] = process.argv.slice(2);
const targets = await (await fetch(debuggerUrl + "/json/list")).json();
const socket = new WebSocket(targets.find(target => target.type === "page").webSocketDebuggerUrl);
await new Promise((resolve, reject) => {
    socket.addEventListener("open", resolve, { once: true });
    socket.addEventListener("error", reject, { once: true });
});

let messageId = 0;
const requests = new Map();
socket.addEventListener("message", event => {
    const response = JSON.parse(event.data);
    const request = requests.get(response.id);
    if (request) {
        requests.delete(response.id);
        if (response.error) request.reject(new Error(response.error.message));
        else request.resolve(response.result);
    }
});

function send(method, params = {}) {
    return new Promise((resolve, reject) => {
        const id = ++messageId;
        requests.set(id, { resolve, reject });
        socket.send(JSON.stringify({ id, method, params }));
    });
}

async function evaluate(expression) {
    const result = await send("Runtime.evaluate", { expression, awaitPromise: true, returnByValue: true });
    if (result.exceptionDetails) {
        throw new Error(result.exceptionDetails.exception?.description ?? result.exceptionDetails.text);
    }
    return result.result.value;
}

async function until(expression) {
    for (let retry = 0; retry < 60; retry++) {
        if (await evaluate(expression).catch(() => false)) return;
        await delay(100);
    }
    throw new Error("Timed out: " + expression);
}

async function navigate(path) {
    await send("Page.navigate", { url: app + path });
    await until(`document.readyState === "complete" && location.pathname === ${JSON.stringify(path)}`);
}

async function signIn(username) {
    await navigate("/Account/Login");
    const password = process.env["DemoAccounts__" + username];
    assert(password);
    await evaluate(`(async () => {
        const form = document.querySelector('form[action="/Account/Login"]');
        const fields = new URLSearchParams(new FormData(form));
        fields.set("username", ${JSON.stringify(username)});
        fields.set("password", ${JSON.stringify(password)});
        await fetch(form.action, { method: "POST", body: fields });
    })()`);
}

async function checkWidth(page, width) {
    await navigate(page);
    const layout = await evaluate("({ viewport: innerWidth, document: document.documentElement.scrollWidth })");
    assert.equal(layout.viewport, width, page);
    assert(layout.document <= width + 1, `${page} overflows at ${width}px: ${layout.document}px`);
}

try {
    await navigate("/");
    const id = await evaluate("fetch('/api/novels').then(response => response.json()).then(page => page.items[0].id)");
    const chapterId = await evaluate(`fetch('/api/novels/${id}/chapters').then(response => response.json()).then(chapters => chapters[0].id)`);
    for (const width of [1280, 390, 320]) {
        await send("Emulation.setDeviceMetricsOverride", {
            width, height: 850, deviceScaleFactor: 1, mobile: width < 500
        });
        for (const path of ["/", "/Home/Details/" + id, "/Home/Read/" + chapterId, "/Account/Register"]) {
            await checkWidth(path, width);
        }
    }

    await navigate("/");
    await evaluate("window.catalogDocument = document; document.querySelector('[data-catalog-page]').click()");
    await until("location.search.includes('page=2') && document.querySelectorAll('.tc-update-row').length === 4");
    assert(await evaluate("window.catalogDocument === document"));
    await evaluate(`document.querySelector("#catalog-query").value = "Đại Hạ"; document.querySelector("[data-catalog-filter]").requestSubmit()`);
    await until("document.querySelectorAll('.tc-update-row').length === 1");
    assert(await evaluate("window.catalogDocument === document"));

    await signIn("tacgia");
    for (const width of [1280, 390, 320]) {
        await send("Emulation.setDeviceMetricsOverride", { width, height: 850, deviceScaleFactor: 1, mobile: width < 500 });
        for (const path of ["/Studio", "/Studio/Create", "/Library"]) await checkWidth(path, width);
    }

    await signIn("admin");
    for (const width of [1280, 390, 320]) {
        await send("Emulation.setDeviceMetricsOverride", { width, height: 850, deviceScaleFactor: 1, mobile: width < 500 });
        await checkWidth("/Admin", width);
    }
    console.log("PASS browser AJAX and responsive layout at 1280, 390 and 320 px");
} finally {
    socket.close();
}
