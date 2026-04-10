using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp.NhanVien.DatBan;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
//using System.Net.Http; // <-- THÊM MỚI

namespace AppCafebookApi.Controllers.app.NhanVien
{
    [Route("api/app/datban")]
    [ApiController]
    public class DatBanController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IConfiguration _config;
        //private readonly IHttpClientFactory _clientFactory; // <-- THÊM MỚI

        private const int ReservationSlotHours = 2;
        private const int ReservationBufferMinutes = 5;

        private class OpeningHours
        {
            public TimeSpan Open { get; set; } = new TimeSpan(6, 0, 0);
            public TimeSpan Close { get; set; } = new TimeSpan(23, 0, 0);
            public bool IsValid { get; set; } = false;
        }

        // SỬA HÀM KHỞI TẠO
        public DatBanController(CafebookDbContext context, IConfiguration config, IHttpClientFactory clientFactory)
        {
            _context = context;
            _config = config;
            //_clientFactory = clientFactory; // <-- THÊM MỚI
        }

        // *** SỬA: XÓA HOÀN TOÀN HÀM AutoCancelLateReservationsAsync() KHỎI ĐÂY ***

        #region GET Endpoints
        [HttpGet("list")]
        public async Task<ActionResult<IEnumerable<PhieuDatBanDto>>> GetDatBanList()
        {
            // === SỬA: GỌI HÀM NỘI BỘ TRỰC TIẾP (NHANH & CHÍNH XÁC) ===
            await AutoCancelLateReservationsInternal();
            // === KẾT THÚC SỬA ===

            var list = await _context.PhieuDatBans
                .Include(p => p.KhachHang)
                .Include(p => p.Ban).ThenInclude(b => b.KhuVuc)
                .OrderByDescending(p => p.ThoiGianDat)
                .Select(p => new PhieuDatBanDto
                {
                    IdPhieuDatBan = p.IdPhieuDatBan,
                    TenKhachHang = p.KhachHang != null ? p.KhachHang.HoTen : p.HoTenKhach,
                    SoDienThoai = p.KhachHang != null ? p.KhachHang.SoDienThoai : p.SdtKhach,
                    Email = p.KhachHang != null ? p.KhachHang.Email : null,
                    IdBan = p.IdBan,
                    SoBan = p.Ban.SoBan,
                    TenKhuVuc = p.Ban.KhuVuc != null ? p.Ban.KhuVuc.TenKhuVuc : "N/A",
                    ThoiGianDat = p.ThoiGianDat,
                    SoLuongKhach = p.SoLuongKhach,
                    TrangThai = p.TrangThai,
                    GhiChu = p.GhiChu,
                    IdKhachHang = p.IdKhachHang
                }).ToListAsync();
            return Ok(list);
        }

        [HttpGet("available-bans")]
        public async Task<ActionResult<IEnumerable<BanDatBanDto>>> GetAvailableBans()
        {
            var bans = await _context.Bans
                .Include(b => b.KhuVuc)
                .Where(b => b.TrangThai == "Trống" || b.TrangThai == "Đã đặt")
                .Select(b => new BanDatBanDto
                {
                    IdBan = b.IdBan,
                    SoBan = b.SoBan,
                    TenKhuVuc = b.KhuVuc != null ? b.KhuVuc.TenKhuVuc : "N/A",
                    SoGhe = b.SoGhe,
                    IdKhuVuc = b.IdKhuVuc
                }).ToListAsync();
            return Ok(bans);
        }

        [HttpGet("search-customer")]
        public async Task<ActionResult<IEnumerable<KhachHangLookupDto>>> SearchCustomer([FromQuery] string query)
        {
            // SỬA DÒNG NÀY:
            if (string.IsNullOrEmpty(query)) //
            // THAY VÌ DÒNG CŨ: if (string.IsNullOrEmpty(query) || query.Length < 3)
            {
                return Ok(new List<KhachHangLookupDto>());
            }
            var queryLower = query.ToLower(); // Sửa: Thêm .ToLower()
            var results = await _context.KhachHangs
                .Where(kh => (kh.SoDienThoai != null && kh.SoDienThoai.Contains(query)) ||
                             (kh.Email != null && kh.Email.ToLower().Contains(queryLower)) || // Sửa: Thêm .ToLower()
                             kh.HoTen.ToLower().Contains(queryLower)) // Sửa: Thêm .ToLower()
                .Select(kh => new KhachHangLookupDto
                {
                    IdKhachHang = kh.IdKhachHang,
                    HoTen = kh.HoTen,
                    SoDienThoai = kh.SoDienThoai ?? "",
                    Email = kh.Email
                })
                .Take(10).ToListAsync();
            return Ok(results);
        }

        [HttpGet("opening-hours")]
        public async Task<ActionResult<string>> GetOpeningHours()
        {
            var setting = await _context.CaiDats
                .FirstOrDefaultAsync(cd => cd.TenCaiDat == "LienHe_GioMoCua");
            if (setting == null || string.IsNullOrEmpty(setting.GiaTri))
            {
                return Ok("06:00 - 23:00");
            }
            var match = Regex.Match(setting.GiaTri, @"(\d{2}:\d{2})\s*-\s*(\d{2}:\d{2})");
            if (match.Success)
            {
                return Ok(match.Value);
            }
            return Ok("06:00 - 23:00");
        }
        #endregion

        #region POST/PUT Endpoints (Logic chính)

        [HttpPost("create-staff")]
        public async Task<IActionResult> CreateDatBanStaff(PhieuDatBanCreateUpdateDto dto)
        {
            KhachHang khachHang = null;

            // SỬA ĐỔI: Thay thế hoàn toàn logic IsKhachVangLai
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. XỬ LÝ KHÁCH HÀNG (Logic "Tìm hoặc Tạo")
                if (string.IsNullOrWhiteSpace(dto.TenKhachHang) || string.IsNullOrWhiteSpace(dto.SoDienThoai))
                {
                    return BadRequest("Tên khách hàng và Số điện thoại là bắt buộc.");
                }

                string? sdt = dto.SoDienThoai;
                string? email = dto.Email;

                if (!string.IsNullOrWhiteSpace(sdt))
                {
                    khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.SoDienThoai == sdt);
                }
                if (khachHang == null && !string.IsNullOrWhiteSpace(email))
                {
                    khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Email == email);
                }

                if (khachHang == null) // Tạo mới
                {
                    if (!string.IsNullOrWhiteSpace(sdt) && await _context.KhachHangs.AnyAsync(k => k.SoDienThoai == sdt))
                    {
                        return Conflict("Số điện thoại này đã tồn tại.");
                    }
                    if (!string.IsNullOrWhiteSpace(email) && await _context.KhachHangs.AnyAsync(k => k.Email == email))
                    {
                        return Conflict("Email này đã tồn tại.");
                    }

                    string tenDangNhap;
                    if (!string.IsNullOrWhiteSpace(sdt)) { tenDangNhap = sdt; }
                    else if (!string.IsNullOrWhiteSpace(email)) { tenDangNhap = email; }
                    else { tenDangNhap = $"temp_{Guid.NewGuid().ToString("N")[..12]}"; }

                    var newKH = new KhachHang
                    {
                        HoTen = dto.TenKhachHang,
                        SoDienThoai = sdt,
                        Email = email,
                        NgayTao = DateTime.Now,
                        DiemTichLuy = 0,
                        BiKhoa = false,
                        TenDangNhap = tenDangNhap,
                        MatKhau = "123456", // Mật khẩu mặc định
                        TaiKhoanTam = true
                    };
                    _context.KhachHangs.Add(newKH);
                    await _context.SaveChangesAsync(); // Lưu KH để lấy ID
                    khachHang = newKH;
                }
                else // Cập nhật khách tìm thấy
                {
                    khachHang.HoTen = dto.TenKhachHang;
                    khachHang.Email = email; // Cập nhật Email nếu có
                }

                // 2. XỬ LÝ ĐẶT BÀN (Giữ nguyên)
                var ban = await _context.Bans.FindAsync(dto.IdBan);
                if (ban == null) return BadRequest("Bàn không tồn tại.");
                if (ban.TrangThai == "Có khách") return BadRequest("Bàn đang có khách, không thể đặt.");

                var openingHours = await GetAndParseOpeningHours();
                if (!IsTimeValid(dto.ThoiGianDat, openingHours))
                {
                    return BadRequest($"Giờ đặt ({dto.ThoiGianDat:HH:mm}) nằm ngoài giờ mở cửa ({openingHours.Open:hh\\:mm} - {openingHours.Close:hh\\:mm}).");
                }
                if (dto.SoLuongKhach > ban.SoGhe)
                {
                    return BadRequest($"Số lượng khách ({dto.SoLuongKhach}) vượt quá số ghế của bàn ({ban.SoGhe}).");
                }

                // Logic kiểm tra xung đột (chồng chéo slot + 5 phút đệm)
                DateTime newSlotStart = dto.ThoiGianDat;
                DateTime newSlotEnd = dto.ThoiGianDat.AddHours(ReservationSlotHours);

                var conflict = await _context.PhieuDatBans
                    .Where(p => p.IdBan == dto.IdBan &&
                                (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận"))
                    .ToListAsync();

                foreach (var p in conflict)
                {
                    DateTime existingSlotStart = p.ThoiGianDat.AddMinutes(-ReservationBufferMinutes);
                    DateTime existingSlotEnd = p.ThoiGianDat.AddHours(ReservationSlotHours).AddMinutes(ReservationBufferMinutes);

                    if (newSlotStart < existingSlotEnd && existingSlotStart < newSlotEnd)
                    {
                        return Conflict($"Xung đột! Bàn này đã có phiếu đặt lúc {p.ThoiGianDat:HH:mm} (có 5 phút đệm).");
                    }
                }

                // 3. TẠO PHIẾU
                var phieu = new PhieuDatBan
                {
                    IdKhachHang = khachHang?.IdKhachHang, // Gán ID khách hàng đã tìm/tạo
                    IdBan = dto.IdBan,
                    HoTenKhach = dto.TenKhachHang, // Luôn lưu tên/SĐT từ form (Snapshot)
                    SdtKhach = dto.SoDienThoai,
                    ThoiGianDat = dto.ThoiGianDat,
                    SoLuongKhach = dto.SoLuongKhach,
                    GhiChu = dto.GhiChu,
                    TrangThai = dto.TrangThai
                };
                _context.PhieuDatBans.Add(phieu);
               //ban.TrangThai = "Đã đặt";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Gửi email
                var emailNguoiNhan = khachHang?.Email ?? dto.Email;
                if (!string.IsNullOrEmpty(emailNguoiNhan))
                {
                    var khachInfo = new KhachHang { HoTen = dto.TenKhachHang, Email = emailNguoiNhan };
                    _ = SendConfirmationEmailAsync(phieu, khachInfo, ban.SoBan);
                }
                return Ok(new { idPhieuDatBan = phieu.IdPhieuDatBan });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message} \nChi tiết: {ex.InnerException?.Message}");
            }
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateDatBan(int id, PhieuDatBanCreateUpdateDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var phieu = await _context.PhieuDatBans
                    .Include(p => p.KhachHang)
                    .Include(p => p.Ban)
                    .FirstOrDefaultAsync(p => p.IdPhieuDatBan == id);

                if (phieu == null) return NotFound("Không tìm thấy phiếu đặt.");

                var openingHours = await GetAndParseOpeningHours();
                if (!IsTimeValid(dto.ThoiGianDat, openingHours))
                {
                    return BadRequest($"Giờ đặt ({dto.ThoiGianDat:HH:mm}) nằm ngoài giờ mở cửa ({openingHours.Open:hh\\:mm} - {openingHours.Close:hh\\:mm}).");
                }

                string oldTrangThai = phieu.TrangThai;

                // SỬA ĐỔI: Logic "Find or Create" tương tự như Create
                KhachHang khachHang = null;
                if (string.IsNullOrWhiteSpace(dto.TenKhachHang) || string.IsNullOrWhiteSpace(dto.SoDienThoai))
                {
                    return BadRequest("Tên khách hàng và Số điện thoại là bắt buộc.");
                }
                string? sdt = dto.SoDienThoai;
                string? email = dto.Email;

                if (!string.IsNullOrWhiteSpace(sdt)) { khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.SoDienThoai == sdt); }
                if (khachHang == null && !string.IsNullOrWhiteSpace(email)) { khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Email == email); }

                if (khachHang == null) // Tạo mới
                {
                    if (!string.IsNullOrWhiteSpace(sdt) && await _context.KhachHangs.AnyAsync(k => k.SoDienThoai == sdt)) { return Conflict("Số điện thoại này đã tồn tại."); }
                    if (!string.IsNullOrWhiteSpace(email) && await _context.KhachHangs.AnyAsync(k => k.Email == email)) { return Conflict("Email này đã tồn tại."); }

                    string tenDangNhap;
                    if (!string.IsNullOrWhiteSpace(sdt)) { tenDangNhap = sdt; }
                    else if (!string.IsNullOrWhiteSpace(email)) { tenDangNhap = email; }
                    else { tenDangNhap = $"temp_{Guid.NewGuid().ToString("N")[..12]}"; }

                    var newKH = new KhachHang
                    {
                        HoTen = dto.TenKhachHang,
                        SoDienThoai = sdt,
                        Email = email,
                        NgayTao = DateTime.Now,
                        DiemTichLuy = 0,
                        BiKhoa = false,
                        TenDangNhap = tenDangNhap,
                        MatKhau = "123456",
                        TaiKhoanTam = true
                    };
                    _context.KhachHangs.Add(newKH);
                    await _context.SaveChangesAsync();
                    khachHang = newKH;
                }
                else // Cập nhật
                {
                    khachHang.HoTen = dto.TenKhachHang;
                    khachHang.Email = email;
                }
                // Kết thúc SỬA ĐỔI

                phieu.IdKhachHang = khachHang?.IdKhachHang;
                phieu.HoTenKhach = dto.TenKhachHang;
                phieu.SdtKhach = dto.SoDienThoai;

                if (phieu.IdBan != dto.IdBan)
                {
                    var oldBan = await _context.Bans.FindAsync(phieu.IdBan);
                    if (oldBan != null) oldBan.TrangThai = "Trống";
                    var newBan = await _context.Bans.FindAsync(dto.IdBan);
                    if (newBan == null) return BadRequest("Bàn mới không tồn tại.");
                    if (newBan.TrangThai == "Có khách") return BadRequest("Bàn mới đang có khách.");
                    if (dto.SoLuongKhach > newBan.SoGhe)
                    {
                        return BadRequest($"Số lượng khách ({dto.SoLuongKhach}) vượt quá số ghế của bàn mới ({newBan.SoGhe}).");
                    }
                    //newBan.TrangThai = "Đã đặt";
                    phieu.IdBan = dto.IdBan;
                }

                // Kiểm tra xung đột khi Sửa
                DateTime newSlotStart = dto.ThoiGianDat;
                DateTime newSlotEnd = dto.ThoiGianDat.AddHours(ReservationSlotHours);
                var conflict = await _context.PhieuDatBans
                    .Where(p => p.IdBan == dto.IdBan &&
                                p.IdPhieuDatBan != id && // Loại trừ chính nó
                                (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận"))
                    .ToListAsync();

                foreach (var p in conflict)
                {
                    DateTime existingSlotStart = p.ThoiGianDat.AddMinutes(-ReservationBufferMinutes);
                    DateTime existingSlotEnd = p.ThoiGianDat.AddHours(ReservationSlotHours).AddMinutes(ReservationBufferMinutes);
                    if (newSlotStart < existingSlotEnd && existingSlotStart < newSlotEnd)
                    {
                        return Conflict($"Xung đột! Bàn này đã có phiếu đặt lúc {p.ThoiGianDat:HH:mm} (có 5 phút đệm).");
                    }
                }

                phieu.ThoiGianDat = dto.ThoiGianDat;
                phieu.SoLuongKhach = dto.SoLuongKhach;
                phieu.GhiChu = dto.GhiChu;
                phieu.TrangThai = dto.TrangThai;

                // Lưu tất cả thay đổi
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Gửi email
                if (oldTrangThai == "Chờ xác nhận" && dto.TrangThai == "Đã xác nhận")
                {
                    var emailNguoiNhan = khachHang?.Email ?? dto.Email;
                    if (!string.IsNullOrEmpty(emailNguoiNhan) && phieu.Ban != null)
                    {
                        var khachInfo = new KhachHang { HoTen = dto.TenKhachHang, Email = emailNguoiNhan };
                        _ = SendConfirmationEmailAsync(phieu, khachInfo, phieu.Ban.SoBan);
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message} \nChi tiết: {ex.InnerException?.Message}");
            }
        }

        [HttpPost("xacnhan-den")]
        public async Task<ActionResult<XacNhanKhachDenResponseDto>> XacNhanKhachDen(XacNhanKhachDenRequestDto dto)
        {
            var phieu = await _context.PhieuDatBans
                .Include(p => p.Ban)
                .FirstOrDefaultAsync(p => p.IdPhieuDatBan == dto.IdPhieuDatBan);

            if (phieu == null) return NotFound("Phiếu đặt không tồn tại.");
            if (phieu.Ban == null) return BadRequest("Bàn không hợp lệ.");
            var ban = phieu.Ban;
            if (ban.TrangThai == "Có khách")
            {
                return BadRequest($"Bàn {ban.SoBan} đã có khách. Vui lòng kiểm tra lại.");
            }
            var hoaDon = new HoaDon
            {
                IdBan = phieu.IdBan,
                IdNhanVien = dto.IdNhanVien,
                IdKhachHang = phieu.IdKhachHang,
                ThoiGianTao = DateTime.Now,
                TrangThai = "Chưa thanh toán",
                LoaiHoaDon = "Tại quán",
                TongTienGoc = 0,
                GiamGia = 0,
                TongPhuThu = 0
            };
            _context.HoaDons.Add(hoaDon);
            await _context.SaveChangesAsync();
            phieu.TrangThai = "Khách đã đến";
            ban.TrangThai = "Có khách";
            await _context.SaveChangesAsync();
            return Ok(new XacNhanKhachDenResponseDto { IdHoaDon = hoaDon.IdHoaDon });
        }

        [HttpPost("huy/{id}")]
        public async Task<IActionResult> HuyDatBan(int id)
        {
            var phieu = await _context.PhieuDatBans.Include(p => p.Ban)
                        .FirstOrDefaultAsync(p => p.IdPhieuDatBan == id);
            if (phieu == null) return NotFound("Phiếu đặt không tồn tại.");
            if (phieu.TrangThai == "Đã hủy" || phieu.TrangThai == "Khách đã đến")
            {
                return BadRequest("Không thể hủy phiếu ở trạng thái này.");
            }
            phieu.TrangThai = "Đã hủy";
            if (phieu.Ban != null)
            {
                phieu.Ban.TrangThai = "Trống";
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("create-web")]
        public async Task<IActionResult> CreateDatBanWeb(PhieuDatBanWebCreateDto dto)
        {
            // Người dùng web luôn được coi là khách hàng thật (không vãng lai)
            KhachHang khachHang = null;
            try
            {
                // Dùng helper cũ vì logic web khác
                khachHang = await FindOrCreateKhachHangWeb(dto.TenKhachHang, dto.SoDienThoai, dto.Email);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var ban = await _context.Bans.FindAsync(dto.IdBan);
            if (ban == null) return BadRequest("Bàn không tồn tại.");
            if (ban.TrangThai != "Trống") return BadRequest("Bàn đã có người đặt hoặc đang có khách.");

            var openingHours = await GetAndParseOpeningHours();
            if (!IsTimeValid(dto.ThoiGianDat, openingHours))
            {
                return BadRequest($"Giờ đặt ({dto.ThoiGianDat:HH:mm}) nằm ngoài giờ mở cửa ({openingHours.Open:hh\\:mm} - {openingHours.Close:hh\\:mm}).");
            }
            if (dto.SoLuongKhach > ban.SoGhe)
            {
                return BadRequest($"Số lượng khách ({dto.SoLuongKhach}) vượt quá số ghế của bàn ({ban.SoGhe}).");
            }

            DateTime newSlotStart = dto.ThoiGianDat;
            DateTime newSlotEnd = dto.ThoiGianDat.AddHours(ReservationSlotHours);
            var conflict = await _context.PhieuDatBans
                .Where(p => p.IdBan == dto.IdBan &&
                            (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận"))
                .ToListAsync();
            foreach (var p in conflict)
            {
                DateTime existingSlotStart = p.ThoiGianDat.AddMinutes(-ReservationBufferMinutes);
                DateTime existingSlotEnd = p.ThoiGianDat.AddHours(ReservationSlotHours).AddMinutes(ReservationBufferMinutes);
                if (newSlotStart < existingSlotEnd && existingSlotStart < newSlotEnd)
                {
                    return Conflict($"Xung đột! Bàn này đã có phiếu đặt lúc {p.ThoiGianDat:HH:mm} (có 5 phút đệm).");
                }
            }

            var phieu = new PhieuDatBan
            {
                IdKhachHang = khachHang?.IdKhachHang, // Gán IdKhachHang
                IdBan = dto.IdBan,
                HoTenKhach = dto.TenKhachHang,
                SdtKhach = dto.SoDienThoai,
                ThoiGianDat = dto.ThoiGianDat,
                SoLuongKhach = dto.SoLuongKhach,
                GhiChu = dto.GhiChu,
                TrangThai = "Chờ xác nhận"
            };
            _context.PhieuDatBans.Add(phieu);
            //ban.TrangThai = "Đã đặt";

            // Lưu lần 1 (Phiếu, Bàn, Khách hàng (nếu có))
            await _context.SaveChangesAsync();

            var noiDungTB = $"Phiếu đặt bàn #{phieu.IdPhieuDatBan} ({khachHang.HoTen}) đang chờ xác nhận.";
            var thongBao = new ThongBao
            {
                IdNhanVienTao = null,
                NoiDung = noiDungTB,
                LoaiThongBao = "DatBan",
                IdLienQuan = phieu.IdPhieuDatBan,
                DaXem = false,
                ThoiGianTao = DateTime.Now
            };
            _context.ThongBaos.Add(thongBao);

            // Lưu lần 2 (Thông báo)
            await _context.SaveChangesAsync();
            return Ok(new { idPhieuDatBan = phieu.IdPhieuDatBan });
        }
        #endregion

        #region Helpers (Khách hàng, Email, Giờ)

        // SỬA ĐỔI: Đổi tên helper này thành Web để tránh nhầm lẫn
        private async Task<KhachHang> FindOrCreateKhachHangWeb(string ten, string sdt, string? email)
        {
            if (string.IsNullOrWhiteSpace(sdt) || sdt == "N/A")
            {
                return null;
            }

            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.SoDienThoai == sdt);

            if (khachHang == null)
            {
                khachHang = new KhachHang
                {
                    HoTen = ten,
                    SoDienThoai = sdt,
                    Email = email,
                    DiemTichLuy = 0,
                    NgayTao = DateTime.Now,
                    BiKhoa = false
                    // Logic TenDangNhap/MatKhau của Web/App khách có thể khác
                };
                _context.KhachHangs.Add(khachHang);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
                {
                    throw new Exception($"Lỗi CSDL: Không thể tạo khách hàng. SĐT hoặc Email có thể đã tồn tại. (Mã lỗi: {sqlEx.Number})");
                }
            }
            else
            {
                khachHang.HoTen = ten;
                khachHang.Email = string.IsNullOrEmpty(email) ? khachHang.Email : email;
                await _context.SaveChangesAsync();
            }
            return khachHang;
        }

        private async Task SendConfirmationEmailAsync(PhieuDatBan phieu, KhachHang khach, string soBan)
        {
            if (khach.Email == null)
            {
                Console.WriteLine("Bỏ qua gửi email: Email khách hàng là null.");
                return;
            }
            try
            {
                var smtpSettings = _config.GetSection("SmtpSettings");
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(smtpSettings["FromName"] ?? "Notify", smtpSettings["Username"]));
                email.To.Add(new MailboxAddress(khach.HoTen, khach.Email));
                email.Subject = "[Cafebook] Xác nhận đặt bàn thành công";
                string body = $@"<p>Xin chào {khach.HoTen},</p>
                    <p>Cảm ơn bạn đã đặt bàn tại Cafebook.</p>
                    <p><strong>Thông tin đặt bàn:</strong></p>
                    <ul>
                        <li><strong>Bàn:</strong> {soBan}</li>
                        <li><strong>Thời gian:</strong> {phieu.ThoiGianDat:HH:mm} ngày {phieu.ThoiGianDat:dd/MM/yyyy}</li>
                        <li><strong>Số khách:</strong> {phieu.SoLuongKhach}</li>
                        <li><strong>Ghi chú:</strong> {phieu.GhiChu ?? "Không có"}</li>
                        <li><strong>Trạng thái:</strong> {phieu.TrangThai}</li>
                    </ul>
                    <p>Rất mong được đón tiếp bạn!</p>
                    <p>Trân trọng,<br>Đội ngũ Cafebook</p>";
                email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(smtpSettings["Host"], int.Parse(smtpSettings["Port"] ?? "0"), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpSettings["Username"], smtpSettings["Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email (chạy ngầm): {ex.Message}");
            }
        }

        private async Task<OpeningHours> GetAndParseOpeningHours()
        {
            var setting = await _context.CaiDats
                .FirstOrDefaultAsync(cd => cd.TenCaiDat == "LienHe_GioMoCua");
            string settingValue = (setting != null && !string.IsNullOrEmpty(setting.GiaTri)) ? setting.GiaTri : "06:00 - 23:00";
            return ParseOpeningHours(settingValue);
        }

        private OpeningHours ParseOpeningHours(string settingValue)
        {
            var hours = new OpeningHours();
            try
            {
                var match = Regex.Match(settingValue, @"(\d{2}:\d{2})\s*-\s*(\d{2}:\d{2})");
                if (match.Success)
                {
                    if (TimeSpan.TryParse(match.Groups[1].Value, out TimeSpan open)) hours.Open = open;
                    if (TimeSpan.TryParse(match.Groups[2].Value, out TimeSpan close)) hours.Close = close;
                    hours.IsValid = true;
                }
            }
            catch { }
            return hours;
        }

        private bool IsTimeValid(DateTime thoiGianDat, OpeningHours hours)
        {
            var timeOfDay = thoiGianDat.TimeOfDay;
            return timeOfDay >= hours.Open && timeOfDay <= hours.Close;
        }
        #endregion

        private async Task AutoCancelLateReservationsInternal()
        {
            try
            {
                var now = DateTime.Now;
                // Logic: Quá 15 phút so với giờ đặt
                var timeLimit = now.AddMinutes(-15);

                // Tìm các phiếu:
                // 1. Trạng thái là "Đã xác nhận" hoặc "Chờ xác nhận"
                // 2. Thời gian đặt < (Hiện tại - 15 phút)
                // 3. Chưa bị hủy
                var lateReservations = await _context.PhieuDatBans
                    .Include(p => p.Ban)
                    .Where(p => (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận") &&
                                p.ThoiGianDat < timeLimit)
                    .ToListAsync();

                if (lateReservations.Any())
                {
                    foreach (var phieu in lateReservations)
                    {
                        phieu.TrangThai = "Đã hủy";
                        // Thêm ghi chú tự động để nhân viên biết tại sao hủy
                        phieu.GhiChu = string.IsNullOrEmpty(phieu.GhiChu)
                            ? "Tự động hủy do khách trễ 15p"
                            : phieu.GhiChu + " | Tự động hủy do trễ 15p";

                        // Quan trọng: Phải trả trạng thái bàn về "Trống" nếu bàn đang bị giữ
                        if (phieu.Ban != null && phieu.Ban.TrangThai != "Có khách")
                        {
                            phieu.Ban.TrangThai = "Trống";
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Ghi log nhẹ nhàng, không làm crash luồng chính
                Console.WriteLine($"Lỗi auto-cancel: {ex.Message}");
            }
        }
    }
}