using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovelPlatform.Data;
using NovelPlatform.Models;

namespace NovelPlatform.Controllers;

[ApiController]
[Route("api/novels")]
public class NovelsApiController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<NovelPage> List(string? q, int? category, string? genre, int page = 1)
    {
        var query = NovelCatalog.Search(db, q, category, genre);
        var total = await query.CountAsync();
        var pages = NovelCatalog.PageCount(total);
        page = Math.Clamp(page, 1, pages);
        return new NovelPage(page, NovelCatalog.PageSize, total, pages,
            await Summaries(NovelCatalog.Page(query, page)).ToListAsync());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NovelSummary>> Get(int id)
    {
        var novel = await Summaries(NovelCatalog.Search(db, null, null, null)
            .Where(novel => novel.Id == id)).SingleOrDefaultAsync();
        return novel is null ? NotFound() : novel;
    }

    [HttpGet("{id:int}/chapters")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ChapterSummary>>> Chapters(int id)
    {
        if (!await NovelCatalog.Search(db, null, null, null).AnyAsync(novel => novel.Id == id))
        {
            return NotFound();
        }

        return await db.Chapters.AsNoTracking()
            .Where(chapter => chapter.NovelId == id && chapter.Status == "Published"
                && chapter.Novel.ReviewStatus == "Approved")
            .OrderBy(chapter => chapter.Number)
            .Select(chapter => new ChapterSummary(chapter.Id, chapter.Number, chapter.Title))
            .ToListAsync();
    }

    private static IQueryable<NovelSummary> Summaries(IQueryable<Novel> query) =>
        query.Select(novel => new NovelSummary(novel.Id, novel.Title, novel.Description,
            novel.Author.DisplayName, novel.Category.Name, novel.CoverImage, novel.Status,
            novel.Chapters.Count(chapter => chapter.Status == "Published")));

    public sealed record NovelSummary(int Id, string Title, string? Description, string Author,
        string Category, string? CoverImage, string Status, int PublishedChapters);

    public sealed record NovelPage(int Page, int PageSize, int Total, int TotalPages,
        List<NovelSummary> Items);

    public sealed record ChapterSummary(int Id, int Number, string Title);
}
