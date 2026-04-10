// Tập tin: CafebookModel/Model/ModelWeb/ThucDonDto.cs
using CafebookModel.Model.ModelApp; // Để dùng chung FilterLookupDto
using System.Collections.Generic;

namespace CafebookModel.Model.ModelWeb
{
    /// <summary>
    /// DTO cho một sản phẩm hiển thị trên lưới thực đơn
    /// </summary>
    public class SanPhamThucDonDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string? TenLoaiSP { get; set; } // Tên danh mục
        public decimal DonGia { get; set; }
        public string? AnhSanPhamUrl { get; set; } // Sửa: Dùng URL
    }

    /// <summary>
    /// DTO chứa kết quả trả về (bao gồm phân trang)
    /// </summary>
    public class ThucDonDto
    {
        public List<SanPhamThucDonDto> Items { get; set; } = new();
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }

    /// <summary>
    /// DTO cho trang Chi Tiết Sản Phẩm
    /// </summary>
    public class SanPhamChiTietDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string? TenLoaiSP { get; set; }
        public decimal DonGia { get; set; }
        public string? HinhAnhUrl { get; set; }
        public string? MoTa { get; set; }
        public List<CongThucDto> CongThucs { get; set; } = new();

        // THÊM MỚI: Danh sách sản phẩm gợi ý
        public List<SanPhamThucDonDto> GoiY { get; set; } = new();
    }

    /// <summary>
    /// DTO con cho công thức/định lượng
    /// </summary>
    public class CongThucDto
    {
        public string TenNguyenLieu { get; set; } = string.Empty;
    }
}