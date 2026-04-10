// Tập tin: CafebookApi/Controllers/Web/ChinhSachController.cs
using CafebookApi.Data;
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/chinhsach")]
    [ApiController]
    public class ChinhSachController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public ChinhSachController(CafebookDbContext context)
        {
            _context = context;
        }

        // Hàm helper để đọc và chuyển đổi Cài đặt an toàn
        private decimal GetSettingValue(List<CafebookModel.Model.Entities.CaiDat> settings, string key, decimal defaultValue)
        {
            var valueString = settings.FirstOrDefault(c => c.TenCaiDat == key)?.GiaTri;
            if (decimal.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        /// <summary>
        /// API lấy dữ liệu động cho trang Chính sách
        /// </summary>
        [HttpGet("data")]
        public async Task<IActionResult> GetChinhSachData()
        {
            var keysToFetch = new[] {
                "Sach_PhiThue",
                "Sach_PhiTraTreMoiNgay",
                "DiemTichLuy_NhanVND",
                "DiemTichLuy_DoiVND"
            };

            var settings = await _context.CaiDats
                .Where(c => keysToFetch.Contains(c.TenCaiDat))
                .ToListAsync();

            var dto = new ChinhSachDto
            {
                PhiThue = GetSettingValue(settings, "Sach_PhiThue", 15000),
                PhiTraTreMoiNgay = GetSettingValue(settings, "Sach_PhiTraTreMoiNgay", 5000),
                DiemNhanVND = GetSettingValue(settings, "DiemTichLuy_NhanVND", 10000),
                DiemDoiVND = GetSettingValue(settings, "DiemTichLuy_DoiVND", 1000)
            };

            return Ok(dto);
        }
    }
}