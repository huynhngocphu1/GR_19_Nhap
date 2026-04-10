using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WebCafebookApi.Services; // SỬA: Namespace
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Net.Http; // <-- THÊM
using System.Net.Http.Json; // <-- THÊM
using CafebookModel.Model.ModelWeb; // <-- THÊM

namespace WebCafebookApi.Pages.Account // SỬA: Namespace
{
    public class QuenMatKhauViewModel : PageModel
    {
        private readonly EmailService _emailService;
        private readonly IHttpClientFactory _httpClientFactory; // <-- SỬA
        private readonly IMemoryCache _cache;
        private readonly Random _random = new Random();

        public QuenMatKhauViewModel(EmailService emailService, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _emailService = emailService;
            _httpClientFactory = httpClientFactory; // <-- SỬA
            _cache = cache;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email.")]
            [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient();

            // 1. SỬA: Kiểm tra Email có tồn tại và hợp lệ không
            try
            {
                var checkEmail = new CheckEmailRequestDto { Email = Input.Email };
                var apiResponse = await httpClient.PostAsJsonAsync("http://localhost:5166/api/web/password/check-email", checkEmail);

                if (!apiResponse.IsSuccessStatusCode)
                {
                    var error = await apiResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
                    ModelState.AddModelError(string.Empty, error?.Message ?? "Email không tồn tại hoặc tài khoản bị khóa.");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi kết nối API: {ex.Message}");
                return Page();
            }

            // 2. Tạo mã xác nhận
            string verificationCode = _random.Next(100000, 999999).ToString("D6");

            // 3. Lưu mã vào Cache
            string cacheKey = $"ForgotPassword_{Input.Email}";
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            _cache.Set(cacheKey, verificationCode, cacheEntryOptions);

            // 4. Gửi email
            try
            {
                await _emailService.SendVerificationCodeAsync(Input.Email, verificationCode);

                // 5. Chuyển hướng
                TempData["VerificationEmail"] = Input.Email;
                return RedirectToPage("./XacNhanMaView");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Không thể gửi email. Lỗi: {ex.Message}");
                return Page();
            }
        }

        private class ApiErrorResponse { public string? Message { get; set; } }
    }
}