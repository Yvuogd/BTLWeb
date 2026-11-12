using Microsoft.EntityFrameworkCore;
using NovelPlatform.Models;

namespace NovelPlatform.Data;

public static class NovelCatalog
{
    public const int PageSize = 6;

    public static IQueryable<Novel> Search(AppDbContext db, string? q, int? category, string? genre)
    {
        var query = db.Novels.AsNoTracking().Where(novel => novel.ReviewStatus == "Approved");
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(novel => novel.Title.Contains(term)
                || novel.Author.DisplayName.Contains(term) || novel.Category.Name.Contains(term));
        }

        if (category.HasValue)
        {
            query = query.Where(novel => novel.CategoryId == category.Value);
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            var term = genre.Trim();
            query = query.Where(novel => novel.Category.Name.Contains(term));
        }

        return query;
    }

    public static IQueryable<Novel> Page(IQueryable<Novel> query, int page) =>
        query.OrderByDescending(novel => novel.CreatedAt).ThenByDescending(novel => novel.Id)
            .Skip((page - 1) * PageSize).Take(PageSize);

    public static int PageCount(int total) => Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
}
