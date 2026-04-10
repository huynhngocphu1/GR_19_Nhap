// Tập tin: WebCafebookApi/Pages/employee/SoDoBanView.cshtml.cs
using CafebookModel.Model.ModelWeb.QuanLy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
// XÓA: using System.Net.Http.Headers; (Không cần nữa)

namespace WebCafebookApi.Pages.employee
{
    [Authorize(Roles = "Quản trị viên, Phục vụ, Thu ngân, Pha chế, Bếp, Quản lý")]
    public class SoDoBanViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public int NhanVienId { get; set; }
        public string? ErrorMessage { get; set; }
        public List<KhuVucDto> KhuVucList { get; set; } = new();

        // THÊM MỚI: Thuộc tính để truyền token xuống JavaScript
        public string? JwtToken { get; set; }

        public SoDoBanViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var idNhanVienStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(idNhanVienStr, out var id);
            NhanVienId = id;

            // --- CẬP NHẬT LOGIC GỌI API ---

            // 1. LẤY TOKEN TỪ SESSION
            // (Giống hệt cách cấu hình HttpClient trong Program.cs)
            JwtToken = HttpContext.Session.GetString("JwtToken");

            if (string.IsNullOrEmpty(JwtToken))
            {
                // Nếu không có JWT token (phiên đăng nhập API hết hạn)
                // -> Về trang đăng nhập để lấy lại
                return RedirectToPage("/account/DangNhapView");
            }

            // 2. SỬ DỤNG ĐÚNG TÊN CLIENT ĐÃ ĐĂNG KÝ
            // "ApiClient" đã được Program.cs cấu hình BaseAddress
            // và tự động gắn Bearer Token từ Session.
            var httpClient = _httpClientFactory.CreateClient("ApiClient");

            // 3. XÓA BỎ TOÀN BỘ CODE LẤY TOKEN THỦ CÔNG TỪ COOKIE
            // (Khối code "Lấy token từ cookie..." đã bị xóa)

            try
            {
                // Đường dẫn API giữ nguyên (BaseAddress là http://localhost:5166)
                var response = await httpClient.GetFromJsonAsync<List<KhuVucDto>>("api/web/quanly/sodoban");
                if (response != null)
                {
                    KhuVucList = response;
                }
            }
            catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Lỗi 401: Token không hợp lệ (hết hạn, sai...)
                ErrorMessage = "Phiên đăng nhập API đã hết hạn. Vui lòng đăng nhập lại.";
                HttpContext.Session.Remove("JwtToken");
                return RedirectToPage("/account/DangNhapView");
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi tải Sơ đồ bàn: {ex.Message}. Vui lòng kiểm tra kết nối API (đang gọi {httpClient.BaseAddress}api/web/quanly/sodoban).";
            }

            return Page();
        }
    }
}