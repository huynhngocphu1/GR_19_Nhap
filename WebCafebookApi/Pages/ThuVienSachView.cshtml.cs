// Tập tin: WebCafebookApi/Pages/ThuVienSachView.cshtml.cs
using CafebookModel.Model.ModelApp; // For FilterLookupDto
using CafebookModel.Model.ModelWeb; // For SachPhanTrangDto
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace WebCafebookApi.Pages
{
    public class ThuVienSachViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // Dữ liệu trả về
        public SachPhanTrangDto? SachResult { get; set; }
        public string? ErrorMessage { get; set; }

        // Dữ liệu cho bộ lọc
        public List<SelectListItem> TheLoaiList { get; set; } = new();
        public List<SelectListItem> TrangThaiList { get; set; } = new();
        public List<SelectListItem> SortList { get; set; } = new();

        // --- Thuộc tính [BindProperty] cho Form (Filters) ---
        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? TheLoai { get; set; } // Dùng int? cho "Tất cả"

        [BindProperty(SupportsGet = true)]
        public string? TrangThai { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "ten_asc";

        [BindProperty(SupportsGet = true)]
        public int PageNum { get; set; } = 1;

        public ThuVienSachViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task OnGetAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();

            try
            {
                // 1. Tải bộ lọc (Thể loại)
                // SỬA LỖI CS0104: Chỉ định rõ namespace ModelWeb
                var filters = await httpClient.GetFromJsonAsync<CafebookModel.Model.ModelWeb.SachFiltersDto>("http://localhost:5166/api/web/thuvien/filters");
                TheLoaiList.Add(new SelectListItem("Tất cả thể loại", "0"));
                if (filters != null)
                {
                    TheLoaiList.AddRange(filters.TheLoais.Select(f => new SelectListItem(f.Ten, f.Id.ToString())));
                }

                // 2. Tải bộ lọc tĩnh (Trạng thái, Sắp xếp)
                TrangThaiList.Add(new SelectListItem("Tất cả", "all"));
                TrangThaiList.Add(new SelectListItem("Còn sách", "con_sach"));
                TrangThaiList.Add(new SelectListItem("Hết sách", "het_sach"));

                SortList.Add(new SelectListItem("Tên (A-Z)", "ten_asc"));
                SortList.Add(new SelectListItem("Tên (Z-A)", "ten_desc"));
                SortList.Add(new SelectListItem("Tiền cọc (Thấp-Cao)", "gia_asc"));
                SortList.Add(new SelectListItem("Tiền cọc (Cao-Thấp)", "gia_desc"));

                // 3. Tải danh sách Sách (có phân trang & lọc)
                var queryString = $"?search={Search}&theLoaiId={TheLoai ?? 0}&trangThai={TrangThai}&sortBy={SortBy}&pageNum={PageNum}";
                SachResult = await httpClient.GetFromJsonAsync<SachPhanTrangDto>($"http://localhost:5166/api/web/thuvien/search{queryString}");
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Không thể tải thư viện sách. Đảm bảo API (http://localhost:5166) đang chạy. Lỗi: {ex.Message}";
            }
        }
    }
}