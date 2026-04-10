using CafebookApi.Data;
using CafebookModel.Model.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using CafebookModel.Model.ModelApp.NhanVien;
using System.Collections.Generic; // Thêm
//using System.Net.Http; // <-- THÊM MỚI

namespace CafebookApi.Controllers.App.NhanVien
{


    [Route("api/app/sodoban")]
    [ApiController]
    public class SoDoBanController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        //private readonly IHttpClientFactory _clientFactory;
        public SoDoBanController(CafebookDbContext context, IHttpClientFactory clientFactory) // <-- SỬA HÀM KHỞI TẠO
        {
            _context = context;
            //_clientFactory = clientFactory; // <-- THÊM MỚI
        }

        // === HÀM ĐÃ ĐƯỢC NÂNG CẤP (10 PHÚT) ===
        [HttpGet("tables")]
        public async Task<IActionResult> GetSoDoBan()
        {
            // === SỬA: GỌI HÀM NỘI BỘ (NHANH & KHÔNG CẦN API RIÊNG) ===
            await AutoCancelLateReservationsInternal();
            // === KẾT THÚC SỬA ===

            var now = DateTime.Now;
            var nowPlus10Minutes = now.AddMinutes(10); // Mốc thời gian 10 phút

            var data = await _context.Bans
               .AsNoTracking()
               .Select(b => new
               {
                   Ban = b,
                   HoaDonHienTai = _context.HoaDons
                       .Where(h => h.IdBan == b.IdBan && h.TrangThai == "Chưa thanh toán")
                       .Select(h => new { h.IdHoaDon, h.ThanhTien })
                       .FirstOrDefault(),

                   PhieuDatSapToi = _context.PhieuDatBans
                       .Where(p => p.IdBan == b.IdBan &&
                                   p.ThoiGianDat > now &&
                                   (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận"))
                       .OrderBy(p => p.ThoiGianDat)
                       .FirstOrDefault()
               })
               .Select(data => new BanSoDoDto
               {
                   IdBan = data.Ban.IdBan,
                   SoBan = data.Ban.SoBan,

                   // Logic hiển thị trạng thái trên Sơ đồ
                   TrangThai = (data.Ban.TrangThai == "Trống" &&
                                data.PhieuDatSapToi != null &&
                                data.PhieuDatSapToi.ThoiGianDat <= nowPlus10Minutes)
                               ? "Đã đặt" // Tự động hiển thị màu vàng/cam khi sắp đến giờ
                               : data.Ban.TrangThai,

                   GhiChu = data.Ban.GhiChu,
                   IdKhuVuc = data.Ban.IdKhuVuc,
                   IdHoaDonHienTai = data.HoaDonHienTai != null ? (int?)data.HoaDonHienTai.IdHoaDon : null,
                   TongTienHienTai = data.HoaDonHienTai != null ? data.HoaDonHienTai.ThanhTien : 0,
                   ThongTinDatBan = (data.Ban.TrangThai == "Trống" && data.PhieuDatSapToi != null)
                                    ? $"Đặt lúc: {data.PhieuDatSapToi.ThoiGianDat:HH:mm}"
                                    : null
               })
               .OrderBy(b => b.SoBan)
               .ToListAsync();

            return Ok(data);
        }

        // (Các hàm còn lại giữ nguyên)

        [HttpPost("createorder/{idBan}/{idNhanVien}")]
        public async Task<IActionResult> CreateOrder(int idBan, int idNhanVien)
        {
            var ban = await _context.Bans.FindAsync(idBan);
            if (ban == null) return NotFound("Không tìm thấy bàn.");
            if (ban.TrangThai != "Trống" && ban.TrangThai != "Đã đặt")
                return Conflict("Bàn này đang bận hoặc đang bảo trì.");
            var nhanVien = await _context.NhanViens.FindAsync(idNhanVien);
            if (nhanVien == null) return NotFound("Nhân viên không hợp lệ.");
            var hoaDon = new HoaDon
            {
                IdBan = idBan,
                IdNhanVien = idNhanVien,
                TrangThai = "Chưa thanh toán",
                LoaiHoaDon = "Tại quán",
                ThoiGianTao = DateTime.Now
            };
            _context.HoaDons.Add(hoaDon);
            ban.TrangThai = "Có khách";
            await _context.SaveChangesAsync();
            return Ok(new { idHoaDon = hoaDon.IdHoaDon });
        }

        [HttpPost("reportproblem/{idBan}/{idNhanVien}")]
        public async Task<IActionResult> BaoCaoSuCo(int idBan, int idNhanVien, [FromBody] BaoCaoSuCoRequestDto request)
        {
            var ban = await _context.Bans.FindAsync(idBan);
            if (ban == null) return NotFound("Không tìm thấy bàn.");
            if (ban.TrangThai == "Có khách")
                return Conflict("Không thể báo cáo sự cố bàn đang có khách.");
            ban.TrangThai = "Bảo trì";
            ban.GhiChu = $"[Sự cố NV báo]: {request.GhiChuSuCo}";
            var thongBao = new ThongBao
            {
                IdNhanVienTao = idNhanVien,
                NoiDung = $"Bàn {ban.SoBan} vừa được báo cáo sự cố: {request.GhiChuSuCo}",
                LoaiThongBao = "SuCoBan",
                IdLienQuan = idBan,
                ThoiGianTao = DateTime.Now,
                DaXem = false
            };
            _context.ThongBaos.Add(thongBao);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Báo cáo sự cố thành công. Bàn đã được khóa." });
        }

        [HttpPost("createorder-no-table/{idNhanVien}")]
        public async Task<IActionResult> CreateOrderNoTable(int idNhanVien, [FromBody] string loaiHoaDon)
        {
            if (loaiHoaDon != "Mang về" && loaiHoaDon != "Tại quán")
                return BadRequest("Loại hóa đơn không hợp lệ.");

            var nhanVien = await _context.NhanViens.FindAsync(idNhanVien);
            if (nhanVien == null) return NotFound("Nhân viên không hợp lệ.");
            var hoaDon = new HoaDon
            {
                IdBan = null,
                IdNhanVien = idNhanVien,
                TrangThai = "Chưa thanh toán",
                LoaiHoaDon = loaiHoaDon,
                ThoiGianTao = DateTime.Now
            };
            _context.HoaDons.Add(hoaDon);
            await _context.SaveChangesAsync();
            return Ok(new { idHoaDon = hoaDon.IdHoaDon });
        }

        [HttpPost("move-table")]
        public async Task<IActionResult> MoveTable([FromBody] BanActionRequestDto dto)
        {
            var hoaDon = await _context.HoaDons.Include(h => h.Ban).FirstOrDefaultAsync(h => h.IdHoaDon == dto.IdHoaDonNguon);
            if (hoaDon == null) return NotFound("Không tìm thấy hóa đơn nguồn.");
            var banDich = await _context.Bans.FindAsync(dto.IdBanDich);
            if (banDich == null) return NotFound("Không tìm thấy bàn đích.");
            if (banDich.TrangThai != "Trống") return Conflict("Bàn đích đang bận, không thể chuyển đến.");
            if (hoaDon.Ban != null) hoaDon.Ban.TrangThai = "Trống";
            banDich.TrangThai = "Có khách";
            hoaDon.IdBan = dto.IdBanDich;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Chuyển bàn thành công." });
        }


        [HttpPost("merge-table")]
        public async Task<IActionResult> MergeTable([FromBody] BanActionRequestDto dto)
        {
            if (dto.IdHoaDonNguon == dto.IdHoaDonDich)
                return BadRequest("Không thể gộp bàn vào chính nó.");

            var chiTietNguon = await _context.ChiTietHoaDons
                .Where(c => c.IdHoaDon == dto.IdHoaDonNguon)
                .ToListAsync();

            if (!chiTietNguon.Any())
                return BadRequest("Bàn nguồn không có sản phẩm để gộp.");

            var hoaDonNguon = await _context.HoaDons
                .Include(h => h.Ban)
                .FirstOrDefaultAsync(h => h.IdHoaDon == dto.IdHoaDonNguon);

            if (hoaDonNguon == null) return NotFound("Không tìm thấy hóa đơn nguồn.");

            if (!dto.IdHoaDonDich.HasValue ||
                !await _context.HoaDons.AnyAsync(h => h.IdHoaDon == dto.IdHoaDonDich.Value))
            {
                return NotFound("Không tìm thấy hóa đơn đích.");
            }

            foreach (var ct in chiTietNguon)
            {
                ct.IdHoaDon = dto.IdHoaDonDich.Value;
            }

            if (hoaDonNguon.Ban != null)
            {
                hoaDonNguon.Ban.TrangThai = "Trống";
            }

            _context.HoaDons.Remove(hoaDonNguon);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Gộp bàn thành công." });
        }
        // === THÊM MỚI: HÀM LOGIC HỦY VÉ (Copy từ DatBanController sang) ===
        private async Task AutoCancelLateReservationsInternal()
        {
            try
            {
                var now = DateTime.Now;
                var timeLimit = now.AddMinutes(-15); // Quá hạn 15 phút

                // Tìm các phiếu trễ mà chưa bị hủy
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
                        phieu.GhiChu = string.IsNullOrEmpty(phieu.GhiChu)
                            ? "Tự động hủy do khách trễ 15p"
                            : phieu.GhiChu + " | Tự động hủy do trễ 15p";

                        // Reset bàn về trạng thái Trống nếu bàn đó chưa có khách ngồi
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
                Console.WriteLine($"Lỗi auto-cancel (SoDoBan): {ex.Message}");
            }
        }
    }
}