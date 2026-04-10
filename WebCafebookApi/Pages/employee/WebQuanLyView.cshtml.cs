using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // <-- THÊM

namespace WebCafebookApi.Pages.employee
{
    // SỬA: Thêm [Authorize] với các vai trò thực tế từ CSDL
    [Authorize(Roles = "Quản trị viên, Phục vụ, Thu ngân, Pha chế, Bếp, Quản lý")]
    public class WebQuanLyViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public WebQuanLyViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public WebQuanLyDashboardDto DashboardData { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var idNhanVienStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // SỬA: Không cần kiểm tra ID nữa vì [Authorize] đã làm việc đó

            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                var response = await httpClient.GetFromJsonAsync<WebQuanLyDashboardDto>($"http://localhost:5166/api/web/quanly/dashboard/{idNhanVienStr}");

                if (response != null)
                {
                    DashboardData = response;
                }
                else
                {
                    ErrorMessage = "Không thể tải dữ liệu dashboard.";
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi kết nối API: {ex.Message}";
            }

            return Page();
        }
    }
}