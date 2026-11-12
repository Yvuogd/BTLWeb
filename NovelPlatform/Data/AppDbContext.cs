using Microsoft.EntityFrameworkCore;
using NovelPlatform.Models;

namespace NovelPlatform.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserRole> Roles => Set<UserRole>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Novel> Novels => Set<Novel>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<UserRole>()
            .HasIndex(role => role.Name)
            .IsUnique();

        builder.Entity<AppUser>()
            .HasIndex(user => user.Username)
            .IsUnique();

        builder.Entity<AppUser>()
            .HasIndex(user => user.Email)
            .IsUnique();

        builder.Entity<Novel>()
            .HasOne(novel => novel.Author)
            .WithMany(author => author.Novels)
            .HasForeignKey(novel => novel.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Chapter>()
            .HasIndex(chapter => new { chapter.NovelId, chapter.Number })
            .IsUnique();

        builder.Entity<Novel>()
            .HasOne(novel => novel.Category)
            .WithMany(category => category.Novels)
            .HasForeignKey(novel => novel.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Bookmark>()
            .HasKey(bookmark => new { bookmark.UserId, bookmark.NovelId });

        builder.Entity<Rating>()
            .HasKey(rating => new { rating.UserId, rating.NovelId });

        builder.Entity<Rating>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_Rating_Score",
                "[Score] >= 1 AND [Score] <= 5"));

        builder.Entity<Bookmark>()
            .HasOne(bookmark => bookmark.User)
            .WithMany()
            .HasForeignKey(bookmark => bookmark.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Bookmark>()
            .HasOne(bookmark => bookmark.Novel)
            .WithMany()
            .HasForeignKey(bookmark => bookmark.NovelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Rating>()
            .HasOne(rating => rating.User)
            .WithMany()
            .HasForeignKey(rating => rating.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Rating>()
            .HasOne(rating => rating.Novel)
            .WithMany(novel => novel.Ratings)
            .HasForeignKey(rating => rating.NovelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Comment>()
            .HasOne(comment => comment.Novel)
            .WithMany(novel => novel.Comments)
            .HasForeignKey(comment => comment.NovelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Comment>()
            .HasOne(comment => comment.User)
            .WithMany()
            .HasForeignKey(comment => comment.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Chapter>()
            .HasOne(chapter => chapter.Novel)
            .WithMany(novel => novel.Chapters)
            .HasForeignKey(chapter => chapter.NovelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
