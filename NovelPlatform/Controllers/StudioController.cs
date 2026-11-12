using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NovelPlatform.Data;
using NovelPlatform.Models;

namespace NovelPlatform.Controllers;

[Authorize(Roles = "Author,Admin")]
public class StudioController(AppDbContext db, IWebHostEnvironment environment,
    ILogger<StudioController> logger) : Controller
{
    private const int MaxCoverBytes = 3 * 1024 * 1024;

    private int UserId => int.Parse(User.FindFirst("uid")!.Value);
    private bool IsAdmin => User.IsInRole("Admin");
    private IQueryable<Novel> MyNovels => db.Novels.Where(novel => novel.AuthorId == UserId || IsAdmin);

    public async Task<IActionResult> Index()
    {
        return View(await MyNovels.Include(novel => novel.Category).Include(novel => novel.Chapters)
            .OrderByDescending(novel => novel.CreatedAt).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create() => await NovelForm(new NovelInput());

    [HttpPost]
    [RequestSizeLimit(MaxCoverBytes + 65536)]
    public Task<IActionResult> Create(NovelInput input) => SaveNovel(new Novel { AuthorId = UserId }, input);

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var novel = await MyNovels.SingleOrDefaultAsync(item => item.Id == id);
        if (novel is null) return NotFound();

        return await NovelForm(new NovelInput
        {
            Title = novel.Title,
            Description = novel.Description,
            CategoryId = novel.CategoryId,
            Status = novel.Status
        }, novel);
    }

    [HttpPost]
    [RequestSizeLimit(MaxCoverBytes + 65536)]
    public async Task<IActionResult> Edit(int id, NovelInput input)
    {
        var novel = await MyNovels.SingleOrDefaultAsync(item => item.Id == id);
        return novel is null ? NotFound() : await SaveNovel(novel, input);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var novel = await MyNovels.SingleOrDefaultAsync(item => item.Id == id);
        if (novel is null) return NotFound();

        db.Novels.Remove(novel);
        await db.SaveChangesAsync();
        DeleteUploadedCover(novel.CoverImage);
        logger.LogInformation("User {UserId} deleted novel {NovelId}", UserId, id);
        TempData["StudioMessage"] = "Đã xóa truyện.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> AddChapter(int id)
    {
        var novel = await MyNovels.SingleOrDefaultAsync(item => item.Id == id);
        if (novel is null) return NotFound();

        var lastNumber = await db.Chapters.Where(chapter => chapter.NovelId == id)
            .MaxAsync(chapter => (int?)chapter.Number);
        return ChapterForm(novel, new ChapterInput { Number = (lastNumber ?? 0) + 1 });
    }

    [HttpPost]
    public async Task<IActionResult> AddChapter(int id, ChapterInput input)
    {
        var novel = await MyNovels.SingleOrDefaultAsync(item => item.Id == id);
        return novel is null ? NotFound() : await SaveChapter(novel, new Chapter { NovelId = id }, input);
    }

    [HttpGet]
    public async Task<IActionResult> EditChapter(int id)
    {
        var chapter = await FindChapter(id);
        if (chapter is null) return NotFound();

        return ChapterForm(chapter.Novel, new ChapterInput
        {
            Title = chapter.Title,
            Number = chapter.Number,
            Content = chapter.Content
        }, id);
    }

    [HttpPost]
    public async Task<IActionResult> EditChapter(int id, ChapterInput input)
    {
        var chapter = await FindChapter(id);
        return chapter is null ? NotFound() : await SaveChapter(chapter.Novel, chapter, input);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteChapter(int id)
    {
        var chapter = await FindChapter(id);
        if (chapter is null) return NotFound();

        db.Chapters.Remove(chapter);
        await db.SaveChangesAsync();
        logger.LogInformation("User {UserId} deleted chapter {ChapterId}", UserId, id);
        TempData["StudioMessage"] = "Đã xóa chương.";
        return RedirectToAction(nameof(Index));
    }

    private Task<Chapter?> FindChapter(int id) => db.Chapters.Include(chapter => chapter.Novel)
        .Where(chapter => MyNovels.Any(novel => novel.Id == chapter.NovelId))
        .SingleOrDefaultAsync(chapter => chapter.Id == id);

    private async Task<IActionResult> SaveNovel(Novel novel, NovelInput input)
    {
        var isNew = novel.Id == 0;
        var oldCover = novel.CoverImage;

        if (!await db.Categories.AnyAsync(category => category.Id == input.CategoryId))
        {
            ModelState.AddModelError(nameof(input.CategoryId), "Thể loại không hợp lệ.");
        }

        var extension = input.Cover is null ? null : await ValidateCover(input.Cover);
        if (!ModelState.IsValid)
        {
            return await NovelForm(input, isNew ? null : novel);
        }

        string? uploadedCover = null;
        try
        {
            if (input.Cover is not null && extension is not null)
            {
                var directory = Path.Combine(environment.WebRootPath, "uploads", "covers");
                Directory.CreateDirectory(directory);
                var filename = Guid.NewGuid().ToString("N") + extension;
                uploadedCover = "/uploads/covers/" + filename;
                await using var output = System.IO.File.Create(Path.Combine(directory, filename));
                await input.Cover.CopyToAsync(output, HttpContext.RequestAborted);
            }

            novel.Title = input.Title.Trim();
            novel.Description = input.Description?.Trim();
            novel.CategoryId = input.CategoryId;
            novel.Status = input.Status;
            novel.CoverImage = uploadedCover ?? oldCover;
            novel.ReviewStatus = IsAdmin ? "Approved" : "Pending";
            novel.UpdatedAt = DateTime.UtcNow;
            if (isNew) db.Novels.Add(novel);

            await db.SaveChangesAsync();
        }
        catch
        {
            DeleteUploadedCover(uploadedCover);
            throw;
        }

        if (uploadedCover is not null) DeleteUploadedCover(oldCover);
        logger.LogInformation("User {UserId} saved novel {NovelId} with review status {ReviewStatus}",
            UserId, novel.Id, novel.ReviewStatus);
        TempData["StudioMessage"] = IsAdmin ? "Đã lưu truyện." : "Đã gửi truyện để kiểm duyệt.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveChapter(Novel novel, Chapter chapter, ChapterInput input)
    {
        if (await db.Chapters.AnyAsync(item => item.NovelId == novel.Id
                && item.Id != chapter.Id && item.Number == input.Number))
        {
            ModelState.AddModelError(nameof(input.Number), "Số chương này đã tồn tại.");
        }

        if (!ModelState.IsValid) return ChapterForm(novel, input, chapter.Id);

        chapter.Number = input.Number;
        chapter.Title = input.Title.Trim();
        chapter.Content = input.Content.Trim();
        chapter.Status = IsAdmin ? "Published" : "Pending";
        chapter.PublishedAt = IsAdmin ? DateTime.UtcNow : null;
        if (chapter.Id == 0) db.Chapters.Add(chapter);
        await db.SaveChangesAsync();

        logger.LogInformation("User {UserId} saved chapter {ChapterId} with status {Status}",
            UserId, chapter.Id, chapter.Status);
        TempData["StudioMessage"] = IsAdmin ? "Đã lưu chương." : "Đã gửi chương để kiểm duyệt.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> NovelForm(NovelInput input, Novel? novel = null)
    {
        ViewBag.NovelId = novel?.Id ?? 0;
        ViewBag.Cover = novel?.CoverImage;
        ViewBag.Categories = new SelectList(await db.Categories.OrderBy(category => category.Name)
            .ToListAsync(), "Id", "Name");
        return View("Create", input);
    }

    private IActionResult ChapterForm(Novel novel, ChapterInput input, int chapterId = 0)
    {
        ViewBag.Novel = novel;
        ViewBag.ChapterId = chapterId;
        return View("AddChapter", input);
    }

    private async Task<string?> ValidateCover(IFormFile file)
    {
        if (file.Length is < 12 or > MaxCoverBytes)
        {
            ModelState.AddModelError(nameof(NovelInput.Cover), "Ảnh phải có dung lượng tối đa 3 MB.");
            return null;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        await stream.ReadExactlyAsync(header, HttpContext.RequestAborted);

        var valid = extension switch
        {
            ".jpg" or ".jpeg" => file.ContentType == "image/jpeg"
                && header.AsSpan(0, 3).SequenceEqual(new byte[] { 255, 216, 255 }),
            ".png" => file.ContentType == "image/png"
                && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".webp" => file.ContentType == "image/webp"
                && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };

        if (valid) return extension == ".jpeg" ? ".jpg" : extension;

        ModelState.AddModelError(nameof(NovelInput.Cover), "Chọn ảnh JPG, PNG hoặc WebP hợp lệ.");
        return null;
    }

    private void DeleteUploadedCover(string? cover)
    {
        const string prefix = "/uploads/covers/";
        if (cover?.StartsWith(prefix, StringComparison.Ordinal) != true) return;

        var filename = cover[prefix.Length..];
        if (filename != Path.GetFileName(filename)) return;
        if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(filename), "N", out _)) return;

        try
        {
            System.IO.File.Delete(Path.Combine(environment.WebRootPath, "uploads", "covers", filename));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not remove cover {Filename}", filename);
        }
    }
}
