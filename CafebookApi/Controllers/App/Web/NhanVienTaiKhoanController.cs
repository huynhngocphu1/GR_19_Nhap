// Tập tin: CafebookApi/Controllers/Web/NhanVienTaiKhoanController.cs
using CafebookApi.Data;
using CafebookModel.Model.Data;
using CafebookModel.Model.ModelApi;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;

// --- THÊM CÁC USING ĐỂ TẠO JWT TOKEN ---
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic; // Để dùng List

// --- SỬA LỖI CS0246: THÊM DÒNG NÀY ---
using CafebookModel.Model.Entities;
// ------------------------------------


namespace CafebookApi.Controllers.Web
{
    [Route("api/web/taikhoannv")]
    [ApiController]
    public class NhanVienTaiKhoanController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;
        private readonly IConfiguration _config;

        public NhanVienTaiKhoanController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            _config = config;
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://127.0.0.1:5166";
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.TenDangNhap) || string.IsNullOrEmpty(model.MatKhau))
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Thông tin không hợp lệ." });
            }

            var userInput = model.TenDangNhap.Trim();
            var passInput = model.MatKhau.Trim();

            var nhanVien = await _context.NhanViens
                .Include(nv => nv.VaiTro)
                .ThenInclude(vt => vt.VaiTroQuyens)
                .ThenInclude(vtq => vtq.Quyen)
                .FirstOrDefaultAsync(nv =>
                    (nv.TenDangNhap == userInput || nv.SoDienThoai == userInput || nv.Email == userInput) && // Sửa lỗi chính tả SoDienThoai
                    (nv.MatKhau == passInput)
                );

            if (nhanVien == null || nhanVien.VaiTro == null)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Sai thông tin đăng nhập hoặc mật khẩu." });
            }

            var dto = new NhanVienDto
            {
                IdNhanVien = nhanVien.IdNhanVien,
                HoTen = nhanVien.HoTen,
                AnhDaiDien = GetFullImageUrl(nhanVien.AnhDaiDien),
                TenVaiTro = nhanVien.VaiTro.TenVaiTro,
                DanhSachQuyen = nhanVien.VaiTro.VaiTroQuyens
                                    .Select(vtq => vtq.IdQuyen)
                                    .ToList()
            };

            // Lỗi CS1503 (Argument 1) xảy ra ở đây vì hàm GenerateJwtToken(NhanVien nhanVien)
            // không tìm thấy kiểu 'NhanVien' (do thiếu using)
            var token = GenerateJwtToken(nhanVien);

            return Ok(new WebLoginResponseModel
            {
                Success = true,
                NhanVienData = dto,
                Token = token
            });
        }

        // Lỗi CS0246 (The type... 'NhanVien' could not be found) xảy ra ở đây
        [ApiExplorerSettings(IgnoreApi = true)]
        private string GenerateJwtToken(NhanVien nhanVien) // Kiểu 'NhanVien' cần using
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                // Các lỗi "BinaryReader" (CS1503) xảy ra ở đây do compiler bị lẫn
                new Claim(JwtRegisteredClaimNames.Sub, nhanVien.IdNhanVien.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, nhanVien.HoTen ?? ""),
                new Claim(JwtRegisteredClaimNames.Email, nhanVien.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, nhanVien.VaiTro.TenVaiTro ?? "NhanVien")
            };

            foreach (var quyen in nhanVien.VaiTro.VaiTroQuyens)
            {
                claims.Add(new Claim("Permission", quyen.IdQuyen));
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}