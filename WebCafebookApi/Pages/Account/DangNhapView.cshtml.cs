// Tập tin: WebCafebookApi/Pages/Account/DangNhapView.cshtml.cs
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CafebookModel.Model.ModelApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace WebCafebookApi.Pages.Account
{
    public class DangNhapViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public DangNhapViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập, Email hoặc SĐT")]
            [Display(Name = "Tên đăng nhập, Email hoặc SĐT")]
            public string LoginIdentifier { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; } = string.Empty;

            // ĐÃ XÓA: Thuộc tính RememberMe
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }
            returnUrl ??= Url.Content("~/");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient();
            var apiRequest = new LoginRequestModel
            {
                TenDangNhap = Input.LoginIdentifier,
                MatKhau = Input.Password
            };
            var response = await httpClient.PostAsJsonAsync("http://localhost:5166/api/web/taikhoankhach/login", apiRequest);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<WebLoginResponseModel>();
                if (apiResponse != null && apiResponse.Success && apiResponse.KhachHangData != null && apiResponse.Token != null)
                {
                    var user = apiResponse.KhachHangData;
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.IdKhachHang.ToString()),
                        new Claim(ClaimTypes.Name, user.TenDangNhap ?? user.Email ?? ""),
                        new Claim(ClaimTypes.GivenName, user.HoTen),
                        new Claim(ClaimTypes.Email, user.Email ?? ""),
                        new Claim(ClaimTypes.MobilePhone, user.SoDienThoai ?? ""),
                        new Claim(ClaimTypes.Role, "KhachHang") // Gán Role
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // SỬA: Không còn IsPersistent = Input.RememberMe
                    var authProperties = new AuthenticationProperties();

                    // 1. Đăng nhập Cookie cho Frontend
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // 2. Lưu JWT Token vào Session để ApiClient sử dụng
                    HttpContext.Session.SetString("JwtToken", apiResponse.Token);

                    HttpContext.Session.SetString("AvatarUrl", user.AnhDaiDienUrl ?? "");
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, apiResponse?.Message ?? "Lỗi không xác định từ API.");
                    return Page();
                }
            }

            ModelState.AddModelError(string.Empty, "Không thể kết nối đến máy chủ đăng nhập.");
            return Page();
        }
    }
}