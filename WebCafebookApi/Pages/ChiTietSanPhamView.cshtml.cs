// Tập tin: WebCafebookApi/Pages/ChiTietSanPhamView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebCafebookApi.Services;
using System.Collections.Generic;
using System.Linq;

namespace WebCafebookApi.Pages
{
    public class ChiTietSanPhamViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ChiTietSanPhamViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public int SoLuong { get; set; } = 1;

        public SanPhamChiTietDto? SanPham { get; set; }
        public string? ErrorMessage { get; set; }

        public List<DanhGiaWebDto> DanhSachDanhGia { get; set; } = new List<DanhGiaWebDto>();
        public double SaoTrungBinh { get; set; } = 0;
        public int TongSoDanhGia { get; set; } = 0;

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id <= 0)
            {
                ErrorMessage = "ID sản phẩm không hợp lệ.";
                return Page();
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            // --- Bước 1: Tải sản phẩm ---
            try
            {
                // <<< SỬA LỖI 404 TẠI ĐÂY >>>
                // var productApiUrl = $"/api/web/sanpham/{Id}"; // <-- URL CŨ BỊ SAI
                var productApiUrl = $"/api/web/thucdon/{Id}"; // <-- URL ĐÚNG (theo ThucDonController.cs)

                SanPham = await client.GetFromJsonAsync<SanPhamChiTietDto>(productApiUrl);
            }
            catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ErrorMessage = "Không tìm thấy sản phẩm bạn yêu cầu.";
                return Page();
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi khi tải chi tiết sản phẩm: {ex.Message}";
                return Page();
            }

            if (SanPham == null)
            {
                ErrorMessage = "Không tìm thấy sản phẩm bạn yêu cầu.";
                return Page();
            }

            // --- Bước 2: Tải đánh giá (Code này đã đúng từ lần trước) ---
            try
            {
                var reviewsApiUrl = $"/api/web/danhgia/sanpham/{Id}";
                DanhSachDanhGia = await client.GetFromJsonAsync<List<DanhGiaWebDto>>(reviewsApiUrl) ?? new List<DanhGiaWebDto>();

                if (DanhSachDanhGia.Any())
                {
                    SaoTrungBinh = DanhSachDanhGia.Average(d => d.SoSao);
                    TongSoDanhGia = DanhSachDanhGia.Count;
                }
            }
            catch (System.Exception ex)
            {
                TempData["ReviewError"] = $"Không thể tải đánh giá: {ex.Message}";
                DanhSachDanhGia = new List<DanhGiaWebDto>();
            }

            return Page();
        }


        // (Hàm OnPostAddToCart giữ nguyên)
        public IActionResult OnPostAddToCart(int idSanPham)
        {
            var cart = HttpContext.Session.Get<List<CartItemDto>>(WebCafebookApi.Services.SessionExtensions.CartKey) ?? new List<CartItemDto>();

            var existingItem = cart.FirstOrDefault(i => i.Id == idSanPham);

            if (existingItem != null)
            {
                existingItem.SoLuong += this.SoLuong;
                TempData["CartMessage"] = $"Đã cập nhật số lượng (tổng: {existingItem.SoLuong}).";
            }
            else
            {
                cart.Add(new CartItemDto { Id = idSanPham, SoLuong = this.SoLuong });
                TempData["CartMessage"] = "Đã thêm sản phẩm vào giỏ!";
            }

            HttpContext.Session.Set(WebCafebookApi.Services.SessionExtensions.CartKey, cart);

            return RedirectToPage("/ChiTietSanPhamView", new { id = idSanPham });
        }
    }
}