using CafebookModel.Model.ModelWeb; // <-- THÊM
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Net.Http; // <-- THÊM
using System.Net.Http.Json; // <-- THÊM
using System; // <-- THÊM

namespace WebCafebookApi.Pages.Account // SỬA: Namespace
{
    public class DatLaiMatKhauViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory; // <-- SỬA
        private readonly IMemoryCache _cache;

        public DatLaiMatKhauViewModel(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory; // <-- SỬA
            _cache = cache;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [HiddenInput]
            public string Email { get; set; } = string.Empty;

            [Required]
            [HiddenInput]
            public string Token { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
            [StringLength(100, ErrorMessage = "Mật khẩu mới phải dài ít nhất 6 ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu mới")]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu mới")]
            [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public IActionResult OnGet(string email, string token)
        {
            string resetCacheKey = $"ResetToken_{email}";
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) ||
                !_cache.TryGetValue(resetCacheKey, out string? cachedToken) || cachedToken != token)
            {
                ErrorMessage = "Đường dẫn đặt lại mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.";
            }
            else
            {
                Input.Email = email;
                Input.Token = token;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string email = Input.Email;
            string token = Input.Token;
            string resetCacheKey = $"ResetToken_{email}";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) ||
                !_cache.TryGetValue(resetCacheKey, out string? cachedToken) || cachedToken != token)
            {
                ErrorMessage = "Phiên làm việc đã hết hạn. Vui lòng yêu cầu lại mã.";
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient();
            var resetRequest = new ResetPasswordRequestDto
            {
                Email = email,
                NewPassword = Input.NewPassword
            };

            try
            {
                var response = await httpClient.PostAsJsonAsync("http://localhost:5166/api/web/password/reset", resetRequest);

                if (response.IsSuccessStatusCode)
                {
                    _cache.Remove(resetCacheKey);
                    TempData["LoginMessage"] = "Mật khẩu của bạn đã được đặt lại thành công. Vui lòng đăng nhập.";
                    return RedirectToPage("./DangNhapView");
                }
                else
                {
                    ErrorMessage = "Đã xảy ra lỗi khi đặt lại mật khẩu. Vui lòng thử lại.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi kết nối API: {ex.Message}";
                return Page();
            }
        }
    }
}