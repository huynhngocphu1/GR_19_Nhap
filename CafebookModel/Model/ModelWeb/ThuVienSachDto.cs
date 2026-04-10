// Tập tin: CafebookModel/Model/ModelWeb/ThuVienSachDto.cs
using CafebookModel.Model.ModelApp; // Dùng chung FilterLookupDto
using System.Collections.Generic;

namespace CafebookModel.Model.ModelWeb
{
    // DTO cho các thẻ sách (dùng chung)
    public class SachCardDto
    {
        public int IdSach { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string? TacGia { get; set; } // Dùng cho trang thư viện chính
        public decimal GiaBia { get; set; }
        public int SoLuongCoSan { get; set; }
        public string? AnhBiaUrl { get; set; }
    }

    // DTO cho Tác giả (để hiển thị link)
    public class TacGiaDto
    {
        public int IdTacGia { get; set; }
        public string TenTacGia { get; set; } = string.Empty;
    }

    // DTO cho Thể loại (để hiển thị link)
    public class TheLoaiDto
    {
        public int IdTheLoai { get; set; }
        public string TenTheLoai { get; set; } = string.Empty;
    }

    // DTO cho Nhà xuất bản (để hiển thị link)
    public class NhaXuatBanDto
    {
        public int IdNhaXuatBan { get; set; }
        public string TenNhaXuatBan { get; set; } = string.Empty;
    }

    // DTO cho trang Chi Tiết Sách (ĐÃ SỬA)
    public class SachChiTietDto
    {
        public int IdSach { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public decimal GiaBia { get; set; }
        public string? AnhBiaUrl { get; set; }
        public string? MoTa { get; set; }
        public string? ViTri { get; set; }
        public int TongSoLuong { get; set; }
        public int SoLuongCoSan { get; set; }

        // === SỬA: Đổi từ string sang List DTO ===
        public List<TacGiaDto> TacGias { get; set; } = new();
        public List<TheLoaiDto> TheLoais { get; set; } = new();
        public List<NhaXuatBanDto> NhaXuatBans { get; set; } = new();

        public List<SachCardDto> GoiY { get; set; } = new();
    }

    // DTO cho trang Kết quả tìm kiếm (MỚI)
    public class SachKetQuaTimKiemDto
    {
        public string TieuDeTrang { get; set; } = "Tất cả Sách của";
        public string? MoTaTrang { get; set; }
        public List<SachCardDto> SachList { get; set; } = new List<SachCardDto>();
    }

    /// <summary>
    /// DTO chứa kết quả trả về (bao gồm phân trang)
    /// </summary>
    public class SachPhanTrangDto
    {
        public List<SachCardDto> Items { get; set; } = new();
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }
    /// <summary>
    /// DTO chứa các bộ lọc (filters) cho trang thư viện
    /// </summary>
    public class SachFiltersDto
    {
        public List<FilterLookupDto> TheLoais { get; set; } = new();
    }
}