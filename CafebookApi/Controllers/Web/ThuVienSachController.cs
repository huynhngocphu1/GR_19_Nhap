// Tập tin: CafebookApi/Controllers/Web/ThuVienSachController.cs
using CafebookApi.Data;
using CafebookModel.Model.ModelWeb;
using CafebookModel.Model.ModelApp;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/thuvien")]
    [ApiController]
    public class ThuVienSachController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public ThuVienSachController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://127.0.0.1:5166";
        }

        // Helper lấy URL ảnh
        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }

        // API lấy bộ lọc (Giữ nguyên)
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            var filters = new CafebookModel.Model.ModelWeb.SachFiltersDto
            {
                TheLoais = await _context.TheLoais
                    .OrderBy(d => d.TenTheLoai)
                    .Select(d => new CafebookModel.Model.ModelApp.FilterLookupDto { Id = d.IdTheLoai, Ten = d.TenTheLoai })
                    .ToListAsync()
            };
            return Ok(filters);
        }

        // API trang thư viện chính (Giữ nguyên)
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? search,
            [FromQuery] int? theLoaiId,
            [FromQuery] string? trangThai,
            [FromQuery] string sortBy = "ten_asc",
            [FromQuery] int pageNum = 1,
            [FromQuery] int pageSize = 12)
        {
            var query = _context.Sachs.AsQueryable();

            if (theLoaiId.HasValue && theLoaiId > 0)
                query = query.Where(s => s.SachTheLoais.Any(stl => stl.IdTheLoai == theLoaiId));

            if (trangThai == "con_sach")
                query = query.Where(s => s.SoLuongHienCo > 0);
            else if (trangThai == "het_sach")
                query = query.Where(s => s.SoLuongHienCo <= 0);

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(s =>
                    s.TenSach.ToLower().Contains(searchLower) ||
                    s.SachTacGias.Any(stg => stg.TacGia.TenTacGia.ToLower().Contains(searchLower))
                );
            }

            query = sortBy switch
            {
                "ten_desc" => query.OrderByDescending(s => s.TenSach),
                "gia_asc" => query.OrderBy(s => s.GiaBia),
                "gia_desc" => query.OrderByDescending(s => s.GiaBia),
                _ => query.OrderBy(s => s.TenSach),
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items_raw = await query
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new {
                    s.IdSach,
                    s.TenSach,
                    TacGia = string.Join(", ", s.SachTacGias.Select(stg => stg.TacGia.TenTacGia)),
                    s.GiaBia,
                    s.SoLuongHienCo,
                    s.AnhBia
                })
                .ToListAsync();

            var items_dto = items_raw.Select(s => new SachCardDto
            {
                IdSach = s.IdSach,
                TieuDe = s.TenSach,
                TacGia = string.IsNullOrEmpty(s.TacGia) ? "Không rõ" : s.TacGia,
                GiaBia = s.GiaBia ?? 0,
                SoLuongCoSan = s.SoLuongHienCo,
                AnhBiaUrl = GetFullImageUrl(s.AnhBia)
            }).ToList();

            var result = new SachPhanTrangDto
            {
                Items = items_dto,
                TotalPages = totalPages,
                CurrentPage = pageNum
            };
            return Ok(result);
        }

        // API trang chi tiết sách (SỬA LẠI)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSachChiTiet(int id)
        {
            var sach = await _context.Sachs
                .Include(s => s.SachTacGias).ThenInclude(stg => stg.TacGia)
                .Include(s => s.SachTheLoais).ThenInclude(stl => stl.TheLoai)
                .Include(s => s.SachNhaXuatBans).ThenInclude(snxb => snxb.NhaXuatBan)
                .Include(s => s.DeXuatSachGocs).ThenInclude(ds => ds.SachDeXuat)
                .Where(s => s.IdSach == id)
                .Select(s => new SachChiTietDto
                {
                    IdSach = s.IdSach,
                    TieuDe = s.TenSach,
                    AnhBiaUrl = s.AnhBia, // Lấy đường dẫn thô
                    MoTa = s.MoTa,
                    GiaBia = s.GiaBia ?? 0,
                    ViTri = s.ViTri,
                    TongSoLuong = s.SoLuongTong,
                    SoLuongCoSan = s.SoLuongHienCo,
                    TacGias = s.SachTacGias.Select(stg => new TacGiaDto { IdTacGia = stg.IdTacGia, TenTacGia = stg.TacGia.TenTacGia }).ToList(),
                    TheLoais = s.SachTheLoais.Select(stl => new TheLoaiDto { IdTheLoai = stl.IdTheLoai, TenTheLoai = stl.TheLoai.TenTheLoai }).ToList(),
                    NhaXuatBans = s.SachNhaXuatBans.Select(snxb => new NhaXuatBanDto { IdNhaXuatBan = snxb.IdNhaXuatBan, TenNhaXuatBan = snxb.NhaXuatBan.TenNhaXuatBan }).ToList(),
                    GoiY = s.DeXuatSachGocs.Select(ds => new SachCardDto
                    {
                        IdSach = ds.IdSachDeXuat,
                        TieuDe = ds.SachDeXuat.TenSach,
                        AnhBiaUrl = ds.SachDeXuat.AnhBia, // Lấy đường dẫn thô
                        GiaBia = ds.SachDeXuat.GiaBia ?? 0
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (sach == null) return NotFound();

            // Hoàn thiện URL (bên ngoài truy vấn SQL)
            sach.AnhBiaUrl = GetFullImageUrl(sach.AnhBiaUrl);
            foreach (var item in sach.GoiY)
            {
                item.AnhBiaUrl = GetFullImageUrl(item.AnhBiaUrl);
            }

            return Ok(sach);
        }

        // === API TÌM KIẾM ĐÃ CẬP NHẬT (thêm MoTaTrang) ===
        [HttpGet("filter-by-id")]
        public async Task<IActionResult> SearchSachById([FromQuery] int? idTacGia, [FromQuery] int? idTheLoai, [FromQuery] int? idNXB)
        {
            var query = _context.Sachs.AsQueryable();
            var resultDto = new SachKetQuaTimKiemDto();

            if (idTacGia.HasValue)
            {
                var tacGia = await _context.TacGias.FindAsync(idTacGia.Value);
                if (tacGia == null) return NotFound("Không tìm thấy tác giả.");

                resultDto.TieuDeTrang = $"Sách của tác giả: {tacGia.TenTacGia}";
                resultDto.MoTaTrang = tacGia.GioiThieu; // <-- CẬP NHẬT MÔ TẢ

                query = query.Where(s => s.SachTacGias.Any(stg => stg.IdTacGia == idTacGia.Value));
            }
            else if (idTheLoai.HasValue)
            {
                var theLoai = await _context.TheLoais.FindAsync(idTheLoai.Value);
                if (theLoai == null) return NotFound("Không tìm thấy thể loại.");

                resultDto.TieuDeTrang = $"Sách thuộc thể loại: {theLoai.TenTheLoai}";
                resultDto.MoTaTrang = theLoai.MoTa; // <-- CẬP NHẬT MÔ TẢ

                query = query.Where(s => s.SachTheLoais.Any(stl => stl.IdTheLoai == idTheLoai.Value));
            }
            else if (idNXB.HasValue)
            {
                var nxb = await _context.NhaXuatBans.FindAsync(idNXB.Value);
                if (nxb == null) return NotFound("Không tìm thấy NXB.");

                resultDto.TieuDeTrang = $"Sách của NXB: {nxb.TenNhaXuatBan}";
                resultDto.MoTaTrang = nxb.MoTa; // <-- CẬP NHẬT MÔ TẢ

                query = query.Where(s => s.SachNhaXuatBans.Any(snxb => snxb.IdNhaXuatBan == idNXB.Value));
            }
            else
            {
                return BadRequest("Cần cung cấp một tiêu chí tìm kiếm.");
            }

            var rawList = await query
                .Select(s => new {
                    s.IdSach,
                    s.TenSach,
                    s.AnhBia,
                    s.GiaBia
                })
                .ToListAsync();

            resultDto.SachList = rawList.Select(s => new SachCardDto
            {
                IdSach = s.IdSach,
                TieuDe = s.TenSach,
                AnhBiaUrl = GetFullImageUrl(s.AnhBia),
                GiaBia = s.GiaBia ?? 0
            }).ToList();

            return Ok(resultDto);
        }
    }
}