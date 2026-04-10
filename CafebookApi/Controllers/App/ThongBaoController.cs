using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/thongbao")]
    [ApiController]
    public class ThongBaoController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public ThongBaoController(CafebookDbContext context)
        {
            _context = context;
        }

        // 1. API lấy danh sách thông báo cho Nhân viên (Đã bỏ HoTroKhachHang)
        [HttpGet("staff/all")]
        public async Task<IActionResult> GetStaffNotifications()
        {
            // Chỉ lấy: Gọi món, Đặt bàn, Đơn ship, Hệ thống
            var allowedTypes = new[] { "PhieuGoiMon", "DatBan", "DonHangMoi", "HeThong" };

            var notifications = await _context.ThongBaos
                .Include(t => t.NhanVienTao)
                .Where(t => allowedTypes.Contains(t.LoaiThongBao))
                .OrderByDescending(t => t.ThoiGianTao)
                .Take(20)
                .Select(t => new ThongBaoDto
                {
                    IdThongBao = t.IdThongBao,
                    NoiDung = t.NoiDung,
                    ThoiGianTao = t.ThoiGianTao,
                    LoaiThongBao = t.LoaiThongBao,
                    IdLienQuan = t.IdLienQuan,
                    DaXem = t.DaXem,
                    TenNhanVienTao = t.NhanVienTao != null ? t.NhanVienTao.HoTen : "Hệ thống"
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // 2. API đếm số lượng chưa đọc cho Nhân viên (Đã bỏ HoTroKhachHang)
        [HttpGet("staff/unread-count")]
        public async Task<IActionResult> GetStaffUnreadCount()
        {
            var allowedTypes = new[] { "PhieuGoiMon", "DatBan", "DonHangMoi", "HeThong" };

            var count = await _context.ThongBaos
                .CountAsync(t => !t.DaXem && allowedTypes.Contains(t.LoaiThongBao));

            return Ok(new ThongBaoCountDto { UnreadCount = count });
        }

        /// <summary>
        /// API đếm số thông báo chưa đọc
        /// </summary>
        // File: ThongBaoController.cs

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            // Danh sách các loại thông báo được phép đếm (Khớp với danh sách hiển thị)
            var allowedTypes = new[] { "SuCoBan", "DonXinNghi", "Kho", "CanhBaoKho", "HeThong" };

            // Đếm những cái CHƯA XEM và thuộc các LOẠI ĐƯỢC PHÉP
            var count = await _context.ThongBaos
                .CountAsync(t => !t.DaXem && allowedTypes.Contains(t.LoaiThongBao));

            return Ok(new ThongBaoCountDto { UnreadCount = count });
        }

        /// <summary>
        /// API lấy tất cả thông báo (mới nhất trước)
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllNotifications()
        {
            // Danh sách các loại thông báo được phép hiển thị
            var allowedTypes = new[] { "SuCoBan", "DonXinNghi", "Kho", "CanhBaoKho", "HeThong" };

            var notifications = await _context.ThongBaos
                .Include(t => t.NhanVienTao)
                // THÊM DÒNG NÀY: Lọc theo danh sách yêu cầu
                .Where(t => allowedTypes.Contains(t.LoaiThongBao))
                .OrderByDescending(t => t.ThoiGianTao)
                .Take(20)
                .Select(t => new ThongBaoDto
                {
                    IdThongBao = t.IdThongBao,
                    NoiDung = t.NoiDung,
                    ThoiGianTao = t.ThoiGianTao,
                    LoaiThongBao = t.LoaiThongBao,
                    IdLienQuan = t.IdLienQuan,
                    DaXem = t.DaXem,
                    TenNhanVienTao = t.NhanVienTao != null ? t.NhanVienTao.HoTen : "Hệ thống"
                })
                .ToListAsync();

            return Ok(notifications);
        }

        /// <summary>
        /// API đánh dấu 1 thông báo là đã đọc
        /// </summary>
        [HttpPost("mark-as-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var thongBao = await _context.ThongBaos.FindAsync(id);
            if (thongBao == null) return NotFound();

            if (!thongBao.DaXem)
            {
                thongBao.DaXem = true;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
    }
}