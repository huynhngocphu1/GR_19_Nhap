// Tập tin: WebCafebookApi/Pages/ChiTietSachView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
// using WebCafebookApi.Services; // ĐÃ XÓA
// using System.Linq; // ĐÃ XÓA
// using Microsoft.AspNetCore.Http; // ĐÃ XÓA
// using System.Collections.Generic; // ĐÃ XÓA

namespace WebCafebookApi.Pages
{
    public class ChiTietSachViewModel : PageModel
    {
        // ... (Constructor và OnGetAsync giữ nguyên) ...
        private readonly IHttpClientFactory _httpClientFactory;
        public ChiTietSachViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public SachChiTietDto? Sach { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id <= 0)
            {
                ErrorMessage = "ID sách không hợp lệ.";
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                Sach = await httpClient.GetFromJsonAsync<SachChiTietDto>($"api/web/thuvien/{Id}");
                if (Sach == null)
                {
                    ErrorMessage = "Không tìm thấy cuốn sách bạn yêu cầu.";
                    return Page();
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi khi tải chi tiết: {ex.Message}";
            }
            return Page();
        }


        // === CẬP NHẬT HÀM XỬ LÝ GIỎ HÀNG ===
        // 
        //  ĐÃ XÓA TOÀN BỘ PHƯƠNG THỨC OnPostAddToCart TỪ ĐÂY
        //
    }
}