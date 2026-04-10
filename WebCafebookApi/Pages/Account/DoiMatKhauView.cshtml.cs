using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using System; // <-- THÊM

namespace WebCafebookApi.Pages.Account
{
    [Authorize(Roles = "KhachHang")]
    public class DoiMatKhauViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        [BindProperty]
        public PasswordChangeModel PasswordInput { get; set; } = new();

        [TempData]
        public string? PasswordSuccessMessage { get; set; }
        [TempData]
        public string? PasswordErrorMessage { get; set; }

        public DoiMatKhauViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public void OnGet()
        {
            // SỬA LỖI CS0103: Xóa dòng "Layout = ..." ở đây.
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);
            return userId;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // SỬA LỖI CS0103: Xóa dòng "Layout = ..." ở đây.
            var userId = GetCurrentUserId();
            if (userId == 0) return Challenge();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                var response = await httpClient.PostAsJsonAsync($"http://localhost:5166/api/web/profile/change-password/{userId}", PasswordInput);

                if (response.IsSuccessStatusCode)
                {
                    await HttpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                    TempData["LoginMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
                    return RedirectToPage("/Account/DangNhapView");
                }
                else
                {
                    var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                    PasswordErrorMessage = error?.Message ?? "Lỗi không xác định.";
                }
            }
            catch (Exception ex)
            {
                PasswordErrorMessage = $"Lỗi: {ex.Message}";
            }

            return Page();
        }

        private class ApiErrorResponse { public string? Message { get; set; } }
    }
}