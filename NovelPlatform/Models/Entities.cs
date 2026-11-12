using System.ComponentModel.DataAnnotations;

namespace NovelPlatform.Models;

public class UserRole
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string Name { get; set; } = "";

    public List<AppUser> Users { get; set; } = [];
}

public class AppUser
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public UserRole Role { get; set; } = null!;

    [MaxLength(50)]
    public string Username { get; set; } = "";

    [MaxLength(180)]
    public string Email { get; set; } = "";

    [MaxLength(300)]
    public string PasswordHash { get; set; } = "";

    [MaxLength(80)]
    public string DisplayName { get; set; } = "";

    [MaxLength(300)]
    public string? Avatar { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Active";
    public Guid SessionVersion { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Novel> Novels { get; set; } = [];
}

public class Category
{
    public int Id { get; set; }

    [MaxLength(60)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }

    public List<Novel> Novels { get; set; } = [];
}

public class Novel
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public AppUser Author { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    [MaxLength(180)]
    public string Title { get; set; } = "";

    [MaxLength(2500)]
    public string? Description { get; set; }

    [MaxLength(300)]
    public string? CoverImage { get; set; }

    // Status describes whether the story is ongoing or completed.
    [MaxLength(30)]
    public string Status { get; set; } = "Ongoing";

    // ReviewStatus controls moderation and public visibility.
    [MaxLength(20)]
    public string ReviewStatus { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<Chapter> Chapters { get; set; } = [];
    public List<Rating> Ratings { get; set; } = [];
    public List<Comment> Comments { get; set; } = [];
}

public class Chapter
{
    public int Id { get; set; }
    public int NovelId { get; set; }
    public Novel Novel { get; set; } = null!;
    public int Number { get; set; }

    [MaxLength(180)]
    public string Title { get; set; } = "";

    public string Content { get; set; } = "";

    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
}

public class Bookmark
{
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public int NovelId { get; set; }
    public Novel Novel { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Rating
{
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public int NovelId { get; set; }
    public Novel Novel { get; set; } = null!;

    [Range(1, 5)]
    public int Score { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class Comment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public int NovelId { get; set; }
    public Novel Novel { get; set; } = null!;

    [MaxLength(2000)]
    public string Content { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
