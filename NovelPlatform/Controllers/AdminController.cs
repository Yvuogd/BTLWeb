using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovelPlatform.Data;
using NovelPlatform.Models;

namespace NovelPlatform.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(AppDbContext db, ILogger<AdminController> logger) : Controller
{
    private int UserId => int.Parse(User.FindFirst("uid")!.Value);
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
        if (decision is not ("approve" or "reject")) return BadRequest();
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
        logger.LogInformation("Admin {AdminId} reviewed novel {NovelId}: {Decision}", UserId, id, decision);

        TempData["AdminMessage"] = decision == "approve"
            ? "Đã duyệt truyện."
            : "Đã từ chối truyện.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewChapter(int id, string decision)
    {
        if (decision is not ("approve" or "reject")) return BadRequest();
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
        logger.LogInformation("Admin {AdminId} reviewed chapter {ChapterId}: {Decision}", UserId, id, decision);

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
        if (user is not null && user.Id != UserId)
        {
            user.Status = user.Status == "Active" ? "Blocked" : "Active";
            user.SessionVersion = Guid.NewGuid();
            await db.SaveChangesAsync();
            logger.LogInformation("Admin {AdminId} changed user {UserId} status to {Status}",
                UserId, id, user.Status);
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
            logger.LogInformation("Admin {AdminId} deleted comment {CommentId}", UserId, id);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AddCategory(string name, string? description) =>
        SaveCategory(new Category(), name, description);

    [HttpPost]
    public async Task<IActionResult> EditCategory(int id, string name, string? description)
    {
        var category = await db.Categories.FindAsync(id);
        return category is null ? NotFound() : await SaveCategory(category, name, description);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var category = await db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        if (await db.Novels.AnyAsync(novel => novel.CategoryId == id))
        {
            TempData["AdminMessage"] = "Không thể xóa thể loại đang có truyện.";
        }
        else
        {
            db.Categories.Remove(category);
            await db.SaveChangesAsync();
            TempData["AdminMessage"] = "Đã xóa thể loại.";
            logger.LogInformation("Admin {AdminId} deleted category {CategoryId}", UserId, id);
        }

        await transaction.CommitAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveCategory(Category category, string name, string? description)
    {
        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName) || normalizedName.Length > 60
            || description?.Length > 500
            || await db.Categories.AnyAsync(item => item.Id != category.Id && item.Name == normalizedName))
        {
            TempData["AdminMessage"] = "Tên thể loại đã tồn tại hoặc dữ liệu chưa hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        category.Name = normalizedName;
        category.Description = description?.Trim();
        if (category.Id == 0) db.Categories.Add(category);
        await db.SaveChangesAsync();
        logger.LogInformation("Admin {AdminId} saved category {CategoryId}", UserId, category.Id);
        TempData["AdminMessage"] = "Đã lưu thể loại.";
        return RedirectToAction(nameof(Index));
    }
}
