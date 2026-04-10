using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using CafebookModel.Model.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/banquanly")]
    [ApiController]
    public class BanQuanLyController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public BanQuanLyController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy toàn bộ cây Khu Vực và Bàn
        /// </summary>
        [HttpGet("tree")]
        public async Task<IActionResult> GetKhuVucTree()
        {
            var data = await _context.KhuVucs
                .AsNoTracking()
                .Select(kv => new KhuVucDto // Chuyển đổi sang DTO
                {
                    IdKhuVuc = kv.IdKhuVuc,
                    TenKhuVuc = kv.TenKhuVuc,
                    MoTa = kv.MoTa,
                    Bans = kv.Bans.Select(b => new BanDto
                    {
                        IdBan = b.IdBan,
                        SoBan = b.SoBan,
                        SoGhe = b.SoGhe,
                        TrangThai = b.TrangThai,
                        GhiChu = b.GhiChu,
                        IdKhuVuc = b.IdKhuVuc ?? 0
                    }).ToList()
                })
                .ToListAsync();

            return Ok(data);
        }

        // --- API CHO KHU VỰC ---

        [HttpPost("khuvuc")]
        public async Task<IActionResult> CreateKhuVuc([FromBody] KhuVucUpdateRequestDto dto)
        {
            var khuVuc = new KhuVuc
            {
                TenKhuVuc = dto.TenKhuVuc,
                MoTa = dto.MoTa
            };
            _context.KhuVucs.Add(khuVuc);
            await _context.SaveChangesAsync();
            return Ok(khuVuc); // Trả về đối tượng đã tạo
        }

        [HttpPut("khuvuc/{id}")]
        public async Task<IActionResult> UpdateKhuVuc(int id, [FromBody] KhuVucUpdateRequestDto dto)
        {
            var khuVuc = await _context.KhuVucs.FindAsync(id);
            if (khuVuc == null) return NotFound();

            khuVuc.TenKhuVuc = dto.TenKhuVuc;
            khuVuc.MoTa = dto.MoTa;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("khuvuc/{id}")]
        public async Task<IActionResult> DeleteKhuVuc(int id)
        {
            // Kiểm tra ràng buộc (còn bàn không)
            if (await _context.Bans.AnyAsync(b => b.IdKhuVuc == id))
            {
                return Conflict("Không thể xóa khu vực đang chứa bàn. Vui lòng di chuyển hoặc xóa các bàn trước.");
            }

            var khuVuc = await _context.KhuVucs.FindAsync(id);
            if (khuVuc == null) return NotFound();

            _context.KhuVucs.Remove(khuVuc);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // --- API CHO BÀN ---

        [HttpPost("ban")]
        public async Task<IActionResult> CreateBan([FromBody] BanUpdateRequestDto dto)
        {
            var ban = new Ban
            {
                SoBan = dto.SoBan,
                SoGhe = dto.SoGhe,
                IdKhuVuc = dto.IdKhuVuc,
                TrangThai = "Trống", // Mặc định khi thêm mới
                GhiChu = dto.GhiChu
            };
            _context.Bans.Add(ban);
            await _context.SaveChangesAsync();
            return Ok(ban);
        }

        [HttpPut("ban/{id}")]
        public async Task<IActionResult> UpdateBan(int id, [FromBody] BanUpdateRequestDto dto)
        {
            var ban = await _context.Bans.FindAsync(id);
            if (ban == null) return NotFound();

            ban.SoBan = dto.SoBan;
            ban.SoGhe = dto.SoGhe;
            ban.IdKhuVuc = dto.IdKhuVuc;   // Cho phép di chuyển bàn
            ban.TrangThai = dto.TrangThai; // Cho phép khóa bàn
            ban.GhiChu = dto.GhiChu;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("ban/{id}")]
        public async Task<IActionResult> DeleteBan(int id)
        {
            // Kiểm tra ràng buộc theo yêu cầu
            if (await _context.HoaDons.AnyAsync(h => h.IdBan == id && h.TrangThai != "Đã thanh toán"))
            {
                return Conflict("Không thể xóa. Bàn đang có hóa đơn CHƯA thanh toán.");
            }
            if (await _context.PhieuDatBans.AnyAsync(p => p.IdBan == id && p.ThoiGianDat > DateTime.Now))
            {
                return Conflict("Không thể xóa. Bàn đang có phiếu đặt trước CHƯA diễn ra.");
            }

            var ban = await _context.Bans.FindAsync(id);
            if (ban == null) return NotFound();

            _context.Bans.Remove(ban);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // --- API NÂNG CAO ---

        [HttpGet("ban/{id}/history")]
        public async Task<IActionResult> GetBanHistory(int id)
        {
            var history = new BanHistoryDto
            {
                SoLuotPhucVu = await _context.HoaDons.CountAsync(h => h.IdBan == id && h.TrangThai == "Đã thanh toán"),
                TongDoanhThu = await _context.HoaDons
                                    .Where(h => h.IdBan == id && h.TrangThai == "Đã thanh toán")
                                    .SumAsync(h => h.ThanhTien),
                SoLuotDatTruoc = await _context.PhieuDatBans.CountAsync(p => p.IdBan == id)
            };
            return Ok(history);
        }
    }
}