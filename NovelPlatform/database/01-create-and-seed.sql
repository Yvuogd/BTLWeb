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
    N'ABEiM0RVZneImaq7zN3u/w==.RLD9xhWfRQQuLym4F+xJBbpv5NukL08d42pIFetlr+s=',
    N'Độc giả Mây',
    N'Active'),
    
(2,
    N'tacgia',
    N'author@novel.local',
    N'ABEiM0RVZneImaq7zN3u/w==.HY6igPvmbePjbYr4VaCkLSID80q0flLOAtwsgSNwH2g=',
    N'Minh An',
    N'Active'),
    
(3,
    N'admin',
    N'admin@novel.local',
    N'ABEiM0RVZneImaq7zN3u/w==.ZOyVcn8qHSut/8K/16htQXRG2Id7zVlAfCUp4r2G724=',
    N'Quản trị viên',
    N'Active'),

(2,
    N'tacgia-ran-nuoc',
    N'tacgia-ran-nuoc@novel.local',
    N'ABEiM0RVZneImaq7zN3u/w==.HY6igPvmbePjbYr4VaCkLSID80q0flLOAtwsgSNwH2g=',
    N'Tương Công Nạp Cá Thiếp Ba',
    N'Active'),

(2,
    N'tacgia-dai-duong',
    N'tacgia-dai-duong@novel.local',
    N'ABEiM0RVZneImaq7zN3u/w==.HY6igPvmbePjbYr4VaCkLSID80q0flLOAtwsgSNwH2g=',
    N'Truy Cần Thời Gian Đích Tiểu Oa Ngư',
    N'Active'),

(2,
    N'tacgia-tay-mon',
    N'tacgia-tay-mon@novel.local',
    N'ABEiM0RVZneImaq7zN3u/w==.HY6igPvmbePjbYr4VaCkLSID80q0flLOAtwsgSNwH2g=',
    N'Đạo Tâm Trường Thanh',
    N'Active'),

(2,
    N'tacgia-hai-dao',
    N'tacgia-hai-dao@novel.local',
    N'ABEiM0RVZneImaq7zN3u/w==.HY6igPvmbePjbYr4VaCkLSID80q0flLOAtwsgSNwH2g=',
    N'Lão Tưởng Trù Yên',
    N'Active');

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
