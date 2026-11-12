using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NovelPlatform.Models;

namespace NovelPlatform.Data;

public static partial class SeedData
{
    private static readonly SampleStory[] SampleStories =
    [
        new(
            "Mỗi Ngày Tình Báo: Từ Rắn Nước Đến Kinh Hà Long Vương",
            "tacgia-ran-nuoc",
            "Tương Công Nạp Cá Thiếp Ba",
            "Tiên Hiệp, Xuyên Không, Cổ Đại",
            "Fantasy",
            "Liễu Thanh xuyên thành một con rắn nước ở hồ sâu, đứng trước nguy cơ bị thiên địch săn đuổi. Hắn thức tỉnh hệ thống tình báo mỗi ngày và bắt đầu hành trình tìm kiếm cơ duyên để hóa thành Kinh Hà Long Vương.",
            "/images/cover-ran-nuoc.jpg",
            ["Tỉnh dậy giữa hồ sâu", "Tiếng nói của hệ thống", "Tin tức đầu tiên", "Dấu vết long cung"]),
        new(
            "Đại Đường, Ta Mới Vừa Xuyên Qua, Thế Nhưng Cho Ta Phát Tức Phụ",
            "tacgia-dai-duong",
            "Truy Cần Thời Gian Đích Tiểu Oa Ngư",
            "Kiếm Hiệp, Ngôn Tình, Huyền Huyễn",
            "Romance",
            "Vừa xuyên đến Đại Đường, nhân vật chính bất ngờ được sắp đặt một mối hôn sự. Giữa triều đại xa lạ, những tình huống dở khóc dở cười và cơ duyên mới cũng từ đó bắt đầu.",
            "/images/cover-dai-duong.jpg",
            ["Xuyên đến Đại Đường", "Mối hôn sự bất ngờ", "Ngày đầu ở phủ"]),
        new(
            "Tây Môn Tiên Tộc",
            "tacgia-tay-mon",
            "Đạo Tâm Trường Thanh",
            "Tiên Hiệp, Huyền Huyễn",
            "Mystery",
            "Cùng Tây Môn gia gây dựng lại cơ nghiệp tiên tộc: tu luyện, mở cửa hàng, khám phá bí cảnh và chinh phục vùng biển xa. Gia tộc từng bước trở lại thời kỳ huy hoàng.",
            "/images/cover-tay-mon.jpg",
            ["Tây Môn gia", "Mầm non tu tiên", "Cửa hàng đầu tiên", "Tin tức bí cảnh", "Cơ nghiệp mới"]),
        new(
            "Toàn Dân Hải Đảo Cầu Sinh: Ta Có Thể Nghe Được Vạn Vật Tiếng Lòng",
            "tacgia-hai-dao",
            "Lão Tưởng Trù Yên",
            "Ngôn Tình, Huyền Huyễn, Đô Thị, Mạt Thế, Khác",
            "Adventure",
            "Giữa cuộc chơi sinh tồn trên quần đảo bí ẩn, nhân vật chính phát hiện mình có thể nghe được tiếng lòng của vạn vật. Mỗi lựa chọn về tài nguyên, đồng đội và hiểm họa đều ảnh hưởng đến con đường sống còn.",
            "/images/cover-hai-dao.jpg",
            ["Ngày đầu trên đảo", "Tiếng lòng đầu tiên", "Tín hiệu cầu cứu", "Đêm giữa biển khơi"])
    ];

    public static async Task InitializeAsync(AppDbContext db, IConfiguration configuration)
    {
        await db.Database.EnsureCreatedAsync();

        // EnsureCreated does not update existing tables, so add this field for older databases.
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH(N'dbo.Novels', N'ReviewStatus') IS NULL
            BEGIN
                ALTER TABLE dbo.Novels
                ADD ReviewStatus nvarchar(20) NOT NULL
                    CONSTRAINT DF_Novels_ReviewStatus DEFAULT N'Approved' WITH VALUES;
            END
            IF COL_LENGTH(N'dbo.Users', N'SessionVersion') IS NULL
            BEGIN
                ALTER TABLE dbo.Users
                ADD SessionVersion uniqueidentifier NOT NULL
                    CONSTRAINT DF_Users_SessionVersion DEFAULT '00000000-0000-0000-0000-000000000000' WITH VALUES;
            END
            """);

        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sp_getapplock
                @Resource = N'NovelPlatform.Seed', @LockMode = N'Exclusive', @LockOwner = N'Transaction';
            IF @result < 0 THROW 50000, 'Unable to lock sample-data initialization.', 1;
            """);

        await ReplaceExistingSampleStoriesAsync(db);
        await InitializeOriginalStoriesAsync(db);
        await AddNewStoriesAsync(db);
        await ConfigureDemoAccountsAsync(db, configuration);
        await transaction.CommitAsync();
    }

    private static async Task InitializeOriginalStoriesAsync(AppDbContext db)
    {

        if (await db.Users.AnyAsync())
        {
            return;
        }

        var roles = new[]
        {
            new UserRole { Name = "Reader" },
            new UserRole { Name = "Author" },
            new UserRole { Name = "Admin" }
        };

        db.Roles.AddRange(roles);
        await db.SaveChangesAsync();

        var reader = NewUser(
            "docgia", "reader@novel.local", "Độc giả Mây", roles[0].Id);
        var admin = NewUser(
            "admin", "admin@novel.local", "Quản trị viên", roles[2].Id);
        var authorAccount = NewUser(
            "tacgia", "author@novel.local", "Minh An", roles[1].Id);

        var authors = SampleStories
            .Select(story => NewUser(
                story.AuthorUsername,
                $"{story.AuthorUsername}@novel.local",
                story.AuthorName,
                roles[1].Id))
            .ToArray();

        db.Users.AddRange([reader, authorAccount, .. authors, admin]);

        var categories = SampleStories
            .Select(story => new Category
            {
                Name = story.Category,
                Description = "Thể loại: " + story.Category
            })
            .ToArray();

        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        var novels = SampleStories.Select((story, index) => new Novel
        {
            Author = authors[index],
            Category = categories[index],
            Title = story.Title,
            Description = story.Description,
            CoverImage = story.CoverImage,
            Status = "Ongoing",
            ReviewStatus = "Approved"
        }).ToArray();

        db.Novels.AddRange(novels);
        await db.SaveChangesAsync();

        for (var novelIndex = 0; novelIndex < novels.Length; novelIndex++)
        {
            for (var chapterIndex = 0;
                 chapterIndex < SampleStories[novelIndex].ChapterTitles.Length;
                 chapterIndex++)
            {
                var chapterTitle = SampleStories[novelIndex].ChapterTitles[chapterIndex];
                db.Chapters.Add(new Chapter
                {
                    NovelId = novels[novelIndex].Id,
                    Number = chapterIndex + 1,
                    Title = chapterTitle,
                    Status = "Published",
                    PublishedAt = DateTime.UtcNow.AddDays(-novelIndex - chapterIndex),
                    Content = $"Nội dung chương {chapterIndex + 1} (bản mẫu thử nghiệm)."
                });
            }
        }

        db.Ratings.AddRange(
            new Rating { User = reader, Novel = novels[0], Score = 5 },
            new Rating { User = reader, Novel = novels[1], Score = 4 },
            new Rating { User = reader, Novel = novels[2], Score = 5 });

        db.Comments.AddRange(
            new Comment
            {
                User = reader,
                Novel = novels[0],
                Content = "Mình đang theo dõi câu chuyện này, đây là bình luận mẫu."
            },
            new Comment
            {
                User = reader,
                Novel = novels[1],
                Content = "Nội dung chương mẫu dễ đọc, mong chờ phần tiếp theo."
            });

        db.Bookmarks.Add(new Bookmark { User = reader, Novel = novels[0] });
        await db.SaveChangesAsync();
    }

    private static async Task ReplaceExistingSampleStoriesAsync(AppDbContext db)
    {
        if (!await db.Users.AnyAsync())
        {
            return;
        }

        var previousTitles = new[]
        {
            "Thư viện giữa tầng mây",
            "Mùa hạ viết tiếp",
            "Bí mật căn phòng số 9",
            "Đường về phía bình minh"
        };
        var existingNovels = await db.Novels
            .Where(novel => previousTitles.Contains(novel.Title))
            .ToListAsync();

        if (existingNovels.Count == 0)
        {
            return;
        }

        var authorRole = await db.Roles.FirstOrDefaultAsync(role => role.Name == "Author");
        if (authorRole is null)
        {
            return;
        }

        for (var index = 0; index < SampleStories.Length; index++)
        {
            var story = SampleStories[index];
            var novel = existingNovels.FirstOrDefault(item =>
                item.Title == previousTitles[index]);
            if (novel is null)
            {
                continue;
            }

            var category = await db.Categories
                .FirstOrDefaultAsync(item => item.Name == story.Category);
            if (category is null)
            {
                category = new Category
                {
                    Name = story.Category,
                    Description = "Thể loại: " + story.Category
                };
                db.Categories.Add(category);
            }

            var author = await db.Users
                .FirstOrDefaultAsync(user => user.Username == story.AuthorUsername);
            if (author is null)
            {
                author = NewUser(
                    story.AuthorUsername,
                    $"{story.AuthorUsername}@novel.local",
                    story.AuthorName,
                    authorRole.Id);
                db.Users.Add(author);
            }
            else
            {
                author.DisplayName = story.AuthorName;
            }

            novel.Title = story.Title;
            novel.Author = author;
            novel.Category = category;
            novel.Description = story.Description;
            novel.CoverImage = story.CoverImage;
            novel.Status = "Ongoing";
            novel.ReviewStatus = "Approved";
            novel.UpdatedAt = DateTime.UtcNow;

            var oldChapters = await db.Chapters
                .OrderBy(chapter => chapter.Number)
                .Where(chapter => chapter.NovelId == novel.Id)
                .ToListAsync();

            if (oldChapters.Count > story.ChapterTitles.Length)
            {
                db.Chapters.RemoveRange(oldChapters.Skip(story.ChapterTitles.Length));
            }

            for (var chapterIndex = 0;
                 chapterIndex < story.ChapterTitles.Length;
                 chapterIndex++)
            {
                var chapter = chapterIndex < oldChapters.Count
                    ? oldChapters[chapterIndex]
                    : new Chapter { Novel = novel };

                chapter.Number = chapterIndex + 1;
                chapter.Title = story.ChapterTitles[chapterIndex];
                chapter.Content = $"Nội dung chương {chapterIndex + 1} (bản mẫu thử nghiệm).";
                chapter.Status = "Published";
                chapter.PublishedAt = DateTime.UtcNow;

                if (chapterIndex >= oldChapters.Count)
                {
                    db.Chapters.Add(chapter);
                }
            }
        }

        var previousCategories = SampleStories
            .Select(story => story.PreviousCategory)
            .Distinct()
            .ToArray();
        var unusedPreviousCategories = await db.Categories
            .Where(category => previousCategories.Contains(category.Name)
                && !category.Novels.Any())
            .ToListAsync();
        db.Categories.RemoveRange(unusedPreviousCategories);

        await db.SaveChangesAsync();
    }

    private sealed record SampleStory(
        string Title,
        string AuthorUsername,
        string AuthorName,
        string Category,
        string PreviousCategory,
        string Description,
        string CoverImage,
        string[] ChapterTitles);

    private static AppUser NewUser(
        string username,
        string email,
        string displayName,
        int roleId)
    {
        return new AppUser
        {
            Username = username,
            Email = email,
            DisplayName = displayName,
            RoleId = roleId,
            PasswordHash = "",
            Status = "Blocked"
        };
    }

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            120_000,
            HashAlgorithmName.SHA256,
            32);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encoded)
    {
        try
        {
            var parts = encoded.Split('.');
            if (parts.Length != 2 || string.IsNullOrEmpty(password)) return false;
            var expectedHash = Convert.FromBase64String(parts[1]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                Convert.FromBase64String(parts[0]),
                120_000,
                HashAlgorithmName.SHA256,
                32);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    public static string SessionStamp(string passwordHash) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(passwordHash)));
}

