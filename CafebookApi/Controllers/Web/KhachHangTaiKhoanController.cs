// Tập tin: CafebookApi/Controllers/Web/KhachHangTaiKhoanController.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApi;
using CafebookModel.Model.ModelWeb;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
// THÊM CÁC USING SAU:
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/taikhoankhach")]
    [ApiController]
    public class KhachHangTaiKhoanController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;
        private readonly IConfiguration _config; // SỬA: Thêm IConfiguration

        public KhachHangTaiKhoanController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            _config = config; // SỬA: Thêm
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

        // --- (Hàm Register giữ nguyên) ---
        // --- (Hàm Register đã được sửa đổi theo logic mới) ---
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] DangKyRequestModel model)
        {
            // 1. Kiểm tra đầu vào cơ bản
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.SoDienThoai) || string.IsNullOrEmpty(model.Password))
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Vui lòng điền đầy đủ Email, SĐT và Mật khẩu." });
            }

            // 2. KIỂM TRA XUNG ĐỘT VỚI TÀI KHOẢN CHÍNH THỨC (Rule 5)
            // Kiểm tra không trùng Email với người khác (tài khoản chính thức)
            var emailConflict = await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.TaiKhoanTam == false && k.Email == model.Email);

            if (emailConflict != null)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Email này đã được sử dụng bởi một tài khoản khác." });
            }

            // Kiểm tra không trùng SĐT với người khác (tài khoản chính thức)
            var phoneConflict = await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.TaiKhoanTam == false && k.SoDienThoai == model.SoDienThoai);

            if (phoneConflict != null)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "SĐT này đã được sử dụng bởi một tài khoản khác." });
            }

            // 3. TÌM VÀ XỬ LÝ TÀI KHOẢN TẠM
            // Sau khi xác nhận Email và SĐT không xung đột với tài khoản chính,
            // chúng ta tìm tài khoản tạm để hợp nhất.
            var existingTempAccount = await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.TaiKhoanTam == true && (k.Email == model.Email || k.SoDienThoai == model.SoDienThoai));

            KhachHang khachHang;

            if (existingTempAccount != null)
            {
                // Đã tìm thấy tài khoản tạm -> Cập nhật và kích hoạt nó
                khachHang = existingTempAccount;

                // Áp dụng các quy tắc cập nhật/bổ sung
                bool emailMatch = khachHang.Email == model.Email;
                bool phoneMatch = khachHang.SoDienThoai == model.SoDienThoai;

                if (emailMatch && !phoneMatch)
                {
                    // Rule 1 & 4: Email đúng, SĐT sai/trống -> Cập nhật SĐT
                    khachHang.SoDienThoai = model.SoDienThoai;
                }
                else if (!emailMatch && phoneMatch)
                {
                    // Rule 2 & 4: SĐT đúng, Email sai/trống -> Cập nhật Email
                    khachHang.Email = model.Email;
                }
                // Rule 3: Cả hai đều đúng -> Không làm gì

                // Kích hoạt tài khoản
                khachHang.MatKhau = model.Password;
                khachHang.NgayTao = DateTime.Now; // Cập nhật ngày tạo là ngày đăng ký
                khachHang.TaiKhoanTam = false;
                khachHang.BiKhoa = false;

                // Cập nhật HoTen nếu nó đang là email (trường hợp tài khoản tạm chỉ có email)
                if (khachHang.HoTen == existingTempAccount.Email)
                {
                    khachHang.HoTen = model.Email; // Cập nhật HoTen thành email mới (nếu có)
                }

                _context.KhachHangs.Update(khachHang);
            }
            else
            {
                // 4. TẠO MỚI (Không tìm thấy tài khoản tạm)
                // Tạo tài khoản mới hoàn toàn
                khachHang = new KhachHang
                {
                    HoTen = model.Email, // Mặc định HoTen là Email khi đăng ký
                    Email = model.Email,
                    SoDienThoai = model.SoDienThoai,
                    TenDangNhap = model.Email,
                    MatKhau = model.Password,
                    NgayTao = DateTime.Now,
                    DiemTichLuy = 0,
                    BiKhoa = false,
                    TaiKhoanTam = false // Tài khoản chính thức
                };
                _context.KhachHangs.Add(khachHang);
            }

            // 5. LƯU THAY ĐỔI VÀ TRẢ VỀ KẾT QUẢ
            await _context.SaveChangesAsync();

            // Tạo DTO và Token
            var dto = new KhachHangDto
            {
                IdKhachHang = khachHang.IdKhachHang,
                HoTen = khachHang.HoTen,
                Email = khachHang.Email,
                SoDienThoai = khachHang.SoDienThoai,
                TenDangNhap = khachHang.TenDangNhap,
                AnhDaiDienUrl = GetFullImageUrl(khachHang.AnhDaiDien)
            };

            // Tạo JWT Token
            string token = GenerateJwtToken(khachHang);

            return Ok(new WebLoginResponseModel { Success = true, KhachHangData = dto, Token = token });
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
            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k =>
                (k.TenDangNhap == userInput || k.SoDienThoai == userInput || k.Email == userInput) &&
                (k.MatKhau == passInput)
            );
            if (khachHang == null)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Sai thông tin đăng nhập hoặc mật khẩu." });
            }

            if (khachHang.BiKhoa)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Tài khoản này đã bị khóa. Vui lòng liên hệ quản trị viên." });
            }

            if (khachHang.TaiKhoanTam)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Tài khoản này là tài khoản tạm. Vui lòng dùng chức năng Đăng Ký để kích hoạt." });
            }

            var dto = new KhachHangDto
            {
                IdKhachHang = khachHang.IdKhachHang,
                HoTen = khachHang.HoTen,
                Email = khachHang.Email,
                SoDienThoai = khachHang.SoDienThoai,
                TenDangNhap = khachHang.TenDangNhap,
                AnhDaiDienUrl = GetFullImageUrl(khachHang.AnhDaiDien)
            };

            // SỬA: Tạo và trả về JWT Token
            string token = GenerateJwtToken(khachHang);
            return Ok(new WebLoginResponseModel { Success = true, KhachHangData = dto, Token = token });
        }

        // --- THÊM MỚI: Hàm helper tạo JWT ---
        [ApiExplorerSettings(IgnoreApi = true)]
        private string GenerateJwtToken(KhachHang khachHang)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]!);

            var claims = new List<Claim>
            {
                // Thêm các Claim giống như trong DangNhapView.cshtml.cs
                new Claim(ClaimTypes.NameIdentifier, khachHang.IdKhachHang.ToString()),
                new Claim(ClaimTypes.Name, khachHang.TenDangNhap ?? khachHang.Email ?? ""),
                new Claim(ClaimTypes.GivenName, khachHang.HoTen),
                new Claim(ClaimTypes.Email, khachHang.Email ?? ""),
                new Claim(ClaimTypes.MobilePhone, khachHang.SoDienThoai ?? ""),
                new Claim(ClaimTypes.Role, "KhachHang"), // Quan trọng
                
                // Thêm Claim "IdNhanVien" (dù là khách) để đồng bộ với App,
                // nhưng set giá trị là 0 hoặc ID khách.
                // Tốt nhất là dùng claim riêng. Chúng ta sẽ dùng NameIdentifier.
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7), // Token hết hạn sau 7 ngày
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}