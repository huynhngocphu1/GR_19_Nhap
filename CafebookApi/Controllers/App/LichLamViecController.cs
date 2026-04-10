using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/lichlamviec")]
    [ApiController]
    public class LichLamViecController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public LichLamViecController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API Lấy danh sách Nhân viên (còn làm việc) để gán lịch
        /// </summary>
        [HttpGet("all-nhanvien")]
        public async Task<IActionResult> GetAllNhanVienLookup()
        {
            var data = await _context.NhanViens
                .Where(nv => nv.TrangThaiLamViec == "Đang làm việc")
                .Select(nv => new NhanVienLookupDto
                {
                    IdNhanVien = nv.IdNhanVien,
                    HoTen = nv.HoTen
                })
                .OrderBy(nv => nv.HoTen)
                .ToListAsync();
            return Ok(data);
        }

        /// <summary>
        /// API Lấy lịch làm việc VÀ lịch nghỉ trong 1 ngày
        /// </summary>
        [HttpGet("by-date")]
        public async Task<IActionResult> GetScheduleByDate([FromQuery] DateTime date)
        {
            var targetDate = date.Date;

            // 1. Lấy Lịch Đi Làm
            var caLamList = await _context.LichLamViecs
                .Include(l => l.NhanVien)
                .Include(l => l.CaLamViec)
                .Where(l => l.NgayLam == targetDate)
                .Select(l => new LichLamViecDisplayDto
                {
                    IdLich = l.IdLichLamViec,
                    IdNhanVien = l.IdNhanVien,
                    HoTenNhanVien = l.NhanVien.HoTen,
                    Ngay = l.NgayLam,
                    LoaiLich = "CaLam",
                    TenCa = l.CaLamViec.TenCa,
                    GioBatDau = l.CaLamViec.GioBatDau,
                    GioKetThuc = l.CaLamViec.GioKetThuc
                })
                .ToListAsync();

            // 2. Lấy Lịch Đã Duyệt Nghỉ (Yêu cầu #5)
            var donNghiList = await _context.DonXinNghis
                .Include(d => d.NhanVien)
                .Where(d => d.TrangThai == "Đã duyệt" &&
                            targetDate >= d.NgayBatDau.Date &&
                            targetDate <= d.NgayKetThuc.Date)
                .Select(d => new LichLamViecDisplayDto
                {
                    IdLich = d.IdDonXinNghi,
                    IdNhanVien = d.IdNhanVien,
                    HoTenNhanVien = d.NhanVien.HoTen,
                    Ngay = targetDate,
                    LoaiLich = "NghiPhep",
                    LyDoNghi = d.LyDo
                })
                .ToListAsync();

            // 3. Gộp 2 danh sách
            var combinedList = caLamList.Concat(donNghiList)
                .OrderBy(l => l.HoTenNhanVien)
                .ToList();

            return Ok(combinedList);
        }

        /// <summary>
        /// API Gán lịch làm việc cho nhiều nhân viên
        /// </summary>
        [HttpPost("assign")]
        public async Task<IActionResult> AssignSchedule([FromBody] LichLamViecCreateDto dto)
        {
            if (dto == null || !dto.DanhSachIdNhanVien.Any())
                return BadRequest("Dữ liệu không hợp lệ.");

            var ngay = dto.NgayGanLich.Date;
            int successCount = 0;
            var errors = new List<string>();

            // Lấy danh sách lịch đã tồn tại
            var existingSchedules = await _context.LichLamViecs
                .Where(l => l.NgayLam == ngay && dto.DanhSachIdNhanVien.Contains(l.IdNhanVien))
                .Select(l => l.IdNhanVien)
                .ToListAsync();

            // Lấy danh sách nghỉ đã tồn tại
            var existingNghiPhep = await _context.DonXinNghis
                .Where(d => dto.DanhSachIdNhanVien.Contains(d.IdNhanVien) &&
                            d.TrangThai == "Đã duyệt" &&
                            ngay >= d.NgayBatDau.Date &&
                            ngay <= d.NgayKetThuc.Date)
                .Select(d => d.IdNhanVien)
                .ToListAsync();

            var newEntries = new List<LichLamViec>();
            foreach (var idNhanVien in dto.DanhSachIdNhanVien)
            {
                if (existingSchedules.Contains(idNhanVien))
                {
                    errors.Add($"ID {idNhanVien}: Đã có lịch làm hôm đó.");
                }
                else if (existingNghiPhep.Contains(idNhanVien))
                {
                    errors.Add($"ID {idNhanVien}: Đang trong thời gian nghỉ phép.");
                }
                else
                {
                    newEntries.Add(new LichLamViec
                    {
                        IdNhanVien = idNhanVien,
                        IdCa = dto.IdCa,
                        NgayLam = ngay
                    });
                    successCount++;
                }
            }

            if (newEntries.Any())
            {
                _context.LichLamViecs.AddRange(newEntries);
                await _context.SaveChangesAsync();
            }

            return Ok(new LichLamViecAssignResponseDto
            {
                Message = $"Gán lịch thành công cho {successCount} nhân viên.",
                Failures = errors
            });
        }

        /// <summary>
        /// API Xóa 1 lịch làm việc cụ thể
        /// </summary>
        [HttpDelete("{idLichLamViec}")]
        public async Task<IActionResult> DeleteSchedule(int idLichLamViec)
        {
            var entity = await _context.LichLamViecs.FindAsync(idLichLamViec);
            if (entity == null) return NotFound();

            // Kiểm tra xem đã chấm công chưa (Yêu cầu #6)
            if (await _context.BangChamCongs.AnyAsync(c => c.IdLichLamViec == idLichLamViec))
            {
                return Conflict("Không thể xóa. Lịch làm việc này đã có dữ liệu chấm công.");
            }

            _context.LichLamViecs.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}