using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/donxinnghi")]
    [ApiController]
    public class DonXinNghiController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public DonXinNghiController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API Lấy danh sách Đơn Xin Nghỉ (có lọc/tìm kiếm)
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? searchText,
            [FromQuery] string? trangThai)
        {
            var query = _context.DonXinNghis
                .Include(d => d.NhanVien)
                .Include(d => d.NguoiDuyet)
                .AsQueryable();

            if (!string.IsNullOrEmpty(trangThai) && trangThai != "Tất cả")
            {
                query = query.Where(d => d.TrangThai == trangThai);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                string searchLower = searchText.ToLower();
                query = query.Where(d =>
                    d.NhanVien.HoTen.ToLower().Contains(searchLower) ||
                    d.LyDo.ToLower().Contains(searchLower)
                );
            }

            var results = await query
                .OrderByDescending(d => d.NgayBatDau)
                .Select(d => new DonXinNghiDto
                {
                    IdDonXinNghi = d.IdDonXinNghi,
                    TenNhanVien = d.NhanVien.HoTen,
                    LoaiDon = d.LoaiDon,
                    NgayBatDau = d.NgayBatDau,
                    NgayKetThuc = d.NgayKetThuc,
                    LyDo = d.LyDo,
                    TrangThai = d.TrangThai,
                    TenNguoiDuyet = d.NguoiDuyet != null ? d.NguoiDuyet.HoTen : null,
                    NgayDuyet = d.NgayDuyet,
                    GhiChuPheDuyet = d.GhiChuPheDuyet
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// API Thêm mới Đơn Xin Nghỉ
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDonXinNghi([FromBody] DonXinNghiCreateDto dto)
        {
            var entity = new DonXinNghi
            {
                IdNhanVien = dto.IdNhanVien,
                LoaiDon = dto.LoaiDon,
                LyDo = dto.LyDo,
                NgayBatDau = dto.NgayBatDau.Date,
                NgayKetThuc = dto.NgayKetThuc.Date,
                TrangThai = "Chờ duyệt", // Mặc định
                NgayDuyet = null
            };

            _context.DonXinNghis.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        /// <summary>
        /// API Duyệt đơn
        /// </summary>
        [HttpPut("approve/{idDon}")]
        public async Task<IActionResult> ApproveDon(int idDon, [FromBody] DonXinNghiActionDto dto)
        {
            var don = await _context.DonXinNghis.FindAsync(idDon);
            if (don == null) return NotFound("Không tìm thấy đơn.");
            if (don.TrangThai != "Chờ duyệt") return Conflict("Đơn này đã được xử lý trước đó.");

            // Cập nhật trạng thái đơn
            don.TrangThai = "Đã duyệt";
            don.IdNguoiDuyet = dto.IdNguoiDuyet;
            don.GhiChuPheDuyet = dto.GhiChuPheDuyet;
            don.NgayDuyet = DateTime.Now;

            // TỰ ĐỘNG CẬP NHẬT LỊCH LÀM VIỆC (Yêu cầu #4)
            var conflictingSchedules = await _context.LichLamViecs
                .Where(l => l.IdNhanVien == don.IdNhanVien &&
                            l.NgayLam >= don.NgayBatDau.Date &&
                            l.NgayLam <= don.NgayKetThuc.Date)
                .ToListAsync();

            // Kiểm tra xem có lịch nào đã chấm công chưa
            foreach (var lich in conflictingSchedules)
            {
                if (await _context.BangChamCongs.AnyAsync(c => c.IdLichLamViec == lich.IdLichLamViec))
                {
                    // Nếu đã chấm công, không thể duyệt -> Rollback
                    return Conflict($"Không thể duyệt. Nhân viên đã chấm công ngày {lich.NgayLam:dd/MM/yyyy}.");
                }
            }

            // Nếu không có xung đột -> Xóa các lịch làm việc
            if (conflictingSchedules.Any())
            {
                _context.LichLamViecs.RemoveRange(conflictingSchedules);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Duyệt đơn thành công và đã cập nhật lịch làm việc." });
        }

        /// <summary>
        /// API Từ chối đơn
        /// </summary>
        [HttpPut("reject/{idDon}")]
        public async Task<IActionResult> RejectDon(int idDon, [FromBody] DonXinNghiActionDto dto)
        {
            var don = await _context.DonXinNghis.FindAsync(idDon);
            if (don == null) return NotFound("Không tìm thấy đơn.");
            if (don.TrangThai != "Chờ duyệt") return Conflict("Đơn này đã được xử lý trước đó.");

            don.TrangThai = "Đã từ chối";
            don.IdNguoiDuyet = dto.IdNguoiDuyet;
            don.GhiChuPheDuyet = dto.GhiChuPheDuyet;
            don.NgayDuyet = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Từ chối đơn thành công." });
        }

        /// <summary>
        /// API Xóa đơn (chỉ khi chưa duyệt)
        /// </summary>
        [HttpDelete("{idDon}")]
        public async Task<IActionResult> DeleteDon(int idDon)
        {
            var don = await _context.DonXinNghis.FindAsync(idDon);
            if (don == null) return NotFound();

            if (don.TrangThai != "Chờ duyệt")
            {
                return Conflict("Không thể xóa. Đơn này đã được xử lý.");
            }

            _context.DonXinNghis.Remove(don);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}