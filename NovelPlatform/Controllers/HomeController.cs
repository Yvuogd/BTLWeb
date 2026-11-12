using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovelPlatform.Data;
using NovelPlatform.Models;

namespace NovelPlatform.Controllers;

public class HomeController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int? category, string? genre, int page = 1)
    {
        var query = NovelCatalog.Search(db, q, category, genre);
        ViewBag.Total = await query.CountAsync();
        int pages = NovelCatalog.PageCount((int)ViewBag.Total);
        ViewBag.Pages = pages;
        ViewBag.Page = page = Math.Clamp(page, 1, pages);
        ViewBag.Query = q;
        ViewBag.Category = category;
        ViewBag.Genre = genre;

        var novels = await NovelCatalog.Page(query, page)
            .Include(novel => novel.Author)
            .Include(novel => novel.Category)
            .Include(novel => novel.Ratings)
            .Include(novel => novel.Chapters.Where(chapter => chapter.Status == "Published"))
            .AsSplitQuery()
            .ToListAsync();

        Response.Headers.Vary = "X-Requested-With";
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_Catalog", novels);
        }

        ViewBag.Categories = await db.Categories.OrderBy(category => category.Name).ToListAsync();
        return View(novels);
    }

    public async Task<IActionResult> Details(int id)
    {
        var novel = await db.Novels
            .Where(item => item.Id == id && item.ReviewStatus == "Approved")
            .Include(item => item.Author)
            .Include(item => item.Category)
            .Include(item => item.Ratings)
            .Include(item => item.Chapters
                .Where(chapter => chapter.Status == "Published")
                .OrderBy(chapter => chapter.Number))
            .Include(item => item.Comments)
                .ThenInclude(comment => comment.User)
            .FirstOrDefaultAsync();

        if (novel is null)
        {
            return NotFound();
        }

        var userIdClaim = User.FindFirst("uid")?.Value;
        ViewBag.Bookmarked = int.TryParse(userIdClaim, out var userId)
            && await db.Bookmarks.AnyAsync(bookmark =>
                bookmark.NovelId == id && bookmark.UserId == userId);

        return View(novel);
    }

    public async Task<IActionResult> Read(int id)
    {
        var chapter = await db.Chapters
            .Include(item => item.Novel)
                .ThenInclude(novel => novel.Author)
            .Include(item => item.Novel)
                .ThenInclude(novel => novel.Chapters.OrderBy(ch => ch.Number))
            .FirstOrDefaultAsync(item => item.Id == id
                && item.Status == "Published"
                && item.Novel.ReviewStatus == "Approved");

        if (chapter is null)
        {
            return NotFound();
        }

        ViewBag.Previous = chapter.Novel.Chapters
            .Where(item => item.Number < chapter.Number && item.Status == "Published")
            .OrderByDescending(item => item.Number)
            .Select(item => (int?)item.Id)
            .FirstOrDefault();

        ViewBag.Next = chapter.Novel.Chapters
            .Where(item => item.Number > chapter.Number && item.Status == "Published")
            .OrderBy(item => item.Number)
            .Select(item => (int?)item.Id)
            .FirstOrDefault();

        return View(chapter);
    }
}

