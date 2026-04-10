// Tập tin: WebCafebookApi/Pages/employee/HoTroKhachHangView.cshtml.cs
using CafebookModel.Model.ModelWeb.QuanLy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace WebCafebookApi.Pages.employee
{
    // [Authorize(Roles = "NhanVien,QuanLy")]
    public class HoTroKhachHangViewModel : PageModel
    {
        private readonly IConfiguration _configuration;

        // Xóa IHttpClientFactory vì JS sẽ gọi trực tiếp
        public HoTroKhachHangViewModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Xóa [BindProperty] Id và TicketData, JS sẽ quản lý

        public string ApiBaseUrl { get; set; } = "";
        public string? JwtToken { get; set; } // Token của nhân viên

        public void OnGet()
        {
            // Trang này giờ chỉ cần tải, không cần nạp dữ liệu cụ thể
            ApiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5166";

            // Giả định token của nhân viên được lưu trong session
            JwtToken = HttpContext.Session.GetString("JwtToken");

            // Không cần gọi API ở đây nữa
        }
    }
}