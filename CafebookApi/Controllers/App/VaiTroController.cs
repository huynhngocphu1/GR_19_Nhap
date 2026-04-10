using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/vaitro")]
    [ApiController]
    public class VaiTroController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public VaiTroController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API Lấy tất cả Vai Trò
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.VaiTros
                .Select(v => new VaiTroDto
                {
                    IdVaiTro = v.IdVaiTro,
                    TenVaiTro = v.TenVaiTro,
                    MoTa = v.MoTa
                })
                .OrderBy(v => v.TenVaiTro)
                .ToListAsync();
            return Ok(data);
        }

        /// <summary>
        /// API Thêm mới Vai Trò
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VaiTroDto dto)
        {
            if (await _context.VaiTros.AnyAsync(v => v.TenVaiTro.ToLower() == dto.TenVaiTro.ToLower()))
            {
                return Conflict("Tên vai trò đã tồn tại.");
            }
            var entity = new VaiTro
            {
                TenVaiTro = dto.TenVaiTro,
                MoTa = dto.MoTa
            };
            _context.VaiTros.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        /// <summary>
        /// API Cập nhật Vai Trò
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VaiTroDto dto)
        {
            var entity = await _context.VaiTros.FindAsync(id);
            if (entity == null) return NotFound();

            if (await _context.VaiTros.AnyAsync(v => v.TenVaiTro.ToLower() == dto.TenVaiTro.ToLower() && v.IdVaiTro != id))
            {
                return Conflict("Tên vai trò đã tồn tại.");
            }

            entity.TenVaiTro = dto.TenVaiTro;
            entity.MoTa = dto.MoTa;
            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// API Xóa Vai Trò
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _context.NhanViens.AnyAsync(nv => nv.IdVaiTro == id))
            {
                return Conflict("Không thể xóa. Vai trò này đang được gán cho nhân viên.");
            }

            var entity = await _context.VaiTros.FindAsync(id);
            if (entity == null) return NotFound();

            _context.VaiTros.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}