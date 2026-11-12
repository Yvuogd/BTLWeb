using Microsoft.EntityFrameworkCore;
using NovelPlatform.Models;

namespace NovelPlatform.Data;

public static partial class SeedData
{
    private static async Task AddNewStoriesAsync(AppDbContext db)
    {
        var authorRoleId = await db.Roles.Where(role => role.Name == "Author")
            .Select(role => role.Id).SingleAsync();
        var categories = (await db.Categories.OrderBy(category => category.Id).ToListAsync())
            .GroupBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var story in AdditionalStories)
        {
            if (await db.Users.AnyAsync(user => user.Username == story.AuthorUsername))
            {
                continue;
            }

            if (!categories.TryGetValue(story.Category, out var category))
            {
                category = new Category { Name = story.Category, Description = "Thể loại: " + story.Category };
                categories.Add(category.Name, category);
            }

            var novel = new Novel
            {
                Title = story.Title,
                Author = NewUser(story.AuthorUsername, story.AuthorUsername + "@novel.local",
                    story.AuthorName, authorRoleId),
                Category = category,
                Description = story.Description,
                CoverImage = story.CoverImage,
                Status = "Ongoing",
                ReviewStatus = "Approved",
                Chapters = story.ChapterTitles.Select((title, index) => new Chapter
                {
                    Number = index + 1,
                    Title = title,
                    Content = $"Nội dung chương {index + 1} (bản mẫu thử nghiệm).",
                    Status = "Published",
                    PublishedAt = DateTime.UtcNow
                }).ToList()
            };
            db.Novels.Add(novel);
        }

        await db.SaveChangesAsync();
    }

    private static async Task ConfigureDemoAccountsAsync(AppDbContext db, IConfiguration configuration)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.NovelPlatformSecurityUpgrade', N'U') IS NULL
            BEGIN
                UPDATE dbo.Users
                SET PasswordHash = N'', Status = N'Blocked'
                WHERE Email LIKE N'%@novel.local' AND
                    (Username IN (N'docgia', N'tacgia', N'admin') OR Username LIKE N'tacgia-%');
                CREATE TABLE dbo.NovelPlatformSecurityUpgrade (Version int NOT NULL PRIMARY KEY);
                INSERT dbo.NovelPlatformSecurityUpgrade VALUES (1);
            END
            """);

        foreach (var username in new[] { "docgia", "tacgia", "admin" })
        {
            var password = configuration[$"DemoAccounts:{username}"];
            if (string.IsNullOrEmpty(password))
            {
                continue;
            }

            if (password.Length is < 12 or > 128)
            {
                throw new InvalidOperationException($"DemoAccounts:{username} must have 12–128 characters.");
            }

            var user = await db.Users.SingleOrDefaultAsync(item =>
                item.Username == username && item.Email.EndsWith("@novel.local"));
            if (user is not null && !Verify(password, user.PasswordHash))
            {
                user.PasswordHash = Hash(password);
                user.Status = "Active";
            }
        }

        await db.SaveChangesAsync();
    }
    private static readonly SampleStory[] AdditionalStories =
    [
        new(
            "Đại Hạ Thánh Đình, Cả Triều Toàn Là Thiên Cổ Người Tài",
            "tacgia-dai-ha",
            "Túy Diệc Vô Miên",
            "Cổ Đại, Hệ Thống",
            "",
            "Xuyên đến dị giới trong thân phận công tử, nhân vật chính thức tỉnh hệ thống Nhân Hoàng. Với các danh tướng và mưu sĩ như Hạng Vũ, Hoắc Khứ Bệnh và Gia Cát Lượng, hắn bắt đầu xây dựng Đại Hạ giữa cuộc tranh bá của vạn tộc.",
            "/images/cover-dai-ha.jpg",
            ["Lời gọi của Nhân Hoàng", "Danh tướng đầu tiên", "Dựng cờ Đại Hạ"]),
        new(
            "Phù Đạo Chi Tổ / Tạp Dịch Đệ Tử Không Đường Ra? Ta Lấy Vẽ Bùa Đạo Trường Sinh",
            "tacgia-phu-dao",
            "Phiên Gia Trám Đại Tương",
            "Tiên Hiệp, Huyền Huyễn, Cổ Đại",
            "",
            "Một thiếu niên chăn trâu cứu đệ tử tạp dịch và bước vào Tu Tiên giới. Sau khi thức tỉnh huyết mạch ngọc phù, cậu theo đuổi trường sinh bằng nghề vẽ bùa.",
            "/images/cover-phu-dao.jpg",
            ["Thiếu niên chăn trâu", "Huyết mạch ngọc phù", "Lá bùa đầu tiên"]),
        new(
            "Già Thiên: Khai Cục Bái Nhập Dao Quang Thánh Địa",
            "tacgia-gia-thien",
            "Ngã Ái Cật Điềm Tống",
            "Huyền Huyễn, Xuyên Không, Cổ Đại",
            "",
            "Lý Nghiêu xuyên đến thế giới Già Thiên và nhập môn ở Hằng Dương Động Thiên. Dao Quang Thánh Địa đến ngay khi hắn vừa hoàn thành tu luyện cơ sở, mang theo cơ duyên liên quan tới Long Văn Hắc Kim Đỉnh.",
            "/images/cover-gia-thien.jpg",
            ["Hằng Dương Động Thiên", "Dao Quang đến", "Long Văn Hắc Kim Đỉnh"]),
        new(
            "Hỏa Hệ Pháp Gia Tu Luyện Chỉ Nam",
            "tacgia-hoa-he",
            "Hàm Ngư Cật Tây Qua",
            "Huyền Huyễn, Khác",
            "",
            "Trần Dật nhớ lại kiếp trước khi bị cuốn vào một thế giới trò chơi. Với giao diện bí ẩn và năng lực điều khiển lửa, hắn tìm con đường tu luyện của riêng mình.",
            "/images/cover-hoa-he.jpg",
            ["Giao diện bí ẩn", "Ngọn lửa đầu tiên", "Bước vào thế giới trò chơi"]),
        new(
            "Hoàn Mỹ Thế Giới Chi Duy Ta Vĩnh Hằng",
            "tacgia-hoan-my",
            "Phong Vũ Kinh Thế",
            "Huyền Huyễn, Xuyên Không, Cổ Đại",
            "",
            "Xuyên qua dòng thời gian và biển vận mệnh, nhân vật chính đi tìm sự vĩnh hằng. Trên hành trình ấy, hắn gặp Nguyệt Thiền và khám phá những bí ẩn giữa cổ kim.",
            "/images/cover-hoan-my.jpg",
            ["Vượt biển vận mệnh", "Gặp Nguyệt Thiền", "Một niệm vĩnh hằng"]),
        new(
            "Phàm Nhân Tu Tiên: Khai Cục Đạt Được Tam Mẫu Linh Điền / Tiên Đạo Trường Thanh",
            "tacgia-linh-dien",
            "Đặt Danh",
            "Tiên Hiệp, Huyền Huyễn, Xuyên Không",
            "",
            "Mang theo ba mẫu linh điền đến thế giới tu tiên, nhân vật chính được nhị thúc dẫn vào tiên đồ. Là một tán tu, hắn dựa vào trồng trọt và tu luyện để từng bước theo đuổi trường sinh.",
            "/images/cover-linh-dien.jpg",
            ["Ba mẫu linh điền", "Nhị thúc dẫn đường", "Vụ mùa đầu tiên"])
    ];
}
