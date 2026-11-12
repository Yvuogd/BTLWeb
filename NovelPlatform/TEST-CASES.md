# NovelPlatform test cases

Run against a disposable SQL Server database. The recorded run used SQL Server Express, .NET 10, and Microsoft Edge. The HTTP checks are in [smoke_test.py](smoke_test.py). The browser checks are in [responsive_test.mjs](responsive_test.mjs).

| ID | Objective and input | Expected result | Actual result | Status |
|---|---|---|---|---|
| TC01 | Run the SQL setup script on an empty database and start the app. | Ten published novels, including six added titles. | Ten titles returned by the API and six new titles verified by SQL. | Pass |
| TC02 | Open the catalog and request pages 1, 2 and 9999. | Six items on page 1 and four on page 2, with no overlap. Page 9999 clamps to page 2. | All counts, page boundaries and uniqueness checks passed. | Pass |
| TC03 | Search for Đại Hạ and filter by Hệ Thống. Use the next-page control. | One result for each filter. Pagination and search update the catalog without a full reload. | Filter returned one item. Edge preserved the same document while updating results. | Pass |
| TC04 | Open Swagger JSON and request a novel that is awaiting approval. | OpenAPI describes the REST routes. The pending novel returns HTTP 404. | Route present in Swagger and pending novel returned 404. | Pass |
| TC05 | Sign in as Reader and request Studio. Send a delete POST as Author without an anti-forgery token. | Reader cannot use Studio. The POST without a token returns HTTP 400. | Studio controls were unavailable to Reader and POST returned HTTP 400. | Pass |
| TC06 | Submit a valid JPG, a text file labeled JPG, and a file one byte above the 3 MB limit. | Valid image is stored and linked. Invalid and oversized files produce validation errors. | Upload URL returned for valid JPG. Both invalid files were rejected. | Pass |
| TC07 | Create and edit a novel and chapter as Author, approve both as Admin, then delete the chapter and novel as Author. | Approved data is visible in the REST API. Chapter list becomes empty after deletion, and the deleted novel returns 404. | Updated content was visible after approval. Both deletion checks passed. | Pass |
| TC08 | Lock and immediately unlock an Author account while its old session is still open. | The old session can no longer access Studio. | The old cookie lost access after the account was unlocked. | Pass |
| TC09 | Check Home, Details, Read, Register, Studio, Create, Library and Admin at widths 1280, 390 and 320 px. | Main screens fit within the viewport without horizontal page overflow. | Edge measured document widths within the viewport at all three widths. | Pass |
| TC10 | Temporarily rename the Novels table in a disposable database, then request Home and the catalog API. | Home returns a generic HTML error and the API returns a JSON problem. Both use HTTP 500 and log the database exception. | Both returned HTTP 500 with the expected format, and the exception was logged. | Pass |

Set `ConnectionStrings__NovelDb` to a disposable database and supply `DemoAccounts__docgia`, `DemoAccounts__tacgia` and `DemoAccounts__admin` in the environment. Start the web app, then run:

```text
python NovelPlatform/smoke_test.py http://localhost:5297
node NovelPlatform/responsive_test.mjs http://localhost:5297 http://localhost:9222
```

The browser test requires Node.js 22+ and Edge or Chromium running with a dedicated temporary profile and remote debugging enabled on the specified port. Use the URL shown by the app if its configured port differs.
