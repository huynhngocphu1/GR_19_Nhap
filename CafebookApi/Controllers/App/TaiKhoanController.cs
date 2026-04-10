// Tập tin: CafebookApi/Controllers/App/TaiKhoanController.cs
using CafebookApi.Data;
using CafebookModel.Model.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApi;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/taikhoan")]
    [ApiController]
    public class TaiKhoanController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;
        private readonly IConfiguration _config;

        public TaiKhoanController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            _config = config;
            _baseUrl = _config["Jwt:Issuer"] ?? "http://127.0.0.1:5166";

            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if (!Directory.Exists(_env.WebRootPath))
                {
                    Directory.CreateDirectory(_env.WebRootPath);
                }
            }
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
            var userInput = model.TenDangNhap.Trim();
            var passInput = model.MatKhau.Trim();

            var nhanVien = await _context.NhanViens
              .Include(nv => nv.VaiTro)
                .ThenInclude(vt => vt.VaiTroQuyens)
                .ThenInclude(vtq => vtq.Quyen)
              .FirstOrDefaultAsync(nv =>
                (nv.TenDangNhap == userInput || nv.SoDienThoai == userInput || nv.Email == userInput) &&
                (nv.MatKhau == passInput)
              );

            if (nhanVien == null)
            {
                return Ok(new LoginResponseModel { Success = false, Message = "Sai tên đăng nhập hoặc mật khẩu." });
            }

            if (nhanVien.VaiTro == null)
            {
                return Ok(new LoginResponseModel { Success = false, Message = "Tài khoản không có vai trò." });
            }

            var userDto = new NhanVienDto
            {
                IdNhanVien = nhanVien.IdNhanVien,
                HoTen = nhanVien.HoTen,
                AnhDaiDien = GetFullImageUrl(nhanVien.AnhDaiDien),
                // Sửa cảnh báo CS8602 (bằng cách kiểm tra null ở trên)
                TenVaiTro = nhanVien.VaiTro.TenVaiTro,
                DanhSachQuyen = nhanVien.VaiTro.VaiTroQuyens
                  .Select(vtq => vtq.IdQuyen)
                  .ToList()
            };

            // SỬA LỖI CS1503 (Lỗi dây chuyền):
            string token = GenerateJwtToken(nhanVien);

            return Ok(new LoginResponseModel
            {
                Success = true,
                Message = "Đăng nhập thành công!",
                Token = token,
                UserData = userDto
            });
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        // SỬA LỖI CS0118: Ghi rõ đầy đủ (fully qualify) tên class
        private string GenerateJwtToken(CafebookModel.Model.Entities.NhanVien nhanVien)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]!);

            var claims = new List<Claim>
            {
                new Claim("IdNhanVien", nhanVien.IdNhanVien.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, nhanVien.HoTen),
                new Claim(JwtRegisteredClaimNames.Email, nhanVien.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                // Lỗi CS1503 (BinaryReader) tự biến mất khi sửa lỗi CS0118
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}