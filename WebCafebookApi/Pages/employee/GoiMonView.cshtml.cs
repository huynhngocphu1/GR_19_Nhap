// Tập tin: WebCafebookApi/Pages/employee/GoiMonView.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http; // Thêm

namespace WebCafebookApi.Pages.employee
{
    [Authorize(Roles = "Quản trị viên, Phục vụ, Thu ngân, Pha chế, Bếp, Quản lý")]
    public class GoiMonViewModel : PageModel
    {
        // Các thuộc tính này sẽ được truyền xuống JavaScript
        public int IdHoaDon { get; set; }
        public int IdNhanVien { get; set; }
        public string? JwtToken { get; set; }
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            // 1. Lấy IdHoaDon từ query string (?idHoaDon=...)
            if (!int.TryParse(Request.Query["idHoaDon"], out int idHoaDon))
            {
                ErrorMessage = "Không tìm thấy IdHoaDon. Vui lòng quay lại Sơ Đồ Bàn.";
                return Page();
            }
            IdHoaDon = idHoaDon;

            // 2. Lấy IdNhanVien từ Claims (giống SoDoBanView)
            var idNhanVienStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idNhanVienStr, out int idNhanVien))
            {
                return RedirectToPage("/account/DangNhapView"); // Lỗi claim
            }
            IdNhanVien = idNhanVien;

            // 3. Lấy JwtToken từ Session (giống SoDoBanView)
            JwtToken = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(JwtToken))
            {
                return RedirectToPage("/account/DangNhapView"); // Lỗi token
            }

            return Page();
        }
    }
}