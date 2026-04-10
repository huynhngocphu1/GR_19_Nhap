// Tập tin: CafebookApi/Controllers/App/KhachHangController.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.AspNetCore.Http;
using System;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/khachhang")]
    [ApiController]
    public class KhachHangController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public KhachHangController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;

            _env = env;
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if (!Directory.Exists(_env.WebRootPath))
                {
                    Directory.CreateDirectory(_env.WebRootPath);
                }
            }
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url")
                       ?? "http://127.0.0.1:5166";
        }

        // === 5 HÀM HELPER XỬ LÝ FILE ===
        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        private string GenerateFileName(int id, string ten)
        {
            string slug = SlugifyUtil.GenerateSlug(ten);
            return $"{id}_{slug}.jpg";
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        private void DeleteOldImage(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            var fileName = relativePath.TrimStart('/');
            var fullPath = Path.Combine(_env.WebRootPath, fileName.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        private void RenameImage(string oldRelativePath, string newRelativePath)
        {
            if (string.IsNullOrEmpty(oldRelativePath) || string.IsNullOrEmpty(newRelativePath) || oldRelativePath == newRelativePath)
                return;
            var oldFullPath = Path.Combine(_env.WebRootPath, oldRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            var newFullPath = Path.Combine(_env.WebRootPath, newRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(oldFullPath))
            {
                System.IO.File.Move(oldFullPath, newFullPath);
            }
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        private async Task<string> SaveImageFromFile(IFormFile file, string fileName, string relativeDir)
        {
            var saveDir = Path.Combine(_env.WebRootPath, relativeDir);
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }
            var fullSavePath = Path.Combine(saveDir, fileName);
            await using (var stream = new FileStream(fullSavePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/{relativeDir.Replace(Path.DirectorySeparatorChar, '/')}/{fileName}";
        }
        // ===================================

        /// <summary>
        /// API Lấy danh sách Khách hàng
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchKhachHang(
            [FromQuery] string? searchText,
            [FromQuery] bool? biKhoa)
        {
            var query = _context.KhachHangs.AsQueryable();

            if (biKhoa.HasValue)
            {
                query = query.Where(kh => kh.BiKhoa == biKhoa.Value);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                string searchLower = searchText.ToLower();
                query = query.Where(kh =>
                    kh.HoTen.ToLower().Contains(searchLower) ||
                    (kh.SoDienThoai != null && kh.SoDienThoai.Contains(searchLower)) ||
                    (kh.Email != null && kh.Email.ToLower().Contains(searchLower))
                );
            }

            var results = await query
                .Select(kh => new KhachHangDto
                {
                    IdKhachHang = kh.IdKhachHang,
                    HoTen = kh.HoTen,
                    SoDienThoai = kh.SoDienThoai,
                    Email = kh.Email,
                    NgayTao = kh.NgayTao,
                    BiKhoa = kh.BiKhoa
                })
                .OrderBy(kh => kh.HoTen)
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// SỬA: API Lấy chi tiết và lịch sử
        /// </summary>
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetKhachHangDetails(int id)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            var dto = new KhachHangDetailDto
            {
                IdKhachHang = kh.IdKhachHang,
                HoTen = kh.HoTen,
                SoDienThoai = kh.SoDienThoai,
                Email = kh.Email,
                DiaChi = kh.DiaChi,
                DiemTichLuy = kh.DiemTichLuy,
                TenDangNhap = kh.TenDangNhap,
                BiKhoa = kh.BiKhoa,
                AnhDaiDienUrl = GetFullImageUrl(kh.AnhDaiDien), // <-- SỬA

                LichSuDonHang = await _context.HoaDons
                    .Where(h => h.IdKhachHang == id)
                    .OrderByDescending(h => h.ThoiGianTao)
                    .Select(h => new LichSuDonHangDto
                    {
                        IdHoaDon = h.IdHoaDon,
                        ThoiGianTao = h.ThoiGianTao,
                        ThanhTien = h.ThanhTien,
                        TrangThai = h.TrangThai
                    }).ToListAsync(),

                LichSuThueSach = await _context.PhieuThueSachs
                    .Where(p => p.IdKhachHang == id)
                    .Include(p => p.ChiTietPhieuThues)
                        .ThenInclude(ct => ct.Sach)
                    .OrderByDescending(p => p.NgayThue)
                    .SelectMany(p => p.ChiTietPhieuThues.Select(ct => new LichSuThueSachDto
                    {
                        IdPhieuThue = p.IdPhieuThueSach,
                        TieuDeSach = ct.Sach.TenSach,
                        NgayThue = p.NgayThue,
                        TrangThai = p.TrangThai
                    }))
                    .Take(20)
                    .ToListAsync()
            };
            return Ok(dto);
        }

        /// <summary>
        /// SỬA: API Thêm khách hàng mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateKhachHang([FromForm] KhachHangUpdateRequestDto dto) // <-- SỬA [FromForm]
        {
            if (string.IsNullOrEmpty(dto.HoTen) || string.IsNullOrEmpty(dto.SoDienThoai))
            {
                return BadRequest("Họ tên và SĐT là bắt buộc.");
            }
            if (await _context.KhachHangs.AnyAsync(kh => kh.SoDienThoai == dto.SoDienThoai))
            {
                return Conflict("Số điện thoại này đã tồn tại.");
            }
            if (!string.IsNullOrEmpty(dto.Email) && await _context.KhachHangs.AnyAsync(kh => kh.Email == dto.Email))
            {
                return Conflict("Email này đã tồn tại.");
            }

            var khachHang = new KhachHang
            {
                HoTen = dto.HoTen,
                SoDienThoai = dto.SoDienThoai,
                Email = dto.Email,
                DiaChi = dto.DiaChi,
                TenDangNhap = dto.TenDangNhap,
                DiemTichLuy = 0,
                NgayTao = DateTime.Now,
                BiKhoa = false,
                AnhDaiDien = null // SỬA: Tạm thời null
            };
            _context.KhachHangs.Add(khachHang);
            await _context.SaveChangesAsync(); // Lưu lần 1 để lấy ID

            // SỬA: Thêm logic lưu file
            if (dto.AnhDaiDienUpload != null)
            {
                try
                {
                    string fileName = GenerateFileName(khachHang.IdKhachHang, khachHang.HoTen);
                    string relativePath = await SaveImageFromFile(dto.AnhDaiDienUpload, fileName, HinhAnhPaths.UrlAvatarKH.TrimStart('/'));
                    khachHang.AnhDaiDien = relativePath;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _context.KhachHangs.Remove(khachHang);
                    await _context.SaveChangesAsync();
                    return StatusCode(500, $"Lỗi khi lưu ảnh: {ex.Message}");
                }
            }

            return Ok(khachHang);
        }

        /// <summary>
        /// SỬA: API Cập nhật khách hàng
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateKhachHang(int id, [FromForm] KhachHangUpdateRequestDto dto) // <-- SỬA [FromForm]
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            if (await _context.KhachHangs.AnyAsync(k => k.SoDienThoai == dto.SoDienThoai && k.IdKhachHang != id))
            {
                return Conflict("Số điện thoại này đã tồn tại.");
            }
            if (!string.IsNullOrEmpty(dto.Email) && await _context.KhachHangs.AnyAsync(k => k.Email == dto.Email && k.IdKhachHang != id))
            {
                return Conflict("Email này đã tồn tại.");
            }

            // SỬA: Logic xử lý file
            var oldImagePath = kh.AnhDaiDien;
            var oldTen = kh.HoTen;
            bool isImageUpload = dto.AnhDaiDienUpload != null;
            bool isNameChange = dto.HoTen != oldTen;

            // Cập nhật text
            kh.HoTen = dto.HoTen;
            kh.SoDienThoai = dto.SoDienThoai;
            kh.Email = dto.Email;
            kh.DiaChi = dto.DiaChi;
            kh.TenDangNhap = dto.TenDangNhap;
            kh.DiemTichLuy = dto.DiemTichLuy;

            // Xử lý file
            string newFileName = GenerateFileName(kh.IdKhachHang, kh.HoTen);
            string newRelativePath = $"/{HinhAnhPaths.UrlAvatarKH.TrimStart('/')}/{newFileName}";

            if (isImageUpload)
            {
                DeleteOldImage(oldImagePath);
                // === SỬA LỖI CS8604 (Dòng 282) ===
                // Thêm '!' vì logic 'isImageUpload' đã đảm bảo file không null
                kh.AnhDaiDien = await SaveImageFromFile(dto.AnhDaiDienUpload!, newFileName, HinhAnhPaths.UrlAvatarKH.TrimStart('/'));
            }
            else if (dto.XoaAnhDaiDien)
            {
                DeleteOldImage(oldImagePath);
                kh.AnhDaiDien = null;
            }
            else if (isNameChange && !string.IsNullOrEmpty(oldImagePath))
            {
                RenameImage(oldImagePath, newRelativePath);
                kh.AnhDaiDien = newRelativePath;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // Helper class cho API UpdateStatus
        public class UpdateStatusRequest
        {
            public bool BiKhoa { get; set; }
        }

        /// <summary>
        /// SỬA: API Khóa/Mở khóa tài khoản
        /// </summary>
        [HttpPut("update-status/{id}")]
        public async Task<IActionResult> UpdateKhachHangStatus(int id, [FromBody] UpdateStatusRequest request) // <-- SỬA: Nhận Object
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            kh.BiKhoa = request.BiKhoa; // <-- SỬA: Lấy từ object
            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// SỬA: API Xóa khách hàng
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKhachHang(int id)
        {
            // Kiểm tra ràng buộc
            if (await _context.HoaDons.AnyAsync(h => h.IdKhachHang == id))
            {
                return Conflict("Không thể xóa. Khách hàng này đã có lịch sử Hóa đơn.");
            }
            if (await _context.PhieuThueSachs.AnyAsync(p => p.IdKhachHang == id))
            {
                return Conflict("Không thể xóa. Khách hàng này đã có lịch sử Thuê sách.");
            }

            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            // SỬA: Thêm xóa file
            DeleteOldImage(kh.AnhDaiDien);

            _context.KhachHangs.Remove(kh);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}