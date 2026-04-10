using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/quanlydanhgia")]
    [ApiController]
    //[Authorize(Roles = "Quản trị viên")] // Đã sửa từ "QuanLy"
    public class QuanLyDanhGiaController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly string _baseUrl;

        public QuanLyDanhGiaController(CafebookDbContext context, IConfiguration config)
        {
            _context = context;
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://127.0.0.1:5166";
        }

        private string? GetFullImageUrl(string? relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl))
                return null;
            return $"{_baseUrl}{relativeUrl}";
        }

        // GET: api/app/quanlydanhgia
        [HttpGet]
        public async Task<IActionResult> GetReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] int? filterSao = null, [FromQuery] string? trangThai = null)
        {
            var query = _context.DanhGias.AsQueryable();

            if (filterSao.HasValue)
            {
                query = query.Where(d => d.SoSao == filterSao.Value);
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(d => d.TrangThai == trangThai);
            }

            var totalItems = await query.CountAsync();

            // === SỬA LỖI INTERNAL SERVER ERROR TẠI ĐÂY ===

            // BƯỚC 1: Lấy dữ liệu thô từ CSDL (chạy bằng SQL)
            var rawReviews = await query
                .Include(d => d.KhachHang)
                .Include(d => d.SanPham)
                .Include(d => d.PhanHoiDanhGias).ThenInclude(p => p.NhanVien)
                .OrderByDescending(d => d.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new // Tạo đối tượng tạm
                {
                    d.idDanhGia,
                    d.idSanPham, // <-- ĐÃ THÊM
                    TenKhachHang = d.KhachHang != null ? d.KhachHang.HoTen : "Khách ẩn",
                    TenSanPham = d.idSanPham != null ? d.SanPham.TenSanPham : "(Đánh giá chung)",
                    d.SoSao,
                    d.BinhLuan,
                    HinhAnhURL_Raw = d.HinhAnhURL, // Lấy URL tương đối
                    d.NgayTao,
                    d.TrangThai,
                    PhanHoi = d.PhanHoiDanhGias.Select(p => new PhanHoiQuanLyDto
                    {
                        TenNhanVien = p.NhanVien.HoTen,
                        NoiDung = p.NoiDung,
                        NgayTao = p.NgayTao
                    }).FirstOrDefault()
                })
                .ToListAsync(); // <-- Thực thi SQL tại đây, dữ liệu đã nằm trong C# memory

            // BƯỚC 2: Xử lý dữ liệu bằng C# (chạy trong C# memory)
            var finalReviews = rawReviews.Select(d => new DanhGiaQuanLyDto
            {
                IdDanhGia = d.idDanhGia,
                IdSanPham = d.idSanPham, // <-- ĐÃ THÊM
                TenKhachHang = d.TenKhachHang,
                TenSanPham = d.TenSanPham,
                SoSao = d.SoSao,
                BinhLuan = d.BinhLuan,
                HinhAnhUrl = GetFullImageUrl(d.HinhAnhURL_Raw), // <-- Gọi hàm C# ở đây (an toàn)
                NgayTao = d.NgayTao,
                TrangThai = d.TrangThai,
                PhanHoi = d.PhanHoi
            }).ToList(); // .ToList() để hoàn thành việc chuyển đổi

            // === KẾT THÚC SỬA LỖI ===

            var result = new PaginatedResult<DanhGiaQuanLyDto>
            {
                Items = finalReviews, // Trả về danh sách đã xử lý
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                CurrentPage = page
            };

            return Ok(result);
        }

        // (Các hàm Post và Put còn lại giữ nguyên)

        // POST: api/app/quanlydanhgia/5/reply
        [HttpPost("{idDanhGia}/reply")]
        public async Task<IActionResult> ReplyToReview(int idDanhGia, [FromBody] PhanHoiInputDto input)
        {
            var idNhanVienClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idNhanVienClaim, out int idNhanVien))
            {
                // Giả định IdNhanVien = 1 nếu không có token (để test)
                // Trong thực tế, bạn nên trả về:
                // return Unauthorized("Token nhân viên không hợp lệ.");
                idNhanVien = 1; // CHỈ ĐỂ TEST - HÃY XÓA SAU
            }

            var danhGia = await _context.DanhGias.FindAsync(idDanhGia);
            if (danhGia == null) return NotFound("Không tìm thấy đánh giá.");

            var existingReply = await _context.PhanHoiDanhGias.FirstOrDefaultAsync(p => p.idDanhGia == idDanhGia);
            if (existingReply != null)
            {
                existingReply.NoiDung = input.NoiDung;
                existingReply.idNhanVien = idNhanVien;
                existingReply.NgayTao = DateTime.Now;
            }
            else
            {
                var phanHoi = new PhanHoiDanhGia
                {
                    idDanhGia = idDanhGia,
                    idNhanVien = idNhanVien,
                    NoiDung = input.NoiDung,
                    NgayTao = DateTime.Now
                };
                _context.PhanHoiDanhGias.Add(phanHoi);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Phản hồi thành công" });
        }

        // PUT: api/app/quanlydanhgia/5/toggle-status
        [HttpPut("{idDanhGia}/toggle-status")]
        public async Task<IActionResult> ToggleReviewStatus(int idDanhGia)
        {
            var danhGia = await _context.DanhGias.FindAsync(idDanhGia);
            if (danhGia == null) return NotFound("Không tìm thấy đánh giá.");

            danhGia.TrangThai = (danhGia.TrangThai == "Hiển thị") ? "Đã ẩn" : "Hiển thị";

            await _context.SaveChangesAsync();
            return Ok(new { newStatus = danhGia.TrangThai });
        }

        [HttpGet("search-products")]
        public async Task<IActionResult> SearchProducts([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return Ok(new List<ProductSearchResultDto>());
            }

            var products = await _context.SanPhams
                .Where(p => p.TenSanPham.Contains(query))
                .Select(p => new ProductSearchResultDto
                {
                    IdSanPham = p.IdSanPham,
                    TenSanPham = p.TenSanPham
                })
                .Take(10) // Giới hạn 10 kết quả
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("product-stats")]
        public async Task<IActionResult> GetProductStats([FromQuery] int productId)
        {
            var stats = await _context.DanhGias
                .Where(d => d.idSanPham == productId && d.TrangThai == "Hiển thị")
                .GroupBy(d => d.idSanPham)
                .Select(g => new ProductStatsDto
                {
                    TotalReviews = g.Count(),
                    AverageRating = g.Average(d => d.SoSao)
                })
                .FirstOrDefaultAsync();

            if (stats == null)
            {
                // Nếu sản phẩm chưa có đánh giá nào
                return Ok(new ProductStatsDto { TotalReviews = 0, AverageRating = 0 });
            }

            return Ok(stats);
        }
    }
}