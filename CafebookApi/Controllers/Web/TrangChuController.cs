// Tập tin: CafebookApi/Controllers/Web/TrangChuController.cs
using CafebookApi.Data;
using CafebookModel.Model.ModelWeb;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
namespace CafebookApi.Controllers.Web
{
    [Route("api/web/trangchu")]
    [ApiController]
    public class TrangChuController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        // (Constructor và GetFullImageUrl giữ nguyên)
        public TrangChuController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://127.0.0.1:5166";
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }

        [HttpGet("data")] // API endpoint
        public async Task<IActionResult> GetTrangChuData()
        {
            // 1. Lấy thông tin chung (từ bảng CaiDat)
            var settings = await _context.CaiDats
                .Where(c => c.TenCaiDat == "TenQuan" ||
c.TenCaiDat == "GioiThieu" ||
                            c.TenCaiDat == "BannerImage" ||
c.TenCaiDat == "DiaChi" ||
                            c.TenCaiDat == "SoDienThoai" ||
c.TenCaiDat == "LienHe_Email" ||
c.TenCaiDat == "LienHe_GioMoCua")
                .ToListAsync();
            var thongTinChung = new ThongTinChungDto
            {
                TenQuan = settings.FirstOrDefault(c => c.TenCaiDat == "TenQuan")?.GiaTri ?? "Cafebook",
                GioiThieu = settings.FirstOrDefault(c => c.TenCaiDat == "GioiThieu")?.GiaTri,
                BannerImageUrl = GetFullImageUrl(settings.FirstOrDefault(c => c.TenCaiDat == "BannerImage")?.GiaTri),
                DiaChi = settings.FirstOrDefault(c => c.TenCaiDat == "DiaChi")?.GiaTri,
                SoDienThoai = settings.FirstOrDefault(c => c.TenCaiDat == "SoDienThoai")?.GiaTri,
                EmailLienHe = settings.FirstOrDefault(c => c.TenCaiDat == "LienHe_Email")?.GiaTri,
                GioMoCua = settings.FirstOrDefault(c => c.TenCaiDat == "LienHe_GioMoCua")?.GiaTri,
                SoBanTrong = await _context.Bans.CountAsync(b => b.TrangThai == "Trống"),
                SoSachSanSang = await _context.Sachs.CountAsync(s => s.SoLuongHienCo > 0)
            };

            // 2. Lấy 3 khuyến mãi (Đang hoạt động)
            var promotions = await _context.KhuyenMais
                .Where(km => km.TrangThai == "Hoạt động" && km.NgayBatDau <= DateTime.Now && km.NgayKetThuc >= DateTime.Now)
                .OrderBy(km => km.NgayBatDau)
                .Take(3)
.Select(km => new KhuyenMaiDto
{
    TenKhuyenMai = km.TenChuongTrinh,
    MoTa = km.MoTa,
    dieuKienApDung = km.DieuKienApDung
}).ToListAsync();

            // 3. Lấy 5 món nổi bật
            var monNoiBat_Raw = await _context.SanPhams
                .Where(sp => sp.TrangThaiKinhDoanh == true)
                .OrderByDescending(sp => sp.IdSanPham)
                .Take(5)
.Select(sp => new
{
    sp.IdSanPham,
    sp.TenSanPham,
    sp.GiaBan,
    sp.HinhAnh
}).ToListAsync();

            var monNoiBat = monNoiBat_Raw.Select(sp => new SanPhamDto
            {
                IdSanPham = sp.IdSanPham,
                TenSanPham = sp.TenSanPham,
                DonGia = sp.GiaBan,
                AnhSanPhamUrl = GetFullImageUrl(sp.HinhAnh)
            }).ToList();

            // ======================================
            // === SỬA LỖI LINQ TẠI ĐÂY ===
            // ======================================

            // 4. Lấy 4 sách nổi bật
            // BƯỚC 1: Lấy dữ liệu thô (bao gồm Tác giả)
            var sachNoiBat_Raw = await _context.Sachs
                .Include(s => s.SachTacGias).ThenInclude(stg => stg.TacGia) // Tải Tác giả
                .OrderByDescending(s => s.SoLuongHienCo)
                .Take(4)
                .Select(s => new // Chọn các trường cần thiết (vẫn là IQueryable)
                {
                    s.IdSach,
                    s.TenSach,
                    // Lấy CẢ collection Tác giả
                    TacGias = s.SachTacGias.Select(stg => stg.TacGia.TenTacGia),
                    s.AnhBia
                }).ToListAsync(); // <-- Chạy SQL tại đây

            // BƯỚC 2: Dùng C# (string.Join) để tạo DTO
            var sachNoiBat = sachNoiBat_Raw.Select(s => new SachDto
            {
                IdSach = s.IdSach,
                TieuDe = s.TenSach,
                TacGia = string.Join(", ", s.TacGias), // <-- string.Join chạy trong C#
                AnhBiaUrl = GetFullImageUrl(s.AnhBia)
            }).ToList();

            // ======================================
            // === KẾT THÚC SỬA LỖI ===
            // ======================================

            // 5. Gộp tất cả vào DTO
            var dto = new TrangChuDto
            {
                Info = thongTinChung,
                Promotions = promotions,
                MonNoiBat = monNoiBat,
                SachNoiBat = sachNoiBat
            };
            return Ok(dto);
        }
    }
}