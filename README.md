<div align="center">
  <img src="https://img.icons8.com/fluency/100/000000/coffee-beans.png" alt="Logo" width="80" height="80">
  <h1 align="center">☕ CAFEBOOK SYSTEM 📚</h1>
  <p align="center">
    <b>Giải pháp quản lý thông minh tích hợp Nhà sách & Quán Cà phê</b>
    <br />
    <i>Sử dụng công nghệ Microservices, ASP.NET Core 8.0 và Trợ lý ảo AI</i>
  </p>

  <p align="center">
    <img src="https://img.shields.io/github/stars/lamcbaotoan/HT_API_Web_App_CafeBook?style=for-the-badge&logo=github&color=orange" alt="stars">
    <img src="https://img.shields.io/github/forks/lamcbaotoan/HT_API_Web_App_CafeBook?style=for-the-badge&logo=github&color=blue" alt="forks">
    <img src="https://img.shields.io/github/license/lamcbaotoan/HT_API_Web_App_CafeBook?style=for-the-badge&color=green" alt="license">
    <img src="https://img.shields.io/badge/Maintained%3F-yes-brightgreen.svg?style=for-the-badge" alt="maintained">
  </p>
</div>

---

## 📌 Mục lục
* [Giới thiệu dự án](#-giới-thiệu-dự-án)
* [Tính năng chính](#-tính-năng-chính)
* [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
* [Yêu cầu môi trường](#-yêu-cầu-môi-trường)
* [Hướng dẫn cài đặt](#-hướng-dẫn-cài-đặt)
* [Cấu hình Database](#-cấu-hình-database)
* [Cách khởi chạy](#-cách-khởi-chạy)
* [Thông tin tác giả](#-thông-tin-tác-giả)

---

## 📖 Giới thiệu dự án

**CafeBook System** là một hệ thống quản lý toàn diện được thiết kế để giải quyết bài toán vận hành cho mô hình kinh doanh kết hợp giữa quán cà phê và hiệu sách. Hệ thống không chỉ dừng lại ở việc bán hàng mà còn tối ưu hóa trải nghiệm khách hàng thông qua **Trợ lý AI** và giúp chủ doanh nghiệp quản lý tài chính, nhân sự một cách khoa học.

> [!NOTE]
> Dự án được xây dựng với cấu trúc đa nền tảng (Web & Desktop) kết nối thông qua hệ thống API tập trung.

---

## ✨ Tính năng chính

### 🛒 Quản lý bán hàng & POS
- [x] Quản lý thực đơn đồ uống và danh mục sách.
- [x] Hệ thống đặt hàng (Order delivery) và thanh toán tại quầy.
- [x] Theo dõi trạng thái đơn hàng thời gian thực.

### 🤖 Trợ lý AI thông minh
- [x] Tư vấn chọn sách dựa trên sở thích khách hàng.
- [x] Gợi ý combo đồ uống đi kèm phù hợp.
- [x] Hỗ trợ giải đáp các thắc mắc về dịch vụ tại quán.

### 📊 Quản trị & Tài chính
- [x] Quản lý nhân sự, chấm công và tính lương tự động.
- [x] Dashboard báo cáo doanh thu theo ngày/tháng/năm.
- [x] Hệ thống tính toán và chi trả cổ tức cho cổ đông.

---

## 🛠 Công nghệ sử dụng

| Thành phần | Công nghệ |
| :--- | :--- |
| **Backend API** | ![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core_8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white) ![Entity Framework](https://img.shields.io/badge/EF_Core-512BD4?style=flat-square&logo=dotnet&logoColor=white) |
| **Frontend Web** | ![React](https://img.shields.io/badge/React-20232A?style=flat-square&logo=react&logoColor=61DAFB) ![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-38B2AC?style=flat-square&logo=tailwind-css&logoColor=white) |
| **Desktop App** | ![C# .NET WinForms](https://img.shields.io/badge/C%23_.NET_WinForms-239120?style=flat-square&logo=c-sharp&logoColor=white) |
| **Database** | ![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=flat-square&logo=microsoft-sql-server&logoColor=white) |

---

## 🛠️ Yêu cầu môi trường & Công cụ

Dự án yêu cầu các công cụ sau được cài đặt sẵn. Bạn có thể tìm thấy bộ cài tại thư mục: `Project\TaiLieu\App can cai`

1. **SQL Server Express**: Chạy file `SQL2025-SSEI-Expr.exe`.
2. **Visual Studio Professional**: Chạy file `vs_Professional.exe`.
    * *Lưu ý:* Chọn tab **Workloads** và tick **FULL** để đảm bảo không thiếu thư viện.
3. **SSMS**: Chạy file `vs_SSMS.exe` để quản lý Cơ sở dữ liệu.

---

## 🚀 Hướng dẫn cài đặt

### Bước 1: Clone dự án

git clone https://github.com/KLTN-03-2026/GR19.git

🛠️ 2. Yêu Cầu Môi Trường & Công Cụ
Để dự án hoạt động ổn định, bạn cần chuẩn bị môi trường theo các bước dưới đây. Các file cài đặt đã được đính kèm sẵn trong thư mục:

Hãy tiến hành cài đặt theo thứ tự ưu tiên:

Cơ sở dữ liệu (SQL Server): Chạy file SQL2025-SSEI-Expr.exe (Bản Express).

Công cụ lập trình (Visual Studio): Chạy file vs_Professional.exe.

Lưu ý quan trọng: Tại cửa sổ cài đặt, chọn tab Workloads, hãy tick chọn FULL tất cả các mục. Việc này đảm bảo máy tính có đầy đủ SDK và thư viện để tránh lỗi thiếu dependencies khi build.

Quản lý Database (SSMS): Chạy file vs_SSMS.exe để cài đặt SQL Server Management Studio.

🗄️ 3. Cấu Hình Cơ Sở Dữ Liệu (Restore Database)
Dự án sử dụng SQL Server. Dữ liệu đã được sao lưu sẵn thành file .bak. Thực hiện các bước sau để khôi phục:

Mở SQL Server Management Studio (SSMS).

Kết nối vào Server với tên: localhost\SQLEXPRESS.

Tại cột Object Explorer, chuột phải vào thư mục Databases -> Chọn Restore Database...

Tại tab General, phần Source chọn Device -> Nhấn nút ... -> Chọn Add.

Trỏ đến file backup theo đường dẫn: Project\Documents\data\CAFEBOOKDB_v2.bak

⚠️ Lưu ý quan trọng để tránh lỗi Restore:
Trang "Files": Tại bảng cấu hình, tìm cột Restore As, bạn phải chỉnh sửa lại đường dẫn của cả 2 file (data và log) trỏ trực tiếp vào thư mục: Project\Database.

Trang "Options": Tick chọn 2 mục sau:

[x] Overwrite the existing database (WITH REPLACE)

[x] Close existing connections to destination database

Nhấn OK và chờ thông báo thành công.

💻 4. Khởi Chạy Chương Trình
Sau khi đã thiết lập xong Database, thực hiện các bước sau trong Visual Studio:

Mở file Solution (.sln) của dự án.

Thiết lập chạy nhiều Project cùng lúc:

Chuột phải vào Solution (dòng đầu tiên trong Solution Explorer) -> Chọn Set Startup Projects...

Chọn mục Multiple startup projects.

Tại cột Action, chuyển sang Start cho 3 project sau:

Cafebookapi

webCafebookapi

appCafebookapi

Nhấn phím F5 hoặc nút Start (▶️) trên thanh công cụ để chạy toàn bộ hệ thống.

[!TIP]
Nếu hệ thống không kết nối được Database, hãy kiểm tra lại chuỗi ConnectionString trong file cấu hình của project API để đảm bảo khớp với tên Server SQL của bạn.

