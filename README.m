Bước 1 để bạn có thể copy toàn bộ đoạn dưới đây và dán thẳng vào GitHub một cách mượt mà nhất:
# Hướng Dẫn Cài Đặt Và Triển Khai Dự Án CafeBook Từ A - Z

## 1. Tải Mã Nguồn (Clone Code)
Mở Terminal, Command Prompt hoặc Git Bash tại thư mục bạn muốn lưu dự án và chạy lệnh dưới đây để tải code về máy:

```bash
git clone [https://github.com/lamcbaotoan/HT_API_Web_App_CafeBook.git](https://github.com/lamcbaotoan/HT_API_Web_App_CafeBook.git)

2. Cài Đặt Môi Trường & Công Cụ
Các file setup phần mềm cần thiết đã được đính kèm sẵn bên trong thư mục của project.
Đường dẫn thư mục chứa file cài đặt: Project\TaiLieu\App can cai
Trong thư mục này có 3 file, bạn cần tiến hành cài đặt đầy đủ theo thứ tự sau:
Cài đặt cơ sở dữ liệu (SQL Server): Chạy file SQL2025-SSEI-Expr.exe để cài đặt SQL Server Express.
Cài đặt công cụ lập trình (Visual Studio): Chạy file vs_Professional.exe.
Tại cửa sổ cài đặt, chọn tab Workloads, bạn hãy tick chọn FULL tất cả các mục (cài ALL). Việc này đảm bảo máy tính có đầy đủ SDK và thư viện để dự án không bị lỗi thiếu dependencies.
Nhấn nút Install (hoặc Modify nếu đã cài trước đó) và chờ quá trình cài đặt hoàn tất.
Cài đặt công cụ quản lý Database (SSMS): Chạy file vs_SSMS.exe và nhấn Install để cài đặt SQL Server Management Studio.

3. Cấu Hình Cơ Sở Dữ Liệu (Restore Database)
Dự án sử dụng SQL Server. Dữ liệu đã được backup sẵn thành file .bak. Bạn cần làm theo các bước sau để tạo database:
Mở phần mềm SQL Server Management Studio (SSMS).
Tại hộp thoại kết nối, nhập thông tin Server name là: localhost\SQLEXPRESS và nhấn Connect.
Ở cửa sổ Object Explorer (cột bên trái), click chuột phải vào thư mục Databases -> Chọn Restore Database...
Tại tab General, phần Source chọn Device -> Nhấn nút ... -> Chọn Add.
Tìm đến file backup theo đường dẫn sau trong project và nhấn OK:
👉 Project\TaiLieu\code\CAFEBOOKDB_v2.bak
⚠️ Quan trọng - Chỉnh sửa cấu hình Restore để tránh lỗi:
Chuyển sang trang "Files" (cột menu bên trái): Tại bảng cấu hình, tìm cột Restore As, bạn phải chỉnh sửa lại đường dẫn của cả 2 file (data và log) trỏ thẳng vào thư mục: Project\Database.
Chuyển sang trang "Options" (cột menu bên trái): Tick chọn 2 mục sau để tránh các lỗi ghi đè hoặc kẹt tiến trình:
[v] Overwrite the existing database (WITH REPLACE)
[v] Close existing connections to destination database
Nhấn OK và chờ hệ thống báo Restore thành công.

4. Khởi Chạy Chương Trình
Sau khi đã thiết lập xong Database, bạn tiến hành chạy dự án theo các bước sau:
Mở file Solution (.sln) của dự án bằng phần mềm Visual Studio.
Cửa sổ Solution Explorer (thường nằm bên phải), click chuột phải vào tên Solution trên cùng -> Chọn Set Startup Projects...
Trong cửa sổ hiện ra, tick chọn mục Multiple startup projects.
Tại cột Action, bạn click vào menu thả xuống và đổi thành Start cho 3 project sau:
Cafebookapi
webCafebookapi
appCafebookapi
Nhấn OK để lưu lại thiết lập.
Cuối cùng, nhấn nút Start (biểu tượng ▶️) hoặc phím F5 trên thanh công cụ của Visual Studio để chạy toàn bộ chương trình.
