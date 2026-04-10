using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using System;

namespace WebCafebookApi.Pages.Account // SỬA: Namespace
{
    public class XacNhanMaViewModel : PageModel
    {
        private readonly IMemoryCache _cache;

        public XacNhanMaViewModel(IMemoryCache cache)
        {
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

            [Required(ErrorMessage = "Vui lòng nhập mã xác nhận.")]
            [Display(Name = "Mã Xác Nhận")]
            [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã xác nhận phải gồm 6 chữ số.")]
            public string VerificationCode { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            var email = TempData["VerificationEmail"] as string;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("./QuenMatKhauView");
            }

            Input.Email = email;
            TempData.Keep("VerificationEmail");
            return Page();
        }

        public IActionResult OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string email = Input.Email;
            string cacheKey = $"ForgotPassword_{email}";

            if (_cache.TryGetValue(cacheKey, out string? cachedCode))
            {
                if (cachedCode == Input.VerificationCode)
                {
                    // Mã ĐÚNG. Tạo token reset
                    string resetToken = Guid.NewGuid().ToString("N");
                    string resetCacheKey = $"ResetToken_{email}";
                    var resetCacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

                    _cache.Set(resetCacheKey, resetToken, resetCacheOptions);
                    _cache.Remove(cacheKey);
                    TempData.Remove("VerificationEmail");

                    return RedirectToPage("./DatLaiMatKhauView", new { email = email, token = resetToken });
                }
                else
                {
                    ErrorMessage = "Mã xác nhận không đúng.";
                    TempData.Keep("VerificationEmail");
                    return Page();
                }
            }
            else
            {
                ErrorMessage = "Mã xác nhận đã hết hạn hoặc không tồn tại.";
                TempData.Keep("VerificationEmail");
                return Page();
            }
        }
    }
}