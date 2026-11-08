using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovelPlatform.Data;

namespace NovelPlatform.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Users = await db.Users
            .Include(user => user.Role)
            .OrderBy(user => user.Username)
            .ToListAsync();

        ViewBag.Categories = await db.Categories
            .OrderBy(category => category.Name)
            .ToListAsync();

        ViewBag.Comments = await db.Comments
            .Include(comment => comment.User)
            .Include(comment => comment.Novel)
            .OrderByDescending(comment => comment.CreatedAt)
            .Take(25)
            .ToListAsync();

        ViewBag.PendingNovels = await db.Novels
            .Where(novel => novel.ReviewStatus == "Pending")
            .Include(novel => novel.Author)
            .Include(novel => novel.Category)
            .OrderBy(novel => novel.CreatedAt)
            .ToListAsync();

        ViewBag.PendingChapters = await db.Chapters
            .Where(chapter => chapter.Status == "Pending")
            .Include(chapter => chapter.Novel)
                .ThenInclude(novel => novel.Author)
            .OrderBy(chapter => chapter.CreatedAt)
            .ToListAsync();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewNovel(int id, string decision)
    {
        var novel = await db.Novels.FindAsync(id);
        if (novel is null)
        {
            return NotFound();
        }

        if (novel.ReviewStatus != "Pending")
        {
            TempData["AdminMessage"] = "Truyện này đã được xử lý trước đó.";
            return RedirectToAction(nameof(Index));
        }

        novel.ReviewStatus = decision == "approve" ? "Approved" : "Rejected";
        novel.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        TempData["AdminMessage"] = decision == "approve"
            ? "Đã duyệt truyện."
            : "Đã từ chối truyện.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewChapter(int id, string decision)
    {
        var chapter = await db.Chapters
            .Include(item => item.Novel)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (chapter is null)
        {
            return NotFound();
        }

        if (chapter.Status != "Pending")
        {
            TempData["AdminMessage"] = "Chương này đã được xử lý trước đó.";
            return RedirectToAction(nameof(Index));
        }

        chapter.Status = decision == "approve" ? "Published" : "Rejected";
        chapter.PublishedAt = decision == "approve" ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();

        TempData["AdminMessage"] = decision == "approve"
            ? "Đã duyệt và đăng chương."
            : "Đã từ chối chương.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUser(int id)
    {
        var user = await db.Users.FindAsync(id);
        var currentUserId = int.TryParse(User.FindFirst("uid")?.Value, out var parsedId)
            ? parsedId
            : 0;

        if (user is not null && user.Id != currentUserId)
        {
            user.Status = user.Status == "Active" ? "Blocked" : "Active";
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var comment = await db.Comments.FindAsync(id);
        if (comment is not null)
        {
            db.Comments.Remove(comment);
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(string name, string? description)
    {
        var normalizedName = name?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedName)
            && !await db.Categories.AnyAsync(category => category.Name == normalizedName))
        {
            db.Categories.Add(new Models.Category
            {
                Name = normalizedName,
                Description = description?.Trim()
            });
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
