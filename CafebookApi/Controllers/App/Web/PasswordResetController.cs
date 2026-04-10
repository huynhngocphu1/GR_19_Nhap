using CafebookApi.Data;
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/password")]
    [ApiController]
    public class PasswordResetController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public PasswordResetController(CafebookDbContext context)
        {
            _context = context;
        }

        // API kiểm tra Email
        [HttpPost("check-email")]
        public async Task<IActionResult> CheckEmailExists([FromBody] CheckEmailRequestDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email))
                return BadRequest(new { Message = "Email là bắt buộc." });

            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(kh => kh.Email == model.Email);

            if (khachHang == null)
                return NotFound(new { Message = "Email không được tìm thấy trong hệ thống." });

            if (khachHang.BiKhoa)
                return BadRequest(new { Message = "Tài khoản này đang bị khóa. Không thể đặt lại mật khẩu." });

            return Ok(); // Email hợp lệ
        }

        // API đặt lại mật khẩu
        [HttpPost("reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.NewPassword))
                return BadRequest(new { Message = "Dữ liệu không hợp lệ." });

            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(kh => kh.Email == model.Email);

            if (khachHang == null)
                return NotFound(new { Message = "Email không được tìm thấy." });

            // (Trong thực tế, bạn phải HASH mật khẩu này)
            khachHang.MatKhau = model.NewPassword;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đặt lại mật khẩu thành công." });
        }
    }
}