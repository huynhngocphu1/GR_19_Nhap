// Tệp: CafebookApi/Controllers/App/NhanVien/ChamCongController.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp.NhanVien;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Đảm bảo có using này
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App.NhanVien
{
    [Route("api/app/chamcong")]
    [ApiController]
    [Authorize]
    public class ChamCongController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public ChamCongController(CafebookDbContext context)
        {
            _context = context;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetChamCongStatus()
        {
            try
            {
                int idNhanVien = GetCurrentUserId();
                if (idNhanVien == 0) return Unauthorized("Token không hợp lệ hoặc thiếu IdNhanVien.");

                var dto = await GetDashboardDto(idNhanVien);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetChamCongStatus Error]: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }

        [HttpPost("clock-in")]
        public async Task<IActionResult> ClockIn()
        {
            int idNhanVien = GetCurrentUserId();
            var homNay = DateTime.Today;

            var lichLamViec = await _context.LichLamViecs
                .FirstOrDefaultAsync(l => l.IdNhanVien == idNhanVien && l.NgayLam == homNay);

            if (lichLamViec == null)
            {
                return BadRequest("Bạn không có ca làm việc được đăng ký hôm nay.");
            }

            var donNghi = await _context.DonXinNghis.AsNoTracking()
                .FirstOrDefaultAsync(d => d.IdNhanVien == idNhanVien &&
                                          d.TrangThai == "Đã duyệt" &&
                                          d.NgayBatDau.Date <= homNay &&
                                          d.NgayKetThuc.Date >= homNay);
            if (donNghi != null)
            {
                return BadRequest("Bạn không thể chấm công. Đơn xin nghỉ của bạn đã được duyệt hôm nay.");
            }

            var chamCongHienTai = await _context.BangChamCongs
                .FirstOrDefaultAsync(c => c.IdLichLamViec == lichLamViec.IdLichLamViec);

            if (chamCongHienTai != null && chamCongHienTai.GioVao != null)
            {
                return BadRequest("Bạn đã chấm công vào ca này rồi.");
            }

            if (chamCongHienTai == null)
            {
                chamCongHienTai = new BangChamCong
                {
                    IdLichLamViec = lichLamViec.IdLichLamViec,
                    GioVao = DateTime.Now
                };
                _context.BangChamCongs.Add(chamCongHienTai);
            }
            else
            {
                chamCongHienTai.GioVao = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            var dto = await GetDashboardDto(idNhanVien);
            return Ok(dto);
        }

        [HttpPost("clock-out")]
        public async Task<IActionResult> ClockOut()
        {
            int idNhanVien = GetCurrentUserId();
            var homNay = DateTime.Today;

            var chamCong = await _context.BangChamCongs
                .Include(c => c.LichLamViec)
                .FirstOrDefaultAsync(c =>
                    c.LichLamViec != null &&
                    c.LichLamViec.IdNhanVien == idNhanVien &&
                    c.LichLamViec.NgayLam == homNay &&
                    c.GioVao != null &&
                    c.GioRa == null);

            if (chamCong == null)
            {
                return BadRequest("Bạn chưa chấm công vào hoặc đã trả ca rồi.");
            }

            chamCong.GioRa = DateTime.Now;
            await _context.SaveChangesAsync();
            await _context.Entry(chamCong).ReloadAsync();

            var dto = await GetDashboardDto(idNhanVien);
            return Ok(dto);
        }

        [HttpGet("lich-su")]
        public async Task<IActionResult> GetLichSuChamCong([FromQuery] int thang, [FromQuery] int nam)
        {
            int idNhanVien = GetCurrentUserId();
            var ngayDauThang = new DateTime(nam, thang, 1);
            var ngayCuoiThang = ngayDauThang.AddMonths(1).AddDays(-1);

            // 1. Lấy lịch sử chấm công (Đã sửa Include)
            var lichSu = await _context.BangChamCongs
                .Include(c => c.LichLamViec)
                    .ThenInclude(l => l.CaLamViec)
                .Where(c => c.LichLamViec != null &&
                            c.LichLamViec.IdNhanVien == idNhanVien &&
                            c.LichLamViec.NgayLam >= ngayDauThang &&
                            c.LichLamViec.NgayLam <= ngayCuoiThang)
                .OrderBy(c => c.LichLamViec.NgayLam)
                .AsNoTracking()
                .ToListAsync();

            var lichSuDto = lichSu.Select(c =>
            {
                var thoiGianTre = (c.GioVao.HasValue && c.LichLamViec!.CaLamViec != null && c.GioVao.Value.TimeOfDay > c.LichLamViec.CaLamViec.GioBatDau)
                    ? c.GioVao.Value.TimeOfDay - c.LichLamViec.CaLamViec.GioBatDau
                    : TimeSpan.Zero;

                return new LichSuItemDto
                {
                    Ngay = c.LichLamViec!.NgayLam.ToString("dd/MM (ddd)"),
                    CaLamViec = c.LichLamViec.CaLamViec?.TenCa ?? "N/A",
                    GioVao = c.GioVao?.ToString("HH:mm") ?? "--",
                    GioRa = c.GioRa?.ToString("HH:mm") ?? "--",
                    DiTre = (thoiGianTre.TotalMinutes > 5) ? $"{thoiGianTre.TotalMinutes:N0} phút" : "",
                    SoGioLam = c.SoGioLam ?? 0
                };
            }).ToList();

            // 2. Lấy đơn xin nghỉ
            var donNghi = await _context.DonXinNghis
                .Where(d => d.IdNhanVien == idNhanVien &&
                            (d.NgayBatDau.Month == thang && d.NgayBatDau.Year == nam))
                .OrderByDescending(d => d.NgayBatDau)
                .AsNoTracking()
                .ToListAsync();

            var donNghiDto = donNghi.Select(d => new DonNghiItemDto
            {
                IdDonXinNghi = d.IdDonXinNghi,
                LoaiDon = d.LoaiDon,
                ThoiGian = $"{d.NgayBatDau:dd/MM} - {d.NgayKetThuc:dd/MM}",
                LyDo = d.LyDo,
                TrangThai = d.TrangThai,
                PheDuyet = d.GhiChuPheDuyet
            }).ToList();

            // 3. Thống kê
            int soNgayNghiPhep = await _context.DonXinNghis
                .Where(d => d.IdNhanVien == idNhanVien &&
                            d.TrangThai == "Đã duyệt" &&
                            d.NgayBatDau.Month == thang && d.NgayBatDau.Year == nam)
                .CountAsync();

            var thongKe = new ThongKeChamCongDto
            {
                TongGioLam = lichSuDto.Sum(l => l.SoGioLam),
                SoLanDiTre = lichSuDto.Count(l => !string.IsNullOrEmpty(l.DiTre)),
                SoNgayNghiPhep = soNgayNghiPhep
            };

            return Ok(new LichSuChamCongPageDto
            {
                LichSuChamCong = lichSuDto,
                DanhSachDonNghi = donNghiDto,
                ThongKe = thongKe
            });
        }

        [HttpPost("submit-leave")]
        public async Task<IActionResult> SubmitLeaveRequest([FromBody] DonXinNghiRequestDto req)
        {
            if (req.NgayKetThuc < req.NgayBatDau)
                return BadRequest("Ngày kết thúc không thể trước ngày bắt đầu.");

            var idNhanVien = GetCurrentUserId();
            var nhanVien = await _context.NhanViens
                .AsNoTracking()
                .Select(nv => new { nv.IdNhanVien, nv.HoTen })
                .FirstOrDefaultAsync(nv => nv.IdNhanVien == idNhanVien);

            if (nhanVien == null)
            {
                return NotFound("Không tìm thấy nhân viên.");
            }

            var don = new DonXinNghi
            {
                IdNhanVien = idNhanVien,
                LoaiDon = req.LoaiDon,
                LyDo = req.LyDo,
                NgayBatDau = req.NgayBatDau,
                NgayKetThuc = req.NgayKetThuc,
                TrangThai = "Chờ duyệt",
            };

            _context.DonXinNghis.Add(don);
            await _context.SaveChangesAsync();

            var thongBao = new ThongBao
            {
                IdNhanVienTao = idNhanVien,
                NoiDung = $"Đơn nghỉ mới từ {nhanVien.HoTen}",
                ThoiGianTao = DateTime.Now,
                LoaiThongBao = "DonXinNghi",
                IdLienQuan = don.IdDonXinNghi,
                DaXem = false
            };

            _context.ThongBaos.Add(thongBao);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Gửi đơn xin nghỉ và tạo thông báo thành công!" });
        }

        // === HÀM HELPER ===
        private int GetCurrentUserId()
        {
            // Lấy đúng Claim "IdNhanVien"
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "IdNhanVien");

            if (idClaim != null && int.TryParse(idClaim.Value, out int idNhanVien))
            {
                return idNhanVien;
            }
            return 0;
        }

        private async Task<ChamCongDashboardDto> GetDashboardDto(int idNhanVien)
        {
            var homNay = DateTime.Today;
            var dauThangNay = new DateTime(homNay.Year, homNay.Month, 1);
            var nhanVien = await _context.NhanViens.FindAsync(idNhanVien);

            var dto = new ChamCongDashboardDto
            {
                TenNhanVien = nhanVien?.HoTen ?? "N/A"
            };

            // 1. Kiểm tra đơn xin nghỉ
            var donNghi = await _context.DonXinNghis.AsNoTracking()
                .FirstOrDefaultAsync(d => d.IdNhanVien == idNhanVien &&
                                          d.NgayBatDau.Date <= homNay &&
                                          d.NgayKetThuc.Date >= homNay);
            if (donNghi != null)
            {
                dto.TrangThaiDonNghi = donNghi.TrangThai;
                if (donNghi.TrangThai == "Đã duyệt")
                {
                    dto.TrangThai = "NghiPhep";
                    dto.TenCa = "Nghỉ phép";
                }
            }

            // 2. Kiểm tra lịch làm (chỉ khi không nghỉ phép)
            if (dto.TrangThai != "NghiPhep")
            {
                var lichLamViec = await _context.LichLamViecs
                    .Include(l => l.CaLamViec)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.IdNhanVien == idNhanVien && l.NgayLam == homNay);

                if (lichLamViec == null)
                {
                    dto.TrangThai = "KhongCoCa";
                    dto.TenCa = "Không có lịch làm";
                }
                else
                {
                    if (lichLamViec.CaLamViec == null)
                    {
                        dto.TrangThai = "KhongCoCa";
                        dto.TenCa = "Lỗi: Ca làm việc không hợp lệ";
                    }
                    else
                    {
                        dto.TenCa = lichLamViec.CaLamViec.TenCa;
                        dto.GioBatDauCa = lichLamViec.CaLamViec.GioBatDau;
                        dto.GioKetThucCa = lichLamViec.CaLamViec.GioKetThuc;

                        var chamCong = await _context.BangChamCongs
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.IdLichLamViec == lichLamViec.IdLichLamViec);

                        if (chamCong == null || chamCong.GioVao == null)
                        {
                            dto.TrangThai = "ChuaChamCong";
                        }
                        else if (chamCong.GioRa == null)
                        {
                            dto.TrangThai = "DaChamCong";
                            dto.GioVao = chamCong.GioVao;
                        }
                        else
                        {
                            dto.TrangThai = "DaTraCa";
                            dto.GioVao = chamCong.GioVao;
                            dto.GioRa = chamCong.GioRa;
                            dto.SoGioLam = chamCong.SoGioLam;
                        }
                    }
                }
            }

            // 4. Tính số lần đi trễ
            // *** SỬA LỖI: Dùng EF.Functions.DateDiffMinute ***
            int soPhutTreChoPhep = 5;
            var soLanDiTre = await _context.BangChamCongs
                .Include(c => c.LichLamViec)
                    .ThenInclude(l => l.CaLamViec)
                .AsNoTracking()
                .CountAsync(c =>
                    c.LichLamViec != null &&
                    c.LichLamViec.IdNhanVien == idNhanVien &&
                    c.LichLamViec.NgayLam >= dauThangNay &&
                    c.LichLamViec.NgayLam <= homNay &&
                    c.GioVao != null &&
                    c.LichLamViec.CaLamViec != null &&
                    // Sử dụng DateDiffMinute để EF Core dịch sang SQL
                    EF.Functions.DateDiffMinute(c.LichLamViec.CaLamViec.GioBatDau, c.GioVao.Value.TimeOfDay) > soPhutTreChoPhep
                );

            dto.SoLanDiTreThangNay = soLanDiTre;

            return dto;
        }
    }
}