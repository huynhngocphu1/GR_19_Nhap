using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public DashboardController(CafebookDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var dto = new DashboardSummaryDto();

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            DateTime startDate30Days = today.AddDays(-29);

            // 1. Lấy các hóa đơn đã thanh toán hôm nay
            var hoaDonsHomNay = _context.HoaDons
                .Where(h => h.ThoiGianThanhToan >= today &&
                            h.ThoiGianThanhToan < tomorrow &&
                            h.TrangThai == "Đã thanh toán");

            // 2. Tính KPI Thẻ
            dto.TongDoanhThuHomNay = await hoaDonsHomNay.SumAsync(h => h.ThanhTien);
            dto.TongDonHangHomNay = await hoaDonsHomNay.CountAsync();

            // 3. Lấy sản phẩm bán chạy hôm nay
            var spBanChayQuery = await _context.ChiTietHoaDons
                .Where(ct => ct.HoaDon.ThoiGianThanhToan >= today && ct.HoaDon.ThoiGianThanhToan < tomorrow && ct.HoaDon.TrangThai == "Đã thanh toán")
                .GroupBy(ct => ct.IdSanPham)
                .Select(g => new {
                    Id = g.Key,
                    SoLuong = g.Sum(ct => ct.SoLuong)
                })
                .OrderByDescending(x => x.SoLuong)
                .FirstOrDefaultAsync();

            if (spBanChayQuery != null)
            {
                var sanPham = await _context.SanPhams.FindAsync(spBanChayQuery.Id);
                dto.SanPhamBanChayHomNay = sanPham?.TenSanPham ?? "Không rõ";
            }

            // 4. Lấy dữ liệu biểu đồ 30 ngày
            var doanhThuQuery = await _context.HoaDons
                .Where(h => h.ThoiGianThanhToan >= startDate30Days && h.TrangThai == "Đã thanh toán")
                .GroupBy(h => h.ThoiGianThanhToan!.Value.Date) // Group by ngày
                .Select(g => new ChartDataPoint
                {
                    Ngay = g.Key,
                    TongTien = g.Sum(h => h.ThanhTien)
                })
                .OrderBy(dp => dp.Ngay)
                .ToListAsync();

            // Điền vào các ngày bị thiếu (nếu không có doanh thu)
            dto.DoanhThu30Ngay = Enumerable.Range(0, 30)
                .Select(i => startDate30Days.AddDays(i))
                .GroupJoin(doanhThuQuery, // Left join
                           ngay => ngay,
                           data => data.Ngay,
                           (ngay, data) => data.SingleOrDefault(new ChartDataPoint { Ngay = ngay, TongTien = 0 }))
                .ToList();

            return Ok(dto);
        }
    }
}