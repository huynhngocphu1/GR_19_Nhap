// Tập tin: WebCafebookApi/Pages/employee/DangNhapEmployee.cshtml.cs
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CafebookModel.Model.ModelApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using CafebookModel.Model.Data; // Cần thiết cho LoginRequestModel & WebLoginResponseModel
using System.Collections.Generic; // Cần thiết cho List<Claim>
using System; // Cần thiết cho DateTimeOffset
using System.Threading.Tasks; // Cần thiết cho Task
using Microsoft.AspNetCore.Http; // Cần thiết cho Session

namespace WebCafebookApi.Pages.employee
{
    public class DangNhapEmployeeModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public DangNhapEmployeeModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập tài khoản")]
            public string TenDangNhap { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
            [DataType(DataType.Password)]
            public string MatKhau { get; set; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // Xóa JWT Token khỏi session nếu có
            HttpContext.Session.Remove("JwtToken");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Dùng "ApiClient" đã cấu hình BaseAddress
            var httpClient = _httpClientFactory.CreateClient("ApiClient");

            var apiRequest = new LoginRequestModel
            {
                TenDangNhap = Input.TenDangNhap,
                MatKhau = Input.MatKhau
            };

            // Dùng đường dẫn tương đối
            var response = await httpClient.PostAsJsonAsync("api/web/taikhoannv/login", apiRequest);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<WebLoginResponseModel>();

                // SỬA: Kiểm tra cả Token
                if (apiResponse != null && apiResponse.Success && apiResponse.NhanVienData != null && !string.IsNullOrEmpty(apiResponse.Token))
                {
                    var user = apiResponse.NhanVienData;

                    // --- VIỆC 1: TẠO COOKIE XÁC THỰC (Cho Razor Pages) ---
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.IdNhanVien.ToString()),
                        new Claim(ClaimTypes.Name, user.HoTen ?? ""),
                        new Claim(ClaimTypes.GivenName, user.HoTen ?? ""),
                        new Claim(ClaimTypes.Role, user.TenVaiTro ?? "NhanVien") // Rất quan trọng
                    };
                    foreach (var quyen in user.DanhSachQuyen)
                    {
                        claims.Add(new Claim("Permission", quyen));
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                        IsPersistent = true // Tùy chọn: ghi nhớ đăng nhập
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // --- VIỆC 2: LƯU VÀO SESSION (Cho HttpClient/API) ---

                    // THÊM DÒNG NÀY: Lưu JWT Token để HttpClient tự động sử dụng
                    HttpContext.Session.SetString("JwtToken", apiResponse.Token);

                    // Lưu Avatar (Giữ nguyên)
                    HttpContext.Session.SetString("AvatarUrl", user.AnhDaiDien ?? "");

                    // Chuyển hướng đến trang Dashboard
                    return LocalRedirect(Url.Content("~/Employee/Dashboard"));
                }
                else
                {
                    // Cập nhật thông báo lỗi
                    ErrorMessage = apiResponse?.Message ?? "Lỗi: API trả về dữ liệu không hợp lệ (thiếu Token hoặc NhanVienData).";
                    return Page();
                }
            }

            ErrorMessage = "Không thể kết nối đến máy chủ đăng nhập.";
            return Page();
        }
    }
}