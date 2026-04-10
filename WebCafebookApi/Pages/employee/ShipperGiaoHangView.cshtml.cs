using CafebookModel.Model.ModelWeb.QuanLy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace WebCafebookApi.Pages.employee
{
    //[Authorize(Roles = "GiaoHang,PhucVu,QuanLy,QuanTriVien")]
    [Authorize]
    public class ShipperGiaoHangViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ShipperHistorySummaryDto HistoryData { get; set; } = new ShipperHistorySummaryDto();
        // Vẫn giữ ID 5 để đánh dấu loại hình là "Nội bộ"
        private const int INTERNAL_SHIPPER_TYPE_ID = 5;

        public ShipperGiaoHangViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<GiaoHangItemDto> MyOrders { get; set; } = new List<GiaoHangItemDto>();
        public List<GiaoHangItemDto> AvailableOrders { get; set; } = new List<GiaoHangItemDto>();

        [BindProperty] public IFormFile? ProofImage { get; set; }
        [BindProperty] public string? CancelReason { get; set; }
        [TempData] public string? Message { get; set; }
        [TempData] public string? Error { get; set; }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToPage("/Account/Login");

            var client = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                var result = await client.GetFromJsonAsync<GiaoHangViewDto>("api/web/quanly/giaohang/load?status=Tất cả");

                if (result != null)
                {
                    // 1. Đơn chờ lấy: 
                    // - Chưa có nhân viên nào nhận (IdNhanVien == null)
                    // - HOẶC đã gán cho chính mình (IdNhanVien == userId)
                    AvailableOrders = result.DonGiaoHang
                        .Where(x => x.TrangThaiGiaoHang == "Chờ lấy hàng" &&
                                   (x.IdNhanVien == null || x.IdNhanVien == userId))
                        .OrderByDescending(x => x.ThoiGianTao)
                        .ToList();

                    // 2. Đơn CỦA TÔI:
                    // - Phải là đơn mà CHÍNH TÔI đang giữ (IdNhanVien == userId)
                    MyOrders = result.DonGiaoHang
                        .Where(x => x.IdNhanVien == userId && x.TrangThaiGiaoHang == "Đang giao")
                        .OrderBy(x => x.ThoiGianTao)
                        .ToList();
                }
                // 2. THÊM MỚI: Tải lịch sử & Tổng tiền
                var historyResult = await client.GetFromJsonAsync<ShipperHistorySummaryDto>("api/web/quanly/giaohang/shipper-history");
                if (historyResult != null)
                {
                    HistoryData = historyResult;
                }
            }
            catch (Exception ex) { Error = "Lỗi: " + ex.Message; }
            return Page();
        }

        public async Task<IActionResult> OnPostPickupAsync(int idHoaDon)
        {
            var userId = GetCurrentUserId();
            var client = _httpClientFactory.CreateClient("ApiClient");

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("Đang giao"), nameof(GiaoHangUpdateRequestDto.TrangThaiGiaoHang));

            // Lưu loại hình là "Nội bộ" (ID=5) để thống kê
            content.Add(new StringContent(INTERNAL_SHIPPER_TYPE_ID.ToString()), nameof(GiaoHangUpdateRequestDto.IdNguoiGiaoHang));

            // QUAN TRỌNG: Lưu chính xác ID của Shipper (bạn) vào hóa đơn
            content.Add(new StringContent(userId.ToString()), nameof(GiaoHangUpdateRequestDto.IdNhanVien));

            try
            {
                var res = await client.PostAsync($"api/web/quanly/giaohang/update/{idHoaDon}", content);
                if (res.IsSuccessStatusCode) Message = "Đã nhận đơn thành công!";
                else Error = "Lỗi API: " + res.ReasonPhrase;
            }
            catch (Exception ex) { Error = "Lỗi kết nối: " + ex.Message; }

            return RedirectToPage();
        }

        // ... (Các hàm OnPostSuccessAsync và OnPostFailAsync giữ nguyên như cũ) ...
        // Lưu ý: Không cần sửa OnPostSuccessAsync vì nó chỉ cập nhật trạng thái và ảnh, 
        // IdNhanVien đã được gán lúc Pickup rồi.

        public async Task<IActionResult> OnPostSuccessAsync(int idHoaDon)
        {
            if (ProofImage == null) { Error = "Thiếu ảnh."; return RedirectToPage(); }

            var client = _httpClientFactory.CreateClient("ApiClient");
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("Hoàn thành"), nameof(GiaoHangUpdateRequestDto.TrangThaiGiaoHang));

            var fileContent = new StreamContent(ProofImage.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(ProofImage.ContentType);
            content.Add(fileContent, nameof(GiaoHangUpdateRequestDto.HinhAnhXacNhan), ProofImage.FileName);

            var res = await client.PostAsync($"api/web/quanly/giaohang/update/{idHoaDon}", content);
            if (res.IsSuccessStatusCode) Message = "Đã hoàn thành đơn!";
            else Error = "Lỗi cập nhật.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostFailAsync(int idHoaDon)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("Hủy"), nameof(GiaoHangUpdateRequestDto.TrangThaiGiaoHang));
            var res = await client.PostAsync($"api/web/quanly/giaohang/update/{idHoaDon}", content);
            if (res.IsSuccessStatusCode) Message = "Đã hủy đơn.";
            else Error = "Lỗi cập nhật.";
            return RedirectToPage();
        }
    }
}