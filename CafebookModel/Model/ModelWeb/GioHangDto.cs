// Tập tin: CafebookModel/Model/ModelWeb/GioHangDto.cs
using System.Collections.Generic;
using System.Linq;

namespace CafebookModel.Model.ModelWeb
{
    /// <summary>
    /// DTO cơ bản đại diện cho một món hàng trong giỏ.
    /// SỬA: Đã xóa "Loai". Giờ đây Id mặc định là IdSanPham.
    /// </summary>
    public class CartItemDto
    {
        public int Id { get; set; } // IdSanPham
        public int SoLuong { get; set; }
    }

    /// <summary>
    /// Model đầy đủ thông tin của một món hàng ĐỂ HIỂN THỊ ra View.
    /// SỬA: Đã xóa "Loai".
    /// </summary>
    public class GioHangItemViewModel
    {
        public int Id { get; set; }
        public string TenHienThi { get; set; } = string.Empty;
        public string? HinhAnhUrl { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien => DonGia * SoLuong;
    }

    /// <summary>
    /// ViewModel chính cho trang GioHangView (Không đổi).
    /// </summary>
    public class GioHangViewModel
    {
        public List<GioHangItemViewModel> Items { get; set; } = new();
        public decimal TongTien => Items.Sum(item => item.ThanhTien);
    }
}