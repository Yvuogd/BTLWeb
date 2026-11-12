"""Run against a disposable database seeded with ten novels and three enabled demo accounts."""
import html
import http.cookiejar
import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

base = sys.argv[1].rstrip("/")


def client():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))


def request(browser, path, form=None, headers=None, status=200):
    if isinstance(form, dict):
        form = urllib.parse.urlencode(form).encode()
    try:
        response = browser.open(urllib.request.Request(base + path, form, headers or {}), timeout=20)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        body = response.read().decode()
        assert response.code == status, (path, response.code, status, body[:200])
        return body


def token(page):
    return html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)[1])


def post(browser, path, page, **fields):
    return request(browser, path, {**fields, "__RequestVerificationToken": token(page)})


def sign_in(name):
    browser = client()
    login = request(browser, "/Account/Login")
    landing = post(browser, "/Account/Login", login, username=name,
                   password=os.environ["DemoAccounts__" + name])
    assert "Đăng xuất" in landing
    return browser


def upload(browser, fields, filename, content):
    boundary = "NovelPlatformSmokeTest"
    parts = []
    for key, value in fields.items():
        parts.append((f'--{boundary}\r\nContent-Disposition: form-data; name="{key}"\r\n\r\n'
                      f'{value}\r\n').encode())
    parts.append((f'--{boundary}\r\nContent-Disposition: form-data; name="Cover"; filename="{filename}"\r\n'
                  'Content-Type: image/jpeg\r\n\r\n').encode())
    parts.extend((content, f"\r\n--{boundary}--\r\n".encode()))
    return request(browser, "/Studio/Create", b"".join(parts),
                   {"Content-Type": f"multipart/form-data; boundary={boundary}"})


guest = client()
first = json.loads(request(guest, "/api/novels"))
second = json.loads(request(guest, "/api/novels?page=2"))
assert first["total"] == 10 and first["pageSize"] == 6
assert len(first["items"]) == 6 and len(second["items"]) == 4
assert not ({item["id"] for item in first["items"]} & {item["id"] for item in second["items"]})
assert json.loads(request(guest, "/api/novels?page=9999"))["page"] == 2
filtered = json.loads(request(guest, "/api/novels?" + urllib.parse.urlencode({"genre": "Hệ Thống"})))
assert filtered["total"] == 1
assert "/api/novels" in json.loads(request(guest, "/swagger/v1/swagger.json"))["paths"]
assert request(guest, "/").count('class="tc-update-row"') == 6
ajax = request(guest, "/?page=2", headers={"X-Requested-With": "XMLHttpRequest"})
assert ajax.count('class="tc-update-row"') == 4 and "<html" not in ajax
print("PASS seed count, filtering, paging, AJAX and Swagger")

author = sign_in("tacgia")
admin = sign_in("admin")
reader = sign_in("docgia")
assert "Sửa truyện" not in request(reader, "/Studio")
create = request(author, "/Studio/Create")
category_id = re.search(r'<option value="(\d+)"', create)[1]
fields = {"__RequestVerificationToken": token(create), "Title": "Smoke test novel",
          "CategoryId": category_id, "Status": "Ongoing"}
assert "Chọn ảnh JPG" in html.unescape(upload(author, fields, "fake.jpg", b"not a valid image"))
assert "Ảnh phải có dung lượng" in html.unescape(upload(author, fields, "large.jpg", b"0" * (3 * 1024 * 1024 + 1)))
cover = Path(__file__).resolve().parent / "wwwroot/images/cover-dai-ha.jpg"
studio = upload(author, fields, "sample.jpg", cover.read_bytes())
novel_id = int(re.search(r'/Studio/Edit/(\d+)', studio)[1])
request(guest, f"/api/novels/{novel_id}", status=404)
request(author, f"/Studio/Delete/{novel_id}", {}, status=400)
print("PASS role restrictions, CSRF, cover validation and author moderation")

edit = request(author, f"/Studio/Edit/{novel_id}")
post(author, f"/Studio/Edit/{novel_id}", edit, Title="Smoke test novel updated",
     CategoryId=category_id, Status="Completed")
add = request(author, f"/Studio/AddChapter/{novel_id}")
studio = post(author, f"/Studio/AddChapter/{novel_id}", add,
              Title="Smoke chapter", Number=1, Content="Demo")
chapter_id = int(re.search(r'/Studio/EditChapter/(\d+)', studio)[1])
chapter = request(author, f"/Studio/EditChapter/{chapter_id}")
post(author, f"/Studio/EditChapter/{chapter_id}", chapter,
     Title="Updated chapter", Number=1, Content="Edited demo")
admin_page = request(admin, "/Admin")
post(admin, "/Admin/ReviewNovel", admin_page, id=novel_id, decision="approve")
post(admin, "/Admin/ReviewChapter", admin_page, id=chapter_id, decision="approve")
summary = json.loads(request(guest, f"/api/novels/{novel_id}"))
assert summary["title"] == "Smoke test novel updated"
assert summary["coverImage"].startswith("/uploads/covers/")
chapters_url = f"/api/novels/{novel_id}/chapters"
assert json.loads(request(guest, chapters_url))[0]["title"] == "Updated chapter"
post(author, f"/Studio/DeleteChapter/{chapter_id}", studio)
assert json.loads(request(guest, chapters_url)) == []
post(author, f"/Studio/Delete/{novel_id}", studio)
request(guest, f"/api/novels/{novel_id}", status=404)
print("PASS create, edit, review and delete novels and chapters")

admin_page = request(admin, "/Admin")
account = re.search(r'<small>tacgia · Author</small>.*?<input[^>]*name="id"[^>]*value="(\d+)"',
                    admin_page, flags=re.S)
assert account
for _ in range(2):
    post(admin, "/Admin/ToggleUser", admin_page, id=account[1])
assert "Studio tác giả" not in request(author, "/Studio")
print("PASS session revoked even after account is unlocked")
