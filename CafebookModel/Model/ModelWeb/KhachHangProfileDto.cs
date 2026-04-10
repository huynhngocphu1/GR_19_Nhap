// Tập tin: CafebookModel/Model/ModelWeb/KhachHangProfileDto.cs
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace CafebookModel.Model.ModelWeb
{
    // ... (Các DTO KhachHangTongQuanDto, KhachHangProfileDto, ProfileUpdateModel, PasswordChangeModel giữ nguyên) ...

    /// <summary>
    /// DTO cho trang TỔNG QUAN
    /// </summary>
    public class KhachHangTongQuanDto
    {
        public int DiemTichLuy { get; set; }
        public int TongHoaDon { get; set; }
        public decimal TongChiTieu { get; set; }
        public DateTime NgayTao { get; set; }
    }

    /// <summary>
    /// DTO dùng để TẢI thông tin trang HỒ SƠ
    /// </summary>
    public class KhachHangProfileDto
    {
        public int IdKhachHang { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
        public string? TenDangNhap { get; set; }
        public string? AnhDaiDienUrl { get; set; }
    }

    /// <summary>
    /// Model dùng để CẬP NHẬT thông tin hồ sơ
    /// </summary>
    public class ProfileUpdateModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string HoTen { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? SoDienThoai { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        public string? DiaChi { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(100, ErrorMessage = "Tên đăng nhập phải dài từ 6 đến 100 ký tự.", MinimumLength = 6)]
        public string TenDangNhap { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model dùng để ĐỔI MẬT KHẨU
    /// </summary>
    public class PasswordChangeModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu cũ")]
        [DataType(DataType.Password)]
        public string MatKhauCu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, ErrorMessage = "Mật khẩu mới phải dài ít nhất 6 ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string MatKhauMoi { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu mới và mật khẩu xác nhận không khớp.")]
        public string XacNhanMatKhauMoi { get; set; } = string.Empty;
    }

    // ==========================================================
    // === DTO CHO LỊCH SỬ ĐƠN HÀNG (Đã di chuyển vào đây) ===
    // ==========================================================

    /// <summary>
    /// DTO cho một món hàng trong thẻ lịch sử (Trang List & Trang Detail)
    /// </summary>
    public class DonHangItemWebDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string? HinhAnhUrl { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => DonGia * SoLuong;
    }

    /// <summary>
    /// DTO cho một thẻ (card) trên trang LỊCH SỬ (List)
    /// </summary>
    public class LichSuDonHangWebDto
    {
        public int IdHoaDon { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public string TrangThaiThanhToan { get; set; } = string.Empty;
        public string? TrangThaiGiaoHang { get; set; }
        public decimal ThanhTien { get; set; }
        public List<DonHangItemWebDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO MỚI: Cho một sự kiện trong timeline vận chuyển
    /// </summary>
    public class TrackingEventDto
    {
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCurrent { get; set; } = false;
    }

    /// <summary>
    /// DTO MỚI: DTO chính cho trang CHI TIẾT ĐƠN HÀNG
    /// </summary>
    public class DonHangChiTietWebDto
    {
        // Thông tin cơ bản
        public int IdHoaDon { get; set; }
        public string MaDonHang { get; set; } = string.Empty; // "HD00097"
        public string TrangThaiGiaoHang { get; set; } = string.Empty;
        public string TrangThaiThanhToan { get; set; } = string.Empty;
        public DateTime ThoiGianTao { get; set; }

        // Địa chỉ
        public string HoTen { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string DiaChiGiaoHang { get; set; } = string.Empty;

        // Timeline
        public List<TrackingEventDto> TrackingEvents { get; set; } = new();

        // Món hàng
        public List<DonHangItemWebDto> Items { get; set; } = new();

        // Thanh toán
        public decimal TongTienHang { get; set; } // (Tổng tiền gốc)
        public decimal GiamGia { get; set; } // (GiamGia + Diem)
        public decimal ThanhTien { get; set; }
        public string PhuongThucThanhToan { get; set; } = string.Empty;
        public string? AnhXacNhanGiaoHangUrl { get; set; }
    }
}