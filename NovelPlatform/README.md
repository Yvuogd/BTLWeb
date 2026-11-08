# Truyện Chữ — Nền tảng đọc tiểu thuyết

Website ASP.NET Core MVC được dựng theo yêu cầu trong báo cáo BTL LTWEB. Giao diện lấy cảm hứng từ bố cục trang đọc truyện tham khảo, dùng thương hiệu Truyện Chữ. Dự án dùng SQL Server Express (`localhost\SQLEXPRESS`), EF Core và khởi tạo cơ sở dữ liệu `NovelReadingPlatform` cùng dữ liệu mẫu khi chạy lần đầu. Bìa truyện là các minh họa SVG tự tạo trong `wwwroot/images`.

## Mở bằng Visual Studio

Mở `NovelPlatform.slnx` bằng **Visual Studio 2022/18 Community**, chọn cấu hình `https` hoặc `http`, rồi nhấn **F5**. Gói EF Core SQL Server 10.0.12 được khai báo trong project. Nếu Visual Studio cần restore package, để restore hoàn tất trước khi chạy.

Ứng dụng kết nối tới `localhost\SQLEXPRESS`. Cần có SQL Server Express chạy trên máy. Khi khởi động, ứng dụng tạo database/schema nếu chưa tồn tại và nạp dữ liệu mẫu. Có thể chạy `database/01-create-and-seed.sql` trong SSMS thay cho bước khởi tạo của ứng dụng; chạy script trước thì database sẽ được tạo sẵn với cùng bộ dữ liệu demo.

## Tài khoản mẫu

| Vai trò | Tên đăng nhập | Mật khẩu |
|---|---|---|
| Độc giả | `docgia` | `DocGia123!` |
| Tác giả | `tacgia` | `TacGia123!` |
| Quản trị viên | `admin` | `Admin123!` |

Tài khoản đăng ký mới có vai trò Độc giả. Dữ liệu mẫu gồm 4 truyện theo các ảnh bìa được cung cấp, tất cả ở trạng thái đang ra; mỗi truyện có từ 3 đến 5 chương với nội dung mẫu ngắn. Bộ dữ liệu cũng có dấu trang, điểm đánh giá và bình luận minh họa.

## Quy trình đăng và kiểm duyệt

- Tác giả tạo truyện và gửi chương từ **Studio tác giả**. Truyện/chương mới có trạng thái chờ duyệt nên chưa hiển thị cho độc giả.
- Quản trị viên mở **Quản trị**, xem thông tin truyện hoặc nội dung chương rồi chọn **Duyệt và đăng** hoặc **Từ chối**.
- Quản trị viên có thể tạo truyện và đăng chương trực tiếp từ Studio; nội dung do Admin tạo được xuất bản ngay.
- Truyện chỉ xuất hiện công khai khi được duyệt; chương chỉ đọc được sau khi được duyệt và xuất bản.

## Các chức năng

- Tìm kiếm truyện theo tên, tác giả hoặc thể loại; lọc thể loại.
- Xem trang truyện, đọc chương, chuyển chương trước/sau.
- Đăng ký/đăng nhập, lưu truyện, đánh giá 1–5 sao, viết bình luận.
- Studio tác giả: tạo truyện, thêm chương, xem trạng thái duyệt.
- Quản trị: duyệt/từ chối truyện và chương, khóa/mở tài khoản, thêm thể loại và gỡ bình luận.

Schema gồm các bảng Roles, Users, Categories, Novels, Chapters, Bookmarks, Ratings và Comments; có khóa ngoại, unique constraints cho tài khoản/chương, khóa kép cho đánh dấu/đánh giá và kiểm tra điểm 1–5.
