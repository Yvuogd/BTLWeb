/* Truyện Chữ - chạy trên SQL Server / SQLEXPRESS một lần để tạo CSDL và dữ liệu mẫu. */
IF DB_ID(N'NovelReadingPlatform') IS NULL CREATE DATABASE NovelReadingPlatform;

GO
USE NovelReadingPlatform;

GO
IF OBJECT_ID(N'dbo.Users',
    N'U') IS NOT NULL RETURN;

CREATE TABLE dbo.Roles(Id int IDENTITY PRIMARY KEY,
    Name nvarchar(40) NOT NULL UNIQUE);

CREATE TABLE dbo.Categories(Id int IDENTITY PRIMARY KEY,
    Name nvarchar(60) NOT NULL UNIQUE,
    Description nvarchar(500) NULL);

CREATE TABLE dbo.Users(Id int IDENTITY PRIMARY KEY,
    RoleId int NOT NULL,
    Username nvarchar(50) NOT NULL UNIQUE,
    Email nvarchar(180) NOT NULL UNIQUE,
    PasswordHash nvarchar(300) NOT NULL,
    DisplayName nvarchar(80) NOT NULL,
    Avatar nvarchar(300) NULL,
    Bio nvarchar(500) NULL,
    Status nvarchar(20) NOT NULL,
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Users_Created DEFAULT SYSUTCDATETIME(),
    SessionVersion uniqueidentifier NOT NULL CONSTRAINT DF_Users_SessionVersion DEFAULT '00000000-0000-0000-0000-000000000000',
    CONSTRAINT FK_Users_Roles FOREIGN KEY(RoleId) REFERENCES dbo.Roles(Id));

CREATE TABLE dbo.Novels(Id int IDENTITY PRIMARY KEY,
    AuthorId int NOT NULL,
    CategoryId int NOT NULL,
    Title nvarchar(180) NOT NULL,
    Description nvarchar(2500) NULL,
    CoverImage nvarchar(300) NULL,
    Status nvarchar(30) NOT NULL,
    ReviewStatus nvarchar(20) NOT NULL CONSTRAINT DF_Novels_ReviewStatus DEFAULT N'Approved',
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Novels_Created DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2 NULL,
    CONSTRAINT FK_Novels_Author FOREIGN KEY(AuthorId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Novels_Category FOREIGN KEY(CategoryId) REFERENCES dbo.Categories(Id));

CREATE TABLE dbo.Chapters(Id int IDENTITY PRIMARY KEY,
    NovelId int NOT NULL,
    Number int NOT NULL,
    Title nvarchar(180) NOT NULL,
    Content nvarchar(max) NOT NULL,
    Status nvarchar(20) NOT NULL,
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Chapters_Created DEFAULT SYSUTCDATETIME(),
    PublishedAt datetime2 NULL,
    CONSTRAINT FK_Chapters_Novels FOREIGN KEY(NovelId) REFERENCES dbo.Novels(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Chapters_Novel_Number UNIQUE(NovelId,
    Number));

CREATE TABLE dbo.Bookmarks(UserId int NOT NULL,
    NovelId int NOT NULL,
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Bookmarks_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Bookmarks PRIMARY KEY(UserId,
    NovelId),
    CONSTRAINT FK_Bookmarks_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Bookmarks_Novels FOREIGN KEY(NovelId) REFERENCES dbo.Novels(Id) ON DELETE CASCADE);

CREATE TABLE dbo.Ratings(UserId int NOT NULL,
    NovelId int NOT NULL,
    Score int NOT NULL,
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Ratings_Created DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2 NULL,
    CONSTRAINT PK_Ratings PRIMARY KEY(UserId,
    NovelId),
    CONSTRAINT CK_Rating_Score CHECK(Score BETWEEN 1 AND 5),
    CONSTRAINT FK_Ratings_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Ratings_Novels FOREIGN KEY(NovelId) REFERENCES dbo.Novels(Id) ON DELETE CASCADE);

CREATE TABLE dbo.Comments(Id int IDENTITY PRIMARY KEY,
    UserId int NOT NULL,
    NovelId int NOT NULL,
    Content nvarchar(2000) NOT NULL,
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Comments_Created DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2 NULL,
    CONSTRAINT FK_Comments_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Comments_Novels FOREIGN KEY(NovelId) REFERENCES dbo.Novels(Id) ON DELETE CASCADE);

INSERT dbo.Roles(Name) VALUES(N'Reader'),
    (N'Author'),
    (N'Admin');

INSERT dbo.Categories(Name,
    Description) VALUES(N'Tiên Hiệp, Xuyên Không, Cổ Đại',
    N'Thể loại: Tiên Hiệp, Xuyên Không, Cổ Đại'),
    (N'Kiếm Hiệp, Ngôn Tình, Huyền Huyễn',
    N'Thể loại: Kiếm Hiệp, Ngôn Tình, Huyền Huyễn'),
    (N'Tiên Hiệp, Huyền Huyễn',
    N'Thể loại: Tiên Hiệp, Huyền Huyễn'),
    (N'Ngôn Tình, Huyền Huyễn, Đô Thị, Mạt Thế, Khác',
    N'Thể loại: Ngôn Tình, Huyền Huyễn, Đô Thị, Mạt Thế, Khác');

INSERT dbo.Users(RoleId,
    Username,
    Email,
    PasswordHash,
    DisplayName,
    Status) VALUES
(1,
    N'docgia',
    N'reader@novel.local',
    N'',
    N'Độc giả Mây',
    N'Blocked'),
    
(2,
    N'tacgia',
    N'author@novel.local',
    N'',
    N'Minh An',
    N'Blocked'),
    
(3,
    N'admin',
    N'admin@novel.local',
    N'',
    N'Quản trị viên',
    N'Blocked'),

(2,
    N'tacgia-ran-nuoc',
    N'tacgia-ran-nuoc@novel.local',
    N'',
    N'Tương Công Nạp Cá Thiếp Ba',
    N'Blocked'),

(2,
    N'tacgia-dai-duong',
    N'tacgia-dai-duong@novel.local',
    N'',
    N'Truy Cần Thời Gian Đích Tiểu Oa Ngư',
    N'Blocked'),

(2,
    N'tacgia-tay-mon',
    N'tacgia-tay-mon@novel.local',
    N'',
    N'Đạo Tâm Trường Thanh',
    N'Blocked'),

(2,
    N'tacgia-hai-dao',
    N'tacgia-hai-dao@novel.local',
    N'',
    N'Lão Tưởng Trù Yên',
    N'Blocked');

INSERT dbo.Novels(AuthorId,
    CategoryId,
    Title,
    Description,
    CoverImage,
    Status,
    ReviewStatus) VALUES
(4,
    1,
    N'Mỗi Ngày Tình Báo: Từ Rắn Nước Đến Kinh Hà Long Vương',
    N'Liễu Thanh xuyên thành một con rắn nước ở hồ sâu, đứng trước nguy cơ bị thiên địch săn đuổi. Hắn thức tỉnh hệ thống tình báo mỗi ngày và bắt đầu hành trình tìm kiếm cơ duyên để hóa thành Kinh Hà Long Vương.',
    N'/images/cover-ran-nuoc.jpg',
    N'Ongoing',
    N'Approved'),
    
(5,
    2,
    N'Đại Đường, Ta Mới Vừa Xuyên Qua, Thế Nhưng Cho Ta Phát Tức Phụ',
    N'Vừa xuyên đến Đại Đường, nhân vật chính bất ngờ được sắp đặt một mối hôn sự. Giữa triều đại xa lạ, những tình huống dở khóc dở cười và cơ duyên mới cũng từ đó bắt đầu.',
    N'/images/cover-dai-duong.jpg',
    N'Ongoing',
    N'Approved'),

(6,
    3,
    N'Tây Môn Tiên Tộc',
    N'Cùng Tây Môn gia gây dựng lại cơ nghiệp tiên tộc: tu luyện, mở cửa hàng, khám phá bí cảnh và chinh phục vùng biển xa. Gia tộc từng bước trở lại thời kỳ huy hoàng.',
    N'/images/cover-tay-mon.jpg',
    N'Ongoing',
    N'Approved'),

(7,
    4,
    N'Toàn Dân Hải Đảo Cầu Sinh: Ta Có Thể Nghe Được Vạn Vật Tiếng Lòng',
    N'Giữa cuộc chơi sinh tồn trên quần đảo bí ẩn, nhân vật chính phát hiện mình có thể nghe được tiếng lòng của vạn vật. Mỗi lựa chọn về tài nguyên, đồng đội và hiểm họa đều ảnh hưởng đến con đường sống còn.',
    N'/images/cover-hai-dao.jpg',
    N'Ongoing',
    N'Approved');

DECLARE @n int=1,
    @i int=0,
    @title nvarchar(180),
    @chapters nvarchar(max),
    @chapter nvarchar(180),
    @pos int,
    @next int;

DECLARE novels CURSOR LOCAL FAST_FORWARD FOR SELECT Id,
    Title FROM dbo.Novels ORDER BY Id;

OPEN novels;
 FETCH NEXT FROM novels INTO @n,
    @title;

WHILE @@FETCH_STATUS=0 BEGIN
 SET @chapters = CASE @n
     WHEN 1 THEN N'Tỉnh dậy giữa hồ sâu|Tiếng nói của hệ thống|Tin tức đầu tiên|Dấu vết long cung'
     WHEN 2 THEN N'Xuyên đến Đại Đường|Mối hôn sự bất ngờ|Ngày đầu ở phủ'
     WHEN 3 THEN N'Tây Môn gia|Mầm non tu tiên|Cửa hàng đầu tiên|Tin tức bí cảnh|Cơ nghiệp mới'
     ELSE N'Ngày đầu trên đảo|Tiếng lòng đầu tiên|Tín hiệu cầu cứu|Đêm giữa biển khơi'
 END;

 SET @i=1;
 SET @pos=1;

 WHILE @pos<=LEN(@chapters) BEGIN SET @next=CHARINDEX(N'|',
    @chapters,
    @pos);
 IF @next=0 SET @next=LEN(@chapters)+1;
 SET @chapter=SUBSTRING(@chapters,
    @pos,
    @next-@pos);

 INSERT dbo.Chapters(NovelId,
    Number,
    Title,
    Content,
    Status,
    PublishedAt) VALUES(@n,
    @i,
    @chapter,
     N'Nội dung chương ' + CONVERT(nvarchar(10), @i) + N' (bản mẫu thử nghiệm).',
    N'Published',
    SYSUTCDATETIME());
 SET @i=@i+1;
 SET @pos=@next+1;
 END;

 FETCH NEXT FROM novels INTO @n,
    @title;
 END;
 CLOSE novels;
 DEALLOCATE novels;

INSERT dbo.Bookmarks(UserId,
    NovelId) VALUES(1,
    1);
 INSERT dbo.Ratings(UserId,
    NovelId,
    Score) VALUES(1,
    1,
    5),
    (1,
    2,
    4),
    (1,
    3,
    5);

INSERT dbo.Comments(UserId,
    NovelId,
    Content) VALUES(1,
    1,
    N'Mình đang theo dõi câu chuyện này, đây là bình luận mẫu.'),
    (1,
    2,
    N'Nội dung chương mẫu dễ đọc, mong chờ phần tiếp theo.');

GO

GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lockResult int;
EXEC @lockResult = sp_getapplock
    @Resource = N'NovelPlatform.Seed', @LockMode = N'Exclusive', @LockOwner = N'Transaction';
IF @lockResult < 0 THROW 50000, 'Unable to lock sample-data initialization.', 1;

DECLARE @Stories table (
    Title nvarchar(180), Username nvarchar(50), Author nvarchar(80),
    Category nvarchar(60), Description nvarchar(2500), Cover nvarchar(300),
    Number int, Chapter nvarchar(180)
);
INSERT @Stories VALUES
    (N'Đại Hạ Thánh Đình, Cả Triều Toàn Là Thiên Cổ Người Tài', N'tacgia-dai-ha', N'Túy Diệc Vô Miên', N'Cổ Đại, Hệ Thống', N'Xuyên đến dị giới trong thân phận công tử, nhân vật chính thức tỉnh hệ thống Nhân Hoàng. Với các danh tướng và mưu sĩ như Hạng Vũ, Hoắc Khứ Bệnh và Gia Cát Lượng, hắn bắt đầu xây dựng Đại Hạ giữa cuộc tranh bá của vạn tộc.', N'/images/cover-dai-ha.jpg', 1, N'Lời gọi của Nhân Hoàng'),
    (N'Đại Hạ Thánh Đình, Cả Triều Toàn Là Thiên Cổ Người Tài', N'tacgia-dai-ha', N'Túy Diệc Vô Miên', N'Cổ Đại, Hệ Thống', N'Xuyên đến dị giới trong thân phận công tử, nhân vật chính thức tỉnh hệ thống Nhân Hoàng. Với các danh tướng và mưu sĩ như Hạng Vũ, Hoắc Khứ Bệnh và Gia Cát Lượng, hắn bắt đầu xây dựng Đại Hạ giữa cuộc tranh bá của vạn tộc.', N'/images/cover-dai-ha.jpg', 2, N'Danh tướng đầu tiên'),
    (N'Đại Hạ Thánh Đình, Cả Triều Toàn Là Thiên Cổ Người Tài', N'tacgia-dai-ha', N'Túy Diệc Vô Miên', N'Cổ Đại, Hệ Thống', N'Xuyên đến dị giới trong thân phận công tử, nhân vật chính thức tỉnh hệ thống Nhân Hoàng. Với các danh tướng và mưu sĩ như Hạng Vũ, Hoắc Khứ Bệnh và Gia Cát Lượng, hắn bắt đầu xây dựng Đại Hạ giữa cuộc tranh bá của vạn tộc.', N'/images/cover-dai-ha.jpg', 3, N'Dựng cờ Đại Hạ'),
    (N'Phù Đạo Chi Tổ / Tạp Dịch Đệ Tử Không Đường Ra? Ta Lấy Vẽ Bùa Đạo Trường Sinh', N'tacgia-phu-dao', N'Phiên Gia Trám Đại Tương', N'Tiên Hiệp, Huyền Huyễn, Cổ Đại', N'Một thiếu niên chăn trâu cứu đệ tử tạp dịch và bước vào Tu Tiên giới. Sau khi thức tỉnh huyết mạch ngọc phù, cậu theo đuổi trường sinh bằng nghề vẽ bùa.', N'/images/cover-phu-dao.jpg', 1, N'Thiếu niên chăn trâu'),
    (N'Phù Đạo Chi Tổ / Tạp Dịch Đệ Tử Không Đường Ra? Ta Lấy Vẽ Bùa Đạo Trường Sinh', N'tacgia-phu-dao', N'Phiên Gia Trám Đại Tương', N'Tiên Hiệp, Huyền Huyễn, Cổ Đại', N'Một thiếu niên chăn trâu cứu đệ tử tạp dịch và bước vào Tu Tiên giới. Sau khi thức tỉnh huyết mạch ngọc phù, cậu theo đuổi trường sinh bằng nghề vẽ bùa.', N'/images/cover-phu-dao.jpg', 2, N'Huyết mạch ngọc phù'),
    (N'Phù Đạo Chi Tổ / Tạp Dịch Đệ Tử Không Đường Ra? Ta Lấy Vẽ Bùa Đạo Trường Sinh', N'tacgia-phu-dao', N'Phiên Gia Trám Đại Tương', N'Tiên Hiệp, Huyền Huyễn, Cổ Đại', N'Một thiếu niên chăn trâu cứu đệ tử tạp dịch và bước vào Tu Tiên giới. Sau khi thức tỉnh huyết mạch ngọc phù, cậu theo đuổi trường sinh bằng nghề vẽ bùa.', N'/images/cover-phu-dao.jpg', 3, N'Lá bùa đầu tiên'),
    (N'Già Thiên: Khai Cục Bái Nhập Dao Quang Thánh Địa', N'tacgia-gia-thien', N'Ngã Ái Cật Điềm Tống', N'Huyền Huyễn, Xuyên Không, Cổ Đại', N'Lý Nghiêu xuyên đến thế giới Già Thiên và nhập môn ở Hằng Dương Động Thiên. Dao Quang Thánh Địa đến ngay khi hắn vừa hoàn thành tu luyện cơ sở, mang theo cơ duyên liên quan tới Long Văn Hắc Kim Đỉnh.', N'/images/cover-gia-thien.jpg', 1, N'Hằng Dương Động Thiên'),
    (N'Già Thiên: Khai Cục Bái Nhập Dao Quang Thánh Địa', N'tacgia-gia-thien', N'Ngã Ái Cật Điềm Tống', N'Huyền Huyễn, Xuyên Không, Cổ Đại', N'Lý Nghiêu xuyên đến thế giới Già Thiên và nhập môn ở Hằng Dương Động Thiên. Dao Quang Thánh Địa đến ngay khi hắn vừa hoàn thành tu luyện cơ sở, mang theo cơ duyên liên quan tới Long Văn Hắc Kim Đỉnh.', N'/images/cover-gia-thien.jpg', 2, N'Dao Quang đến'),
    (N'Già Thiên: Khai Cục Bái Nhập Dao Quang Thánh Địa', N'tacgia-gia-thien', N'Ngã Ái Cật Điềm Tống', N'Huyền Huyễn, Xuyên Không, Cổ Đại', N'Lý Nghiêu xuyên đến thế giới Già Thiên và nhập môn ở Hằng Dương Động Thiên. Dao Quang Thánh Địa đến ngay khi hắn vừa hoàn thành tu luyện cơ sở, mang theo cơ duyên liên quan tới Long Văn Hắc Kim Đỉnh.', N'/images/cover-gia-thien.jpg', 3, N'Long Văn Hắc Kim Đỉnh'),
    (N'Hỏa Hệ Pháp Gia Tu Luyện Chỉ Nam', N'tacgia-hoa-he', N'Hàm Ngư Cật Tây Qua', N'Huyền Huyễn, Khác', N'Trần Dật nhớ lại kiếp trước khi bị cuốn vào một thế giới trò chơi. Với giao diện bí ẩn và năng lực điều khiển lửa, hắn tìm con đường tu luyện của riêng mình.', N'/images/cover-hoa-he.jpg', 1, N'Giao diện bí ẩn'),
    (N'Hỏa Hệ Pháp Gia Tu Luyện Chỉ Nam', N'tacgia-hoa-he', N'Hàm Ngư Cật Tây Qua', N'Huyền Huyễn, Khác', N'Trần Dật nhớ lại kiếp trước khi bị cuốn vào một thế giới trò chơi. Với giao diện bí ẩn và năng lực điều khiển lửa, hắn tìm con đường tu luyện của riêng mình.', N'/images/cover-hoa-he.jpg', 2, N'Ngọn lửa đầu tiên'),
    (N'Hỏa Hệ Pháp Gia Tu Luyện Chỉ Nam', N'tacgia-hoa-he', N'Hàm Ngư Cật Tây Qua', N'Huyền Huyễn, Khác', N'Trần Dật nhớ lại kiếp trước khi bị cuốn vào một thế giới trò chơi. Với giao diện bí ẩn và năng lực điều khiển lửa, hắn tìm con đường tu luyện của riêng mình.', N'/images/cover-hoa-he.jpg', 3, N'Bước vào thế giới trò chơi'),
    (N'Hoàn Mỹ Thế Giới Chi Duy Ta Vĩnh Hằng', N'tacgia-hoan-my', N'Phong Vũ Kinh Thế', N'Huyền Huyễn, Xuyên Không, Cổ Đại', N'Xuyên qua dòng thời gian và biển vận mệnh, nhân vật chính đi tìm sự vĩnh hằng. Trên hành trình ấy, hắn gặp Nguyệt Thiền và khám phá những bí ẩn giữa cổ kim.', N'/images/cover-hoan-my.jpg', 1, N'Vượt biển vận mệnh'),
    (N'Hoàn Mỹ Thế Giới Chi Duy Ta Vĩnh Hằng', N'tacgia-hoan-my', N'Phong Vũ Kinh Thế', N'Huyền Huyễn, Xuyên Không, Cổ Đại', N'Xuyên qua dòng thời gian và biển vận mệnh, nhân vật chính đi tìm sự vĩnh hằng. Trên hành trình ấy, hắn gặp Nguyệt Thiền và khám phá những bí ẩn giữa cổ kim.', N'/images/cover-hoan-my.jpg', 2, N'Gặp Nguyệt Thiền'),
    (N'Hoàn Mỹ Thế Giới Chi Duy Ta Vĩnh Hằng', N'tacgia-hoan-my', N'Phong Vũ Kinh Thế', N'Huyền Huyễn, Xuyên Không, Cổ Đại', N'Xuyên qua dòng thời gian và biển vận mệnh, nhân vật chính đi tìm sự vĩnh hằng. Trên hành trình ấy, hắn gặp Nguyệt Thiền và khám phá những bí ẩn giữa cổ kim.', N'/images/cover-hoan-my.jpg', 3, N'Một niệm vĩnh hằng'),
    (N'Phàm Nhân Tu Tiên: Khai Cục Đạt Được Tam Mẫu Linh Điền / Tiên Đạo Trường Thanh', N'tacgia-linh-dien', N'Đặt Danh', N'Tiên Hiệp, Huyền Huyễn, Xuyên Không', N'Mang theo ba mẫu linh điền đến thế giới tu tiên, nhân vật chính được nhị thúc dẫn vào tiên đồ. Là một tán tu, hắn dựa vào trồng trọt và tu luyện để từng bước theo đuổi trường sinh.', N'/images/cover-linh-dien.jpg', 1, N'Ba mẫu linh điền'),
    (N'Phàm Nhân Tu Tiên: Khai Cục Đạt Được Tam Mẫu Linh Điền / Tiên Đạo Trường Thanh', N'tacgia-linh-dien', N'Đặt Danh', N'Tiên Hiệp, Huyền Huyễn, Xuyên Không', N'Mang theo ba mẫu linh điền đến thế giới tu tiên, nhân vật chính được nhị thúc dẫn vào tiên đồ. Là một tán tu, hắn dựa vào trồng trọt và tu luyện để từng bước theo đuổi trường sinh.', N'/images/cover-linh-dien.jpg', 2, N'Nhị thúc dẫn đường'),
    (N'Phàm Nhân Tu Tiên: Khai Cục Đạt Được Tam Mẫu Linh Điền / Tiên Đạo Trường Thanh', N'tacgia-linh-dien', N'Đặt Danh', N'Tiên Hiệp, Huyền Huyễn, Xuyên Không', N'Mang theo ba mẫu linh điền đến thế giới tu tiên, nhân vật chính được nhị thúc dẫn vào tiên đồ. Là một tán tu, hắn dựa vào trồng trọt và tu luyện để từng bước theo đuổi trường sinh.', N'/images/cover-linh-dien.jpg', 3, N'Vụ mùa đầu tiên');

DECLARE @AddedAuthors table (Username nvarchar(50));
INSERT dbo.Categories (Name, Description)
SELECT DISTINCT story.Category, N'Thể loại: ' + story.Category
FROM @Stories story
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = story.Category);

INSERT dbo.Users (RoleId, Username, Email, PasswordHash, DisplayName, Status)
OUTPUT inserted.Username INTO @AddedAuthors
SELECT DISTINCT role.Id, story.Username, story.Username + N'@novel.local', N'', story.Author, N'Blocked'
FROM @Stories story
CROSS JOIN dbo.Roles role
WHERE role.Name = N'Author'
  AND NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = story.Username);

INSERT dbo.Novels (AuthorId, CategoryId, Title, Description, CoverImage, Status, ReviewStatus)
SELECT author.Id, category.Id, story.Title, story.Description, story.Cover, N'Ongoing', N'Approved'
FROM @Stories story
JOIN @AddedAuthors added ON added.Username = story.Username
JOIN dbo.Users author ON author.Username = story.Username
JOIN dbo.Categories category ON category.Name = story.Category
WHERE story.Number = 1;

INSERT dbo.Chapters (NovelId, Number, Title, Content, Status, PublishedAt)
SELECT novel.Id, story.Number, story.Chapter,
    CONCAT(N'Nội dung chương ', story.Number, N' (bản mẫu thử nghiệm).'), N'Published', SYSUTCDATETIME()
FROM @Stories story
JOIN @AddedAuthors added ON added.Username = story.Username
JOIN dbo.Users author ON author.Username = story.Username
JOIN dbo.Novels novel ON novel.AuthorId = author.Id AND novel.Title = story.Title;
COMMIT;
