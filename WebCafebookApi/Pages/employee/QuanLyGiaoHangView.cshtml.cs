using CafebookModel.Model.ModelWeb.QuanLy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WebCafebookApi.Pages.employee
{
    [Authorize(Roles = "Quản trị viên, Quản lý, Thu ngân, Phục vụ")]
    public class QuanLyGiaoHangViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public QuanLyGiaoHangViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public GiaoHangViewDto GiaoHangData { get; set; } = new GiaoHangViewDto();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; } = "Chờ xác nhận";

        // Để upload ảnh
        [BindProperty]
        public IFormFile? UploadImage { get; set; }

        [BindProperty]
        public int OrderIdToComplete { get; set; }

        [TempData]
        public string? Message { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }

        public string[] StatusTabs { get; set; } = {
            "Chờ xác nhận", "Đang chuẩn bị", "Chờ lấy hàng",
            "Đang giao", "Hoàn thành", "Đã Hủy", "Tất cả"
        };

        public async Task<IActionResult> OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                var result = await client.GetFromJsonAsync<GiaoHangViewDto>($"api/web/quanly/giaohang/load?search={Search}&status={Status}");
                if (result != null) GiaoHangData = result;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Không thể tải dữ liệu: " + ex.Message;
            }
            return Page();
        }

        // Hành động cập nhật trạng thái đơn giản
        public async Task<IActionResult> OnPostUpdateStatusAsync(int idHoaDon, string newStatus, int? shipperId)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(newStatus), "TrangThaiGiaoHang");
            if (shipperId.HasValue) content.Add(new StringContent(shipperId.ToString()!), "IdNguoiGiaoHang");

            var res = await client.PostAsync($"api/web/quanly/giaohang/update/{idHoaDon}", content);
            if (res.IsSuccessStatusCode) Message = "Cập nhật thành công!";
            else ErrorMessage = "Lỗi cập nhật.";

            return RedirectToPage(new { Search, Status });
        }

        // Hành động Hoàn thành (kèm ảnh)
        public async Task<IActionResult> OnPostCompleteOrderAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("Hoàn thành"), "TrangThaiGiaoHang");

            if (UploadImage != null)
            {
                var fileContent = new StreamContent(UploadImage.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(UploadImage.ContentType);
                content.Add(fileContent, "HinhAnhXacNhan", UploadImage.FileName);
            }

            var res = await client.PostAsync($"api/web/quanly/giaohang/update/{OrderIdToComplete}", content);
            if (res.IsSuccessStatusCode) Message = "Đã hoàn thành đơn hàng!";
            else ErrorMessage = "Lỗi khi hoàn thành đơn.";

            return RedirectToPage(new { Search, Status });
        }

        public async Task<IActionResult> OnPostConfirmAllAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            await client.PostAsync("api/web/quanly/giaohang/confirm-all-pending", null);
            Message = "Đã xác nhận tất cả đơn.";
            return RedirectToPage(new { Status = "Đang chuẩn bị" });
        }
    }
}