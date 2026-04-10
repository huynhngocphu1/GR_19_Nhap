// Tập tin: CafebookApi/Controllers/Web/WebQuanLyController.cs
using CafebookApi.Data;
using CafebookModel.Model.ModelWeb;
using CafebookModel.Utils; // THÊM
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting; // THÊM
using Microsoft.Extensions.Configuration; // THÊM
using System.IO; // THÊM
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/quanly")]
    [ApiController]
    public class WebQuanLyController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        // --- THÊM MỚI (Giống các controller khác) ---
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public WebQuanLyController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://127.0.0.1:5166";
        }

        // --- THÊM MỚI: Hàm helper ---
        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }
        // -------------------------

        [HttpGet("dashboard/{idNhanVien}")]
        public async Task<IActionResult> GetEmployeeDashboard(int idNhanVien)
        {
            var nhanVien = await _context.NhanViens
                .Include(nv => nv.VaiTro)
                .AsNoTracking()
                .FirstOrDefaultAsync(nv => nv.IdNhanVien == idNhanVien);
            if (nhanVien == null)
            {
                return NotFound("Không tìm thấy nhân viên.");
            }

            // Lấy ca làm việc hôm nay
            var caLamViec = await _context.LichLamViecs
                .Include(llv => llv.CaLamViec)
                .Where(llv => llv.IdNhanVien == idNhanVien && llv.NgayLam == DateTime.Today)
                .Select(llv => llv.CaLamViec.TenCa)
                .FirstOrDefaultAsync();
            // Lấy thống kê
            var banPhucVu = await _context.Bans.CountAsync(b => b.TrangThai == "Có khách");
            var donXuLy = await _context.HoaDons.CountAsync(h => h.TrangThai == "Chưa thanh toán" || h.TrangThai == "Đang giao");

            var dto = new WebQuanLyDashboardDto
            {
                HoTen = nhanVien.HoTen,
                VaiTro = nhanVien.VaiTro.TenVaiTro,
                // SỬA: Trả về URL thay vì Base64
                AnhDaiDienUrl = GetFullImageUrl(nhanVien.AnhDaiDien),
                CaHienTai = caLamViec ?? "Hôm nay nghỉ",
                TongBanDangPhucVu = banPhucVu,
                TongDonDangXuLy = donXuLy
            };
            return Ok(dto);
        }
    }
}