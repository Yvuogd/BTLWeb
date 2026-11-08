using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NovelPlatform.Data;
using NovelPlatform.Models;

namespace NovelPlatform.Controllers;

[Authorize(Roles = "Author,Admin")]
public class StudioController(AppDbContext db) : Controller
{
    private int UserId => int.Parse(User.FindFirst("uid")!.Value);

    public async Task<IActionResult> Index()
    {
        var novels = await db.Novels
            .Where(n => n.AuthorId == UserId || User.IsInRole("Admin"))
            .Include(n => n.Category)
            .Include(n => n.Chapters)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(novels);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategories();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string title,
        string? description,
        int categoryId,
        string status,
        string? coverImage)
    {
        await LoadCategories();

        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError(nameof(title), "Tên truyện là bắt buộc.");
            return View();
        }

        if (!await db.Categories.AnyAsync(category => category.Id == categoryId))
        {
            ModelState.AddModelError(nameof(categoryId), "Vui lòng chọn thể loại hợp lệ.");
            return View();
        }

        var isAdmin = User.IsInRole("Admin");
        var novel = new Novel
        {
            Title = title.Trim(),
            Description = description?.Trim(),
            CategoryId = categoryId,
            Status = status == "Completed" ? "Completed" : "Ongoing",
            AuthorId = UserId,
            CoverImage = string.IsNullOrWhiteSpace(coverImage)
                ? "/images/cloud-library.svg"
                : coverImage.Trim(),
            ReviewStatus = isAdmin ? "Approved" : "Pending"
        };

        db.Novels.Add(novel);
        await db.SaveChangesAsync();

        TempData["StudioMessage"] = isAdmin
            ? "Truyện đã được đăng."
            : "Truyện đã gửi và đang chờ quản trị viên duyệt.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> AddChapter(int id)
    {
        var novel = await FindMyNovelAsync(id);
        if (novel is null)
        {
            return NotFound();
        }

        ViewBag.Novel = novel;
        var nextNumber = (await db.Chapters
            .Where(chapter => chapter.NovelId == id)
            .MaxAsync(chapter => (int?)chapter.Number) ?? 0) + 1;

        return View(new Chapter { NovelId = id, Number = nextNumber });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddChapter(Chapter chapter)
    {
        var novel = await FindMyNovelAsync(chapter.NovelId);
        if (novel is null)
        {
            return NotFound();
        }

        ViewBag.Novel = novel;

        if (string.IsNullOrWhiteSpace(chapter.Title))
        {
            ModelState.AddModelError(nameof(chapter.Title), "Tên chương là bắt buộc.");
        }

        if (chapter.Number < 1)
        {
            ModelState.AddModelError(nameof(chapter.Number), "Số chương phải lớn hơn 0.");
        }

        if (string.IsNullOrWhiteSpace(chapter.Content))
        {
            ModelState.AddModelError(nameof(chapter.Content), "Nội dung chương không được để trống.");
        }

        if (await db.Chapters.AnyAsync(item =>
                item.NovelId == chapter.NovelId && item.Number == chapter.Number))
        {
            ModelState.AddModelError(nameof(chapter.Number), "Số chương này đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(chapter);
        }

        var isAdmin = User.IsInRole("Admin");
        chapter.Title = chapter.Title.Trim();
        chapter.Content = chapter.Content.Trim();
        chapter.Status = isAdmin ? "Published" : "Pending";
        chapter.PublishedAt = isAdmin ? DateTime.UtcNow : null;
        chapter.CreatedAt = DateTime.UtcNow;
        db.Chapters.Add(chapter);
        await db.SaveChangesAsync();

        TempData["StudioMessage"] = isAdmin
            ? "Chương đã được đăng."
            : "Chương đã gửi và đang chờ quản trị viên duyệt.";

        return RedirectToAction(nameof(Index));
    }

    private Task<Novel?> FindMyNovelAsync(int id) => db.Novels.FirstOrDefaultAsync(novel =>
        novel.Id == id && (novel.AuthorId == UserId || User.IsInRole("Admin")));

    private async Task LoadCategories()
    {
        var categories = await db.Categories.OrderBy(category => category.Name).ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name");
    }
}
