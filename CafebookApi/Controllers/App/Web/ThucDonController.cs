// Tập tin: CafebookApi/Controllers/Web/ThucDonController.cs
using CafebookApi.Data;
using CafebookModel.Model.ModelWeb;
using CafebookModel.Model.ModelApp; // For FilterLookupDto
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/thucdon")]
    [ApiController]
    public class ThucDonController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        // (Constructor và GetFullImageUrl, GetFilters giữ nguyên)
        public ThucDonController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
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

        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            var danhMucs = await _context.DanhMucs
                .OrderBy(d => d.TenDanhMuc)
                .Select(d => new FilterLookupDto { Id = d.IdDanhMuc, Ten = d.TenDanhMuc })
                .ToListAsync();
            return Ok(danhMucs);
        }

        /// <summary>
        /// API tìm kiếm, lọc và phân trang Thực đơn
        /// </summary>
        [HttpGet("search")]
        // SỬA LỖI CS1737: Di chuyển các tham số optional (có giá trị mặc định) xuống cuối
        public async Task<IActionResult> Search(
            [FromQuery] int? loaiId,
            [FromQuery] string? search,
            [FromQuery] decimal? giaMin,  // <-- ĐÃ DI CHUYỂN LÊN
            [FromQuery] decimal? giaMax,  // <-- ĐÃ DI CHUYỂN LÊN
            [FromQuery] string sortBy = "ten_asc", // <-- ĐÃ DI CHUYỂN XUỐNG
            [FromQuery] int pageNum = 1,
            [FromQuery] int pageSize = 9)
        {
            var query = _context.SanPhams
                .Include(s => s.DanhMuc)
                .Where(s => s.TrangThaiKinhDoanh == true);

            // Lọc
            if (loaiId.HasValue && loaiId > 0)
                query = query.Where(s => s.IdDanhMuc == loaiId);

            if (giaMin.HasValue)
                query = query.Where(s => s.GiaBan >= giaMin);

            if (giaMax.HasValue)
                query = query.Where(s => s.GiaBan <= giaMax);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.TenSanPham.Contains(search));

            // Sắp xếp
            query = sortBy switch
            {
                "ten_desc" => query.OrderByDescending(s => s.TenSanPham),
                "gia_asc" => query.OrderBy(s => s.GiaBan),
                "gia_desc" => query.OrderByDescending(s => s.GiaBan),
                _ => query.OrderBy(s => s.TenSanPham), // Mặc định ten_asc
            };

            // Phân trang
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Bước 1: Lấy dữ liệu thô (gồm path)
            var items_raw = await query
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new {
                    s.IdSanPham,
                    s.TenSanPham,
                    TenLoaiSP = s.DanhMuc.TenDanhMuc,
                    s.GiaBan,
                    s.HinhAnh
                })
                .ToListAsync();

            // Bước 2: Chuyển đổi sang DTO (gọi GetFullImageUrl)
            var items_dto = items_raw.Select(s => new SanPhamThucDonDto
            {
                IdSanPham = s.IdSanPham,
                TenSanPham = s.TenSanPham,
                TenLoaiSP = s.TenLoaiSP,
                DonGia = s.GiaBan,
                AnhSanPhamUrl = GetFullImageUrl(s.HinhAnh)
            }).ToList();

            var result = new ThucDonDto
            {
                Items = items_dto,
                TotalPages = totalPages,
                CurrentPage = pageNum
            };
            return Ok(result);
        }

        /// <summary>
        /// API lấy chi tiết 1 sản phẩm (gồm định lượng)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var sp = await _context.SanPhams
                .Include(s => s.DanhMuc)
                .Include(s => s.DinhLuongs)
                    .ThenInclude(d => d.NguyenLieu)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSanPham == id);

            if (sp == null)
                return NotFound(new { Message = "Không tìm thấy sản phẩm." });

            // THÊM MỚI: Lấy danh sách gợi ý
            var suggestions_raw = await _context.DeXuatSanPhams
                .Where(d => d.IdSanPhamGoc == id) // Tìm các món được gợi ý TỪ món này
                .Include(d => d.SanPhamDeXuat)    // Lấy thông tin của món ĐƯỢC gợi ý
                .OrderByDescending(d => d.DoLienQuan)
                .Take(4) // Lấy 4 món
                .Select(d => d.SanPhamDeXuat) // Chỉ chọn sản phẩm được gợi ý
                .ToListAsync();

            // Chuyển đổi gợi ý sang DTO (chạy trong C#)
            var suggestions_dto = suggestions_raw.Select(s => new SanPhamThucDonDto
            {
                IdSanPham = s.IdSanPham,
                TenSanPham = s.TenSanPham,
                TenLoaiSP = null, // Không cần thiết cho card gợi ý
                DonGia = s.GiaBan,
                AnhSanPhamUrl = GetFullImageUrl(s.HinhAnh)
            }).ToList();


            var dto = new SanPhamChiTietDto
            {
                IdSanPham = sp.IdSanPham,
                TenSanPham = sp.TenSanPham,
                TenLoaiSP = sp.DanhMuc?.TenDanhMuc,
                DonGia = sp.GiaBan,
                HinhAnhUrl = GetFullImageUrl(sp.HinhAnh),
                MoTa = sp.MoTa,
                CongThucs = sp.DinhLuongs.Select(d => new CongThucDto
                {
                    TenNguyenLieu = d.NguyenLieu.TenNguyenLieu
                }).ToList(),

                // Gán danh sách gợi ý
                GoiY = suggestions_dto
            };
            return Ok(dto);
        }
    }
}
    
