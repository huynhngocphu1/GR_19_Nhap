// Tập tin: WebCafebookApi/Pages/HoTroView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

namespace WebCafebookApi.Pages
{
    [AllowAnonymous]
    public class HoTroViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public HoTroViewModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public HoTroViewDto HoTroData { get; set; } = new HoTroViewDto();
        public string ApiBaseUrl { get; set; } = "";
        public string? JwtToken { get; set; } // Dùng để truyền Token xuống JS

        [BindProperty]
        public SendChatRequestDto Input { get; set; } = new SendChatRequestDto();

        public async Task<IActionResult> OnGetAsync()
        {
            ApiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5166";
            string? guestSessionId = null;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // 1. ĐÃ ĐĂNG NHẬP
                var idKhachHang = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var tenKhachHang = User.FindFirstValue(ClaimTypes.GivenName);

                if (string.IsNullOrEmpty(idKhachHang))
                {
                    return RedirectToPage("/Account/Login");
                }

                HoTroData.IdKhachHang = int.Parse(idKhachHang);
                HoTroData.TenKhachHang = tenKhachHang ?? "Khách hàng";

                // Đọc Token từ Session (VẪN CẦN để gửi tin nhắn)
                var token = HttpContext.Session.GetString("JwtToken");
                JwtToken = token; // Lưu token để truyền cho JavaScript

                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToPage("/Account/Login", new { ReturnUrl = "/ho-tro" });
                }

                // ======================================
                // === SỬA THEO YÊU CẦU MỚI ===
                // === (Xóa bỏ việc tải lịch sử chat) ===
                // ======================================
                // try
                // {
                //     var httpClient = _httpClientFactory.CreateClient();
                //     httpClient.BaseAddress = new Uri(ApiBaseUrl);
                //     httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                //     var lichSu = await httpClient.GetFromJsonAsync<List<ChatMessageDto>>("api/web/hotro/history");
                //     ...
                // }
                // catch ...
            }
            else
            {
                // 2. KHÁCH VÃNG LAI
                guestSessionId = HttpContext.Session.GetString("GuestChatSession");
                if (string.IsNullOrEmpty(guestSessionId))
                {
                    guestSessionId = Guid.NewGuid().ToString();
                    HttpContext.Session.SetString("GuestChatSession", guestSessionId);
                }

                HoTroData.IdKhachHang = 0;
                HoTroData.GuestSessionId = guestSessionId;
                HoTroData.TenKhachHang = "Khách vãng lai";
            }

            // 3. AI CHÀO TRƯỚC
            // (Vì cả 2 trường hợp đều không tải lịch sử (Count == 0),
            // code này sẽ luôn chạy)
            if (HoTroData.LichSuChat.Count == 0)
            {
                HoTroData.LichSuChat.Add(new ChatMessageDto
                {
                    IdChat = 0, // ID 0 để đánh dấu là tin hệ thống tự sinh
                    LoaiTinNhan = "AI", // Hoặc "Bot" tùy logic vẽ của bạn
                    NoiDung = "Chào bạn! Tôi là trợ lý ảo của Cafebook. Tôi có thể giúp gì cho bạn.",
                    ThoiGian = DateTime.Now
                });
            }

            return Page();
        }
    }
}