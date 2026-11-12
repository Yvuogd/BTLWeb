# Truyện Chữ

Nền tảng đọc tiểu thuyết dùng ASP.NET Core MVC, EF Core và SQL Server. Độc giả có thể tìm truyện, đọc chương, lưu truyện, đánh giá và bình luận. Tác giả quản lý truyện, chương và ảnh bìa trong Studio. Quản trị viên kiểm duyệt nội dung, quản lý thể loại và khóa tài khoản.

## Chạy trên máy local

Cần .NET 10 SDK và SQL Server Express. Mở `NovelPlatform.slnx` và chạy profile `https`. Trong môi trường Development, ứng dụng mặc định kết nối với SQL Server Express trên máy bằng tài khoản Windows hiện tại. Nếu cần dùng máy chủ hoặc cơ sở dữ liệu khác, đặt cấu hình trong .NET User Secrets.

```powershell
dotnet user-secrets set --project NovelPlatform/NovelPlatform.csproj 'ConnectionStrings:NovelDb' 'Server=lpc:.\SQLEXPRESS;Database=NovelReadingPlatform;Integrated Security=True;TrustServerCertificate=True'
```

Đặt mật khẩu riêng cho các tài khoản demo bằng khóa `DemoAccounts:admin`, `DemoAccounts:tacgia` và `DemoAccounts:docgia`. Mỗi mật khẩu cần từ 12 đến 128 ký tự. Tên đăng nhập tương ứng là `admin`, `tacgia`, `docgia`. Có thể xem hoặc đổi cấu hình cục bộ qua **Manage User Secrets** trong Visual Studio. Trên máy triển khai, dùng secret store hoặc các biến môi trường `ConnectionStrings__NovelDb` và `DemoAccounts__<username>`.

Khi cập nhật từ bản cũ, ứng dụng thu hồi mật khẩu demo mặc định và kích hoạt lại ba tài khoản demo nếu đã đặt mật khẩu mới. Các tài khoản mẫu đại diện cho tác giả trên bìa chỉ dùng để gắn tên tác giả vào truyện. Khóa tài khoản thu hồi phiên đã cấp và khi mở khóa cần đăng nhập lại.

## Cơ sở dữ liệu mẫu

Ứng dụng tự tạo schema và dữ liệu khi khởi động. Có thể chạy [database/01-create-and-seed.sql](database/01-create-and-seed.sql) bằng SSMS để tạo trước cơ sở dữ liệu với cùng mười truyện. Lần khởi động đầu tiên sẽ áp dụng cấu hình tài khoản demo. Có thể chỉ khởi tạo dữ liệu bằng lệnh sau.

```powershell
dotnet run --project NovelPlatform/NovelPlatform.csproj -- --seed-only
```

Sáu truyện mới lấy tên, tác giả, thể loại và bìa từ ảnh tham chiếu, với giới thiệu viết lại và ba chương minh họa mỗi bộ. Số chương của trang nguồn không được dùng làm số chương đã đăng tại Truyện Chữ. Seed thêm truyện vào cơ sở dữ liệu đã có và không tạo lại truyện mẫu đã bị xóa qua Studio.

## API và kiểm thử

Swagger UI nằm tại `/swagger`. API công khai chỉ trả truyện đã duyệt và chương đã xuất bản.

| HTTP | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/api/novels?page=2&q=...&category=...&genre=...` | Tìm, lọc và phân trang, tối đa sáu truyện mỗi trang |
| GET | `/api/novels/{id}` | Thông tin truyện |
| GET | `/api/novels/{id}/chapters` | Danh sách chương công khai |

Xem [TEST-CASES.md](TEST-CASES.md) cho các ca kiểm thử. Ảnh bìa do người dùng tải lên được lưu trong `wwwroot/uploads/covers`. Khi triển khai cần lưu thư mục này trên bộ nhớ bền vững.

## Bản địa hóa

Giao diện hiện dùng tiếng Việt, HTML `lang="vi"`, cấu hình định dạng `vi-VN` và dữ liệu Unicode trong SQL Server. Thời điểm được lưu theo UTC và hiển thị theo múi giờ máy chủ. Với triển khai nhiều khu vực, các điểm cần bản địa hóa gồm thông báo xác thực và validation, nhãn thể loại, định dạng ngày giờ theo người đọc và quy tắc tìm kiếm có dấu. Tên truyện, tác giả và nội dung chương giữ nguyên ngôn ngữ nhập vào.
