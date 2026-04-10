// Tập tin: WebCafebookApi/Pages/ThucDonView.cshtml.cs
using CafebookModel.Model.ModelApp; // For FilterLookupDto
using CafebookModel.Model.ModelWeb; // For ThucDonDto
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectListItem
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace WebCafebookApi.Pages
{
    public class ThucDonViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // Thuộc tính để hứng dữ liệu từ API
        public ThucDonDto? MenuResult { get; set; }
        public List<SelectListItem> LoaiSanPhamsList { get; set; } = new();
        public string? ErrorMessage { get; set; }

        // --- Thuộc tính [BindProperty] cho Form (Filters) ---
        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? LoaiId { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? GiaMin { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? GiaMax { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "ten_asc";

        [BindProperty(SupportsGet = true)]
        public int PageNum { get; set; } = 1;
        // ----------------------------------------------------

        public ThucDonViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task OnGetAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();

            try
            {
                // 1. Tải bộ lọc (Danh mục)
                var filters = await httpClient.GetFromJsonAsync<List<FilterLookupDto>>("http://localhost:5166/api/web/thucdon/filters");
                LoaiSanPhamsList.Add(new SelectListItem("Tất cả danh mục", "0"));
                if (filters != null)
                {
                    LoaiSanPhamsList.AddRange(filters.Select(f => new SelectListItem(f.Ten, f.Id.ToString())));
                }

                // 2. Tải danh sách sản phẩm (có phân trang & lọc)
                var queryString = $"?loaiId={LoaiId ?? 0}&search={Search}&sortBy={SortBy}&giaMin={GiaMin}&giaMax={GiaMax}&pageNum={PageNum}";
                MenuResult = await httpClient.GetFromJsonAsync<ThucDonDto>($"http://localhost:5166/api/web/thucdon/search{queryString}");
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Không thể tải thực đơn. Đảm bảo API (http://localhost:5166) đang chạy. Lỗi: {ex.Message}";
            }
        }
    }
}