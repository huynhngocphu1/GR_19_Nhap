// Tập tin: CafebookApi/Controllers/Web/LienHeController.cs
using CafebookApi.Data;
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Thêm

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/lienhe")]
    [ApiController]
    public class LienHeController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public LienHeController(CafebookDbContext context)
        {
            _context = context;
        }

        // Hàm helper để lấy giá trị an toàn
        private string? GetSettingValue(List<CafebookModel.Model.Entities.CaiDat> settings, string key)
        {
            // Trả về null nếu giá trị là chuỗi rỗng hoặc null
            var value = settings.FirstOrDefault(c => c.TenCaiDat == key)?.GiaTri;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// API lấy toàn bộ thông tin cho Trang Liên Hệ
        /// </summary>
        [HttpGet("info")]
        public async Task<IActionResult> GetContactInfo()
        {
            var keysToFetch = new[] {
                "GioiThieu", "DiaChi", "SoDienThoai", "LienHe_Email", "LienHe_GioMoCua",
                "LienHe_Facebook", "LienHe_Instagram", "LienHe_GoogleMapsEmbed", "LienHe_Zalo", "LienHe_Youtube"
            };

            var settings = await _context.CaiDats
                .Where(c => keysToFetch.Contains(c.TenCaiDat))
                .ToListAsync();

            var dto = new LienHeDto
            {
                GioiThieu = GetSettingValue(settings, "GioiThieu"),
                DiaChi = GetSettingValue(settings, "DiaChi"),
                SoDienThoai = GetSettingValue(settings, "SoDienThoai"),
                EmailLienHe = GetSettingValue(settings, "LienHe_Email"),
                GioMoCua = GetSettingValue(settings, "LienHe_GioMoCua"),
                LinkFacebook = GetSettingValue(settings, "LienHe_Facebook"),
                LinkInstagram = GetSettingValue(settings, "LienHe_Instagram"),
                LinkGoogleMapsEmbed = GetSettingValue(settings, "LienHe_GoogleMapsEmbed"),
                LinkZalo = GetSettingValue(settings, "LienHe_Zalo"),
                LinkYoutube = GetSettingValue(settings, "LienHe_Youtube")
            };

            return Ok(dto);
        }
    }
}