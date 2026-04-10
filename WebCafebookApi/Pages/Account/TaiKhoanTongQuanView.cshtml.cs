using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebCafebookApi.Pages.Account
{
    [Authorize(Roles = "KhachHang")]
    public class TaiKhoanTongQuanViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public KhachHangTongQuanDto Overview { get; set; } = new();
        public string HoTen { get; set; } = string.Empty;

        public TaiKhoanTongQuanViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);
            return userId;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // SỬA LỖI CS0103: Xóa dòng "Layout = ..." ở đây.
            // Layout đã được gán trong file .cshtml
            var userId = GetCurrentUserId();
            if (userId == 0) return Challenge();

            HoTen = User.FindFirstValue(ClaimTypes.GivenName) ?? "Khách hàng";

            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                var result = await httpClient.GetFromJsonAsync<KhachHangTongQuanDto>($"http://localhost:5166/api/web/profile/overview/{userId}");
                if (result != null)
                {
                    Overview = result;
                }
                return Page();
            }
            catch (System.Exception)
            {
                return Page();
            }
        }
    }
}