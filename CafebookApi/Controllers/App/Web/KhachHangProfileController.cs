using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/profile")]
    [ApiController]
    //[Authorize(Roles = "KhachHang")] // <-- BẢO MẬT: Yêu cầu đăng nhập cho tất cả
    public class KhachHangProfileController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public KhachHangProfileController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://127.0.0.1:5166";
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            int.TryParse(idClaim?.Value, out int id);
            return id;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private async Task<string?> SaveImageAsync(IFormFile imageFile, string subFolder, string baseFileNameSlug, int userId)
        {
            if (imageFile == null || imageFile.Length == 0) return null;
            var uploadPath = Path.Combine(_env.WebRootPath, "images", subFolder);
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            var fileExtension = Path.GetExtension(imageFile.FileName);
            var uniqueFileName = $"{userId}_{baseFileNameSlug}{fileExtension}";
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return $"/images/{subFolder}/{uniqueFileName}".Replace(Path.DirectorySeparatorChar, '/');
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private void DeleteImage(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        [HttpGet("overview/{id}")]
        public async Task<IActionResult> GetOverview(int id)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            var hoaDons = await _context.HoaDons
                .Where(hd => hd.IdKhachHang == id && hd.TrangThai == "Đã thanh toán")
                .ToListAsync();

            var dto = new KhachHangTongQuanDto
            {
                DiemTichLuy = kh.DiemTichLuy,
                NgayTao = kh.NgayTao,
                TongHoaDon = hoaDons.Count,
                TongChiTieu = hoaDons.Sum(hd => hd.ThanhTien) // Sửa: Dùng ThanhTien
            };
            return Ok(dto);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            var dto = new KhachHangProfileDto
            {
                IdKhachHang = kh.IdKhachHang,
                HoTen = kh.HoTen,
                SoDienThoai = kh.SoDienThoai,
                Email = kh.Email,
                DiaChi = kh.DiaChi,
                TenDangNhap = kh.TenDangNhap,
                AnhDaiDienUrl = GetFullImageUrl(kh.AnhDaiDien)
            };
            return Ok(dto);
        }

        [HttpPut("update-info/{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromForm] ProfileUpdateModel model, IFormFile? avatarFile)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            if (avatarFile != null)
            {
                DeleteImage(kh.AnhDaiDien);
                string baseSlug = SlugifyUtil.GenerateSlug(model.HoTen);
                kh.AnhDaiDien = await SaveImageAsync(avatarFile, "avatars/avatarKH", baseSlug, id);
            }

            kh.HoTen = model.HoTen;
            kh.SoDienThoai = model.SoDienThoai;
            kh.Email = model.Email;
            kh.DiaChi = model.DiaChi;
            kh.TenDangNhap = model.TenDangNhap; // Đã thêm

            await _context.SaveChangesAsync();
            return Ok(new { newAvatarUrl = GetFullImageUrl(kh.AnhDaiDien) });
        }

        [HttpPost("change-password/{id}")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] PasswordChangeModel model)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            if (kh.MatKhau != model.MatKhauCu)
            {
                return BadRequest(new { Message = "Mật khẩu cũ không chính xác." });
            }

            kh.MatKhau = model.MatKhauMoi;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đổi mật khẩu thành công." });
        }

        [HttpGet("booking-history/{id}")]
        public async Task<IActionResult> GetBookingHistory(int id)
        {
            var bookings = await _context.PhieuDatBans
                .Include(p => p.Ban)
                .Where(p => p.IdKhachHang == id)
                .OrderByDescending(p => p.ThoiGianDat)
                .Select(p => new LichSuDatBanDto
                {
                    IdPhieuDatBan = p.IdPhieuDatBan,
                    TenBan = p.Ban.SoBan, // Sửa từ "TenBan" thành "SoBan"
                    ThoiGianDat = p.ThoiGianDat,
                    SoLuongKhach = p.SoLuongKhach,
                    TrangThai = p.TrangThai,
                    GhiChu = p.GhiChu
                })
                .ToListAsync();

            if (bookings == null)
            {
                return Ok(new List<LichSuDatBanDto>());
            }

            return Ok(bookings);
        }

        [HttpGet("rental-history/{id}")]
        public async Task<IActionResult> GetRentalHistory(int id)
        {
            var rentals = await _context.PhieuThueSachs
                .Include(p => p.ChiTietPhieuThues)
                .Include(p => p.PhieuTraSachs)
                .Where(p => p.IdKhachHang == id)
                .OrderByDescending(p => p.NgayThue)
                .Select(p => new LichSuPhieuThueDto
                {
                    IdPhieuThueSach = p.IdPhieuThueSach,
                    NgayThue = p.NgayThue,
                    TrangThai = p.TrangThai,
                    SoLuongSach = p.ChiTietPhieuThues.Count(),
                    TongTienCoc = p.TongTienCoc,
                    NgayTra = p.PhieuTraSachs.FirstOrDefault() != null ? (DateTime?)p.PhieuTraSachs.FirstOrDefault().NgayTra : null,
                    TongPhiThue = p.PhieuTraSachs.FirstOrDefault() != null ? (decimal?)p.PhieuTraSachs.FirstOrDefault().TongPhiThue : null,
                    TongTienPhat = p.PhieuTraSachs.FirstOrDefault() != null ? (decimal?)p.PhieuTraSachs.FirstOrDefault().TongTienPhat : null,
                    TongTienCocHoan = p.PhieuTraSachs.FirstOrDefault() != null ? (decimal?)p.PhieuTraSachs.FirstOrDefault().TongTienCocHoan : null
                })
                .ToListAsync();

            if (rentals == null)
            {
                return Ok(new List<LichSuPhieuThueDto>());
            }

            return Ok(rentals);
        }

        // ==========================================================
        // === SỬA LỖI 500 API TẠI ĐÂY ===
        // ==========================================================
        [HttpGet("order-history/{id}")]
        public async Task<IActionResult> GetOrderHistory(int id)
        {
            // 1. Tải dữ liệu thô từ CSDL (Không gọi GetFullImageUrl)
            var orders_raw = await _context.HoaDons
                .Include(h => h.ChiTietHoaDons).ThenInclude(ct => ct.SanPham)
                .Where(h => h.IdKhachHang == id && h.LoaiHoaDon == "Giao hàng")
                .OrderByDescending(h => h.ThoiGianTao)
                .Select(h => new // Dùng đối tượng tạm
                {
                    h.IdHoaDon,
                    h.ThoiGianTao,
                    h.TrangThai, // TrangThaiThanhToan
                    h.TrangThaiGiaoHang,
                    h.ThanhTien,
                    Items = h.ChiTietHoaDons.Select(ct => new
                    {
                        ct.IdSanPham,
                        ct.SanPham.TenSanPham,
                        HinhAnhRaw = ct.SanPham.HinhAnh, // Lấy đường dẫn thô
                        ct.SoLuong,
                        ct.DonGia
                    }).ToList()
                })
                .ToListAsync(); // <-- Thực thi SQL ở đây

            // 2. Chuyển đổi sang DTO (Lúc này đã ở trong C#)
            var orders_dto = orders_raw.Select(h => new LichSuDonHangWebDto
            {
                IdHoaDon = h.IdHoaDon,
                ThoiGianTao = h.ThoiGianTao,
                TrangThaiThanhToan = h.TrangThai,
                TrangThaiGiaoHang = h.TrangThaiGiaoHang,
                ThanhTien = h.ThanhTien,
                Items = h.Items.Select(ct => new DonHangItemWebDto
                {
                    IdSanPham = ct.IdSanPham,
                    TenSanPham = ct.TenSanPham,
                    HinhAnhUrl = GetFullImageUrl(ct.HinhAnhRaw), // <-- Gọi hàm C# an toàn
                    SoLuong = ct.SoLuong,
                    DonGia = ct.DonGia
                }).ToList()
            }).ToList();

            return Ok(orders_dto);
        }

        // ==========================================================
        // === YÊU CẦU MỚI: API CHO TRANG CHI TIẾT ĐƠN HÀNG ===
        // ==========================================================
        [HttpGet("order-detail/{id}")]
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            var idKhachHang = GetCurrentUserId();
            if (idKhachHang == 0) return Unauthorized();

            var hoaDon = await _context.HoaDons
                .Include(h => h.ChiTietHoaDons).ThenInclude(ct => ct.SanPham)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.IdHoaDon == id && h.IdKhachHang == idKhachHang);

            if (hoaDon == null)
            {
                return NotFound("Không tìm thấy đơn hàng hoặc bạn không có quyền xem.");
            }

            // Giả lập lịch sử vận chuyển
            var trackingEvents = new List<TrackingEventDto>();
            string currentStatus = hoaDon.TrangThaiGiaoHang ?? "Chờ xác nhận";

            // 1. Đặt hàng
            trackingEvents.Add(new TrackingEventDto
            {
                Timestamp = hoaDon.ThoiGianTao,
                Status = "Đơn hàng đã đặt",
                Description = "Đơn hàng của bạn đã được đặt thành công."
            });

            // 2. Xác nhận
            if (currentStatus != "Chờ xác nhận")
            {
                trackingEvents.Add(new TrackingEventDto
                {
                    Timestamp = hoaDon.ThoiGianTao.AddMinutes(15), // Giả lập
                    Status = "Đơn hàng được xác nhận",
                    Description = "Shop đang chuẩn bị hàng."
                });
            }

            // 3. Giao ĐVVC
            if (currentStatus == "Đang giao" || currentStatus == "Hoàn thành")
            {
                trackingEvents.Add(new TrackingEventDto
                {
                    Timestamp = hoaDon.ThoiGianTao.AddMinutes(45), // Giả lập
                    Status = "Đã giao cho ĐVVC",
                    Description = "Đơn hàng đã được bàn giao cho đơn vị vận chuyển."
                });
            }

            // 4. Hoàn thành
            if (currentStatus == "Hoàn thành")
            {
                trackingEvents.Add(new TrackingEventDto
                {
                    Timestamp = hoaDon.ThoiGianThanhToan ?? hoaDon.ThoiGianTao.AddHours(2), // Giả lập
                    Status = "Giao hàng thành công",
                    Description = "Đơn hàng đã được giao đến bạn.",
                    IsCurrent = true // Đây là trạng thái cuối
                });
            }
            else
            {
                // Đánh dấu trạng thái hiện tại
                var lastEvent = trackingEvents.LastOrDefault();
                if (lastEvent != null) lastEvent.IsCurrent = true;
            }

            string? anhGiaoHangUrl = null;
            if (!string.IsNullOrEmpty(hoaDon.GhiChu) && hoaDon.GhiChu.Contains("Ảnh xác nhận:"))
            {
                // Cấu trúc ghi chú: "... | Ảnh xác nhận: /images/anhgiaohang/..."
                var parts = hoaDon.GhiChu.Split(new[] { "Ảnh xác nhận:" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    string rawPath = parts.Last().Trim();
                    anhGiaoHangUrl = GetFullImageUrl(rawPath);
                }
            }

            var dto = new DonHangChiTietWebDto
            {
                IdHoaDon = hoaDon.IdHoaDon,
                MaDonHang = $"HD{hoaDon.IdHoaDon:D6}",
                TrangThaiGiaoHang = hoaDon.TrangThaiGiaoHang ?? "Chờ xác nhận",
                TrangThaiThanhToan = hoaDon.TrangThai,
                ThoiGianTao = hoaDon.ThoiGianTao,

                // Lấy thông tin từ HĐ
                HoTen = hoaDon.KhachHang?.HoTen ?? "Khách hàng",
                SoDienThoai = hoaDon.SoDienThoaiGiaoHang ?? hoaDon.KhachHang?.SoDienThoai ?? "N/A",
                DiaChiGiaoHang = hoaDon.DiaChiGiaoHang ?? hoaDon.KhachHang?.DiaChi ?? "N/A",

                TrackingEvents = trackingEvents.OrderBy(t => t.Timestamp).ToList(),

                Items = hoaDon.ChiTietHoaDons.Select(ct => new DonHangItemWebDto
                {
                    IdSanPham = ct.IdSanPham,
                    TenSanPham = ct.SanPham.TenSanPham,
                    HinhAnhUrl = GetFullImageUrl(ct.SanPham.HinhAnh),
                    SoLuong = ct.SoLuong,
                    DonGia = ct.DonGia
                }).ToList(),

                TongTienHang = hoaDon.TongTienGoc,
                GiamGia = hoaDon.GiamGia, // Gộp cả KM + Điểm
                ThanhTien = hoaDon.ThanhTien,
                PhuongThucThanhToan = hoaDon.PhuongThucThanhToan ?? "N/A",
                AnhXacNhanGiaoHangUrl = anhGiaoHangUrl
            };

            return Ok(dto);
        }
    }
}