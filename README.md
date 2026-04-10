<div align="center">
  <h1 align="center">☕ CAFEBOOK SYSTEM 📚</h1>
  <p align="center">
    <b>Giải pháp quản lý thông minh tích hợp Nhà sách & Quán Cà phê</b>
    <br />
    <i>Sử dụng kiến trúc phân lớp, ASP.NET Core 8.0 và Trợ lý ảo AI</i>
  </p>

  <p align="center">
    <img src="https://img.shields.io/github/stars/KLTN-03-2026/GR19?style=for-the-badge&logo=github&color=orange" alt="stars">
    <img src="https://img.shields.io/github/forks/KLTN-03-2026/GR19?style=for-the-badge&logo=github&color=blue" alt="forks">
    <img src="https://img.shields.io/github/license/KLTN-03-2026/GR19?style=for-the-badge&color=green" alt="license">
    <img src="https://img.shields.io/badge/Maintained%3F-yes-brightgreen.svg?style=for-the-badge" alt="maintained">
  </p>
</div>

---

## 📌 Mục lục
* [Giới thiệu dự án](#-giới-thiệu-dự-án)
* [Tính năng chính](#-tính-năng-chính)
* [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
* [Yêu cầu môi trường](#️-yêu-cầu-môi-trường--công-cụ)
* [Cấu hình Database](#️-cấu-hình-cơ-sở-dữ-liệu-restore-database)
* [Cách khởi chạy](#-khởi-chạy-chương-trình)
* [Thông tin nhóm phát triển](#-thông-tin-nhóm-phát-triển)

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
| **Frontend Web** | ![Razor Pages](https://img.shields.io/badge/Razor_Pages-512BD4?style=flat-square&logo=dotnet&logoColor=white) ![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-38B2AC?style=flat-square&logo=tailwind-css&logoColor=white) |
| **Desktop App** | ![WPF](https://img.shields.io/badge/C%23_WPF-0078D4?style=flat-square&logo=windows&logoColor=white) |
| **Database** | ![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=flat-square&logo=microsoft-sql-server&logoColor=white) |

---

## 🛠️ Yêu cầu môi trường & Công cụ

Để dự án hoạt động ổn định, bạn cần chuẩn bị môi trường theo các bước dưới đây. Các file cài đặt đã được đính kèm sẵn trong thư mục: `Project\TaiLieu\App can cai`

Hãy tiến hành cài đặt theo thứ tự ưu tiên:

1. **Cơ sở dữ liệu (SQL Server):** Chạy file `SQL2025-SSEI-Expr.exe` (Bản Express).
2. **Công cụ lập trình (Visual Studio):** Chạy file `vs_Professional.exe`.
   * *Lưu ý quan trọng:* Tại cửa sổ cài đặt, chọn tab **Workloads**, hãy tick chọn **FULL** tất cả các mục. Việc này đảm bảo máy tính có đầy đủ SDK và thư viện để tránh lỗi thiếu dependencies khi build.
3. **Quản lý Database (SSMS):** Chạy file `vs_SSMS.exe` để cài đặt SQL Server Management Studio.

---

## 🗄️ Cấu hình Cơ sở dữ liệu (Restore Database)

Dự án sử dụng SQL Server. Dữ liệu đã được sao lưu sẵn thành file `.bak`. Thực hiện các bước sau để khôi phục:

1. Mở SQL Server Management Studio (SSMS).
2. Kết nối vào Server với tên: `localhost\SQLEXPRESS`.
3. Tại cột Object Explorer, chuột phải vào thư mục **Databases** -> Chọn **Restore Database...**
4. Tại tab General, phần Source chọn **Device** -> Nhấn nút `...` -> Chọn **Add**.
5. Trỏ đến file backup theo đường dẫn: `Project\Documents\data\CAFEBOOKDB_v2.bak`

> ⚠️ **Lưu ý quan trọng để tránh lỗi Restore:**
> * Trang **"Files"**: Tại bảng cấu hình, tìm cột `Restore As`, bạn phải chỉnh sửa lại đường dẫn của cả 2 file (data và log) trỏ trực tiếp vào thư mục: `Project\Database`.
> * Trang **"Options"**: Tick chọn 2 mục sau:
>   * `[x]` Overwrite the existing database (WITH REPLACE)
>   * `[x]` Close existing connections to destination database
>
> Nhấn **OK** và chờ thông báo thành công.

---

## 💻 Khởi chạy chương trình

Sau khi đã thiết lập xong Database, thực hiện các bước sau trong Visual Studio:

1. Mở file Solution (`.sln`) của dự án.
2. Thiết lập chạy nhiều Project cùng lúc:
   * Chuột phải vào Solution (dòng đầu tiên trong Solution Explorer) -> Chọn **Set Startup Projects...**
   * Chọn mục **Multiple startup projects**.
   * Tại cột *Action*, chuyển sang **Start** cho 3 project sau:
     1. `CafebookApi`
     2. `WebCafebookApi`
     3. `AppCafebookApi`
3. Nhấn phím **F5** hoặc nút **Start** (▶️) trên thanh công cụ để chạy toàn bộ hệ thống.

> [!TIP]
> Nếu hệ thống không kết nối được Database, hãy kiểm tra lại chuỗi `ConnectionString` trong file cấu hình của project API để đảm bảo khớp với tên Server SQL của bạn.

---

## 👥 Thông tin nhóm phát triển

**Nhóm 19 - Đồ án Tốt nghiệp KLTN 03-2026** **Giảng viên hướng dẫn:** ThS. Phạm Phú Khương

| STT | Họ và Tên | Mã Sinh Viên | Vai trò trong nhóm |
| :---: | :--- | :---: | :--- |
| 1 | **Huỳnh Ngọc Phú** | 28211106495 | Quản lý dự án (Scrum Master), Fullstack Developer |
| 2 | **Nguyễn Minh Tú** | 28211105717 | Thành viên (Frontend/Backend) |
| 3 | **Nguyễn Tú Uyên** | 28201149694 | Thành viên (Frontend/Tester) |
| 4 | **Lâm Chu Bảo Toàn** | 28211105266 | Thành viên (Backend API/AI Integration) |
| 5 | **Vương Quốc Hưng** | 28211145208 | Thành viên (WPF UI/QA) |
