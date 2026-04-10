using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/donvichuyendoi")]
    [ApiController]
    public class DonViChuyenDoiController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public DonViChuyenDoiController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API Lấy tất cả Đơn vị chuyển đổi
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.DonViChuyenDois
                .Include(d => d.NguyenLieu)
                .Select(d => new DonViChuyenDoiDtoo
                {
                    IdChuyenDoi = d.IdChuyenDoi,
                    IdNguyenLieu = d.IdNguyenLieu,
                    TenNguyenLieu = d.NguyenLieu.TenNguyenLieu,
                    TenDonVi = d.TenDonVi,
                    GiaTriQuyDoi = d.GiaTriQuyDoi,
                    LaDonViCoBan = d.LaDonViCoBan
                })
                .OrderBy(d => d.TenNguyenLieu)
                .ThenBy(d => d.LaDonViCoBan ? 0 : 1) // Đơn vị cơ bản lên đầu
                .ThenBy(d => d.TenDonVi)
                .ToListAsync();
            return Ok(data);
        }

        /// <summary>
        /// API Thêm mới Đơn vị chuyển đổi
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DonViChuyenDoiUpdateRequestDto dto)
        {
            if (dto.IdNguyenLieu == 0) return BadRequest("Vui lòng chọn nguyên liệu.");

            // Logic: Một nguyên liệu chỉ có 1 ĐVT Cơ bản
            if (dto.LaDonViCoBan)
            {
                if (await _context.DonViChuyenDois.AnyAsync(d => d.IdNguyenLieu == dto.IdNguyenLieu && d.LaDonViCoBan))
                {
                    return Conflict("Nguyên liệu này đã có Đơn vị cơ bản. Không thể thêm.");
                }
                dto.GiaTriQuyDoi = 1; // ĐVT cơ bản luôn có hệ số 1
            }

            var entity = new DonViChuyenDoi
            {
                IdNguyenLieu = dto.IdNguyenLieu,
                TenDonVi = dto.TenDonVi,
                GiaTriQuyDoi = dto.GiaTriQuyDoi,
                LaDonViCoBan = dto.LaDonViCoBan
            };
            _context.DonViChuyenDois.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        /// <summary>
        /// API Cập nhật Đơn vị chuyển đổi
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DonViChuyenDoiUpdateRequestDto dto)
        {
            var entity = await _context.DonViChuyenDois.FindAsync(id);
            if (entity == null) return NotFound();

            if (dto.LaDonViCoBan)
            {
                if (await _context.DonViChuyenDois.AnyAsync(d => d.IdNguyenLieu == dto.IdNguyenLieu && d.LaDonViCoBan && d.IdChuyenDoi != id))
                {
                    return Conflict("Nguyên liệu này đã có Đơn vị cơ bản. Không thể cập nhật.");
                }
                dto.GiaTriQuyDoi = 1;
            }

            entity.IdNguyenLieu = dto.IdNguyenLieu;
            entity.TenDonVi = dto.TenDonVi;
            entity.GiaTriQuyDoi = dto.GiaTriQuyDoi;
            entity.LaDonViCoBan = dto.LaDonViCoBan;

            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// API Xóa Đơn vị chuyển đổi
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _context.DinhLuongs.AnyAsync(d => d.IdDonViSuDung == id))
            {
                return Conflict("Không thể xóa. Đơn vị này đang được sử dụng trong Định lượng sản phẩm.");
            }
            var entity = await _context.DonViChuyenDois.FindAsync(id);
            if (entity == null) return NotFound();

            _context.DonViChuyenDois.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}