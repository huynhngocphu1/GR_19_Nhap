// Tập tin: WebCafebookApi/Pages/GioHangView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebCafebookApi.Services;
using System.Linq;

namespace WebCafebookApi.Pages
{
    public class GioHangViewPageModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public GioHangViewPageModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public GioHangViewModel Cart { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var sessionCart = HttpContext.Session.Get<List<CartItemDto>>(WebCafebookApi.Services.SessionExtensions.CartKey);
            if (sessionCart == null || !sessionCart.Any())
            {
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            var populatedItems = new List<GioHangItemViewModel>();

            foreach (var item in sessionCart)
            {
                try
                {
                    // SỬA: Xóa bỏ logic kiểm tra "Sach"
                    // Giờ chỉ tải "SanPham"
                    var sp = await httpClient.GetFromJsonAsync<SanPhamChiTietDto>($"api/web/thucdon/{item.Id}");
                    if (sp != null)
                    {
                        populatedItems.Add(new GioHangItemViewModel
                        {
                            Id = item.Id,
                            // Xóa "Loai"
                            TenHienThi = sp.TenSanPham,
                            HinhAnhUrl = sp.HinhAnhUrl,
                            DonGia = sp.DonGia,
                            SoLuong = item.SoLuong
                        });
                    }
                }
                catch (System.Exception ex)
                {
                    ErrorMessage = "Lỗi khi tải thông tin giỏ hàng: " + ex.Message;
                }
            }
            Cart.Items = populatedItems;
            return Page();
        }

        // SỬA: Xóa tham số "loai"
        public IActionResult OnPostRemove(int id)
        {
            var sessionCart = HttpContext.Session.Get<List<CartItemDto>>(WebCafebookApi.Services.SessionExtensions.CartKey);
            if (sessionCart != null)
            {
                // SỬA: Tìm chỉ bằng Id
                var itemToRemove = sessionCart.FirstOrDefault(i => i.Id == id);
                if (itemToRemove != null)
                {
                    sessionCart.Remove(itemToRemove);
                    HttpContext.Session.Set(WebCafebookApi.Services.SessionExtensions.CartKey, sessionCart);
                }
            }
            return RedirectToPage();
        }

        // SỬA: Xóa tham số "loai"
        public IActionResult OnPostUpdateQuantity(int id, int soLuong)
        {
            // SỬA: Xóa kiểm tra "Sach"
            if (soLuong <= 0)
            {
                // Nếu cập nhật số lượng về 0, hãy xóa nó
                return OnPostRemove(id);
            }

            var sessionCart = HttpContext.Session.Get<List<CartItemDto>>(WebCafebookApi.Services.SessionExtensions.CartKey);
            if (sessionCart != null)
            {
                // SỬA: Tìm chỉ bằng Id
                var itemToUpdate = sessionCart.FirstOrDefault(i => i.Id == id);
                if (itemToUpdate != null)
                {
                    itemToUpdate.SoLuong = soLuong;
                    HttpContext.Session.Set(WebCafebookApi.Services.SessionExtensions.CartKey, sessionCart);
                }
            }
            return RedirectToPage();
        }
    }
}