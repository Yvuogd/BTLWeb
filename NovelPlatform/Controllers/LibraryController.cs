using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovelPlatform.Data;
using NovelPlatform.Models;

namespace NovelPlatform.Controllers;

[Authorize]
public class LibraryController(AppDbContext db) : Controller
{
    private int UserId => int.Parse(User.FindFirst("uid")!.Value);

    public async Task<IActionResult> Index()
    {
        var bookmarks = await db.Bookmarks
            .Where(bookmark => bookmark.UserId == UserId
                && bookmark.Novel.ReviewStatus == "Approved")
            .Include(bookmark => bookmark.Novel)
                .ThenInclude(novel => novel.Author)
            .Include(bookmark => bookmark.Novel)
                .ThenInclude(novel => novel.Ratings)
            .Include(bookmark => bookmark.Novel)
                .ThenInclude(novel => novel.Category)
            .ToListAsync();

        return View(bookmarks);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var novelExists = await db.Novels.AnyAsync(novel =>
            novel.Id == id && novel.ReviewStatus == "Approved");
        if (!novelExists)
        {
            return NotFound();
        }

        var bookmark = await db.Bookmarks.FindAsync(UserId, id);
        if (bookmark is null)
        {
            db.Bookmarks.Add(new Bookmark { UserId = UserId, NovelId = id });
        }
        else
        {
            db.Bookmarks.Remove(bookmark);
        }

        await db.SaveChangesAsync();
        return RedirectToAction("Details", "Home", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(int id, int score)
    {
        if (score is < 1 or > 5)
        {
            return BadRequest();
        }

        var novelExists = await db.Novels.AnyAsync(novel =>
            novel.Id == id && novel.ReviewStatus == "Approved");
        if (!novelExists)
        {
            return NotFound();
        }

        var rating = await db.Ratings.FindAsync(UserId, id);
        if (rating is null)
        {
            db.Ratings.Add(new Rating { UserId = UserId, NovelId = id, Score = score });
        }
        else
        {
            rating.Score = score;
            rating.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return RedirectToAction("Details", "Home", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comment(int id, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Trim().Length > 2000)
        {
            return RedirectToAction("Details", "Home", new { id });
        }

        var novelExists = await db.Novels.AnyAsync(novel =>
            novel.Id == id && novel.ReviewStatus == "Approved");
        if (!novelExists)
        {
            return NotFound();
        }

        db.Comments.Add(new Comment
        {
            UserId = UserId,
            NovelId = id,
            Content = content.Trim()
        });

        await db.SaveChangesAsync();
        return RedirectToAction("Details", "Home", new { id });
    }
}
