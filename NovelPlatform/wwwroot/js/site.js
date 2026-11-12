const catalog = document.querySelector("#novel-catalog");
const categoryFilter = document.querySelector("[data-catalog-filter]");
let activeCatalogRequest;

async function loadCatalog(url) {
    activeCatalogRequest?.abort();
    const request = new AbortController();
    activeCatalogRequest = request;
    catalog.setAttribute("aria-busy", "true");

    try {
        const response = await fetch(url, {
            headers: { "X-Requested-With": "XMLHttpRequest" },
            signal: request.signal
        });
        if (!response.ok) {
            throw new Error("Catalog request failed");
        }

        const content = await response.text();
        if (!request.signal.aborted) {
            catalog.innerHTML = content;
            history.pushState(null, "", url);
        }
    } catch (error) {
        if (!request.signal.aborted) {
            window.location.assign(url);
        }
    } finally {
        if (activeCatalogRequest === request) {
            catalog.removeAttribute("aria-busy");
        }
    }
}

categoryFilter?.addEventListener("submit", event => {
    event.preventDefault();
    const url = new URL(categoryFilter.action);
    url.search = new URLSearchParams(new FormData(categoryFilter)).toString();
    loadCatalog(url);
});

catalog?.addEventListener("click", event => {
    const link = event.target.closest("[data-catalog-page]");
    if (!link || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
        return;
    }

    event.preventDefault();
    loadCatalog(link.href);
});

if (catalog) {
    window.addEventListener("popstate", () => window.location.reload());
}

document.addEventListener("submit", event => {
    const message = event.target.dataset.confirm;
    if (message && !window.confirm(message)) {
        event.preventDefault();
    }
});
