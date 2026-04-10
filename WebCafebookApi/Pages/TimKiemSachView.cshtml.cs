// Tập tin: WebCafebookApi/Pages/TimKiemSachView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace WebCafebookApi.Pages
{
    public class TimKiemSachViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        [BindProperty(SupportsGet = true)]
        public int? IdTacGia { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdTheLoai { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdNXB { get; set; }

        public string PageTitle { get; set; } = "Thư Viện Sách";
        public string? PageDescription { get; set; }
        public List<SachCardDto> SachList { get; set; } = new List<SachCardDto>();
        public string? ErrorMessage { get; set; }

        public TimKiemSachViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();
            var sb = new StringBuilder("http://localhost:5166/api/web/thuvien/filter-by-id?");

            if (IdTacGia.HasValue) sb.Append($"idTacGia={IdTacGia.Value}");
            else if (IdTheLoai.HasValue) sb.Append($"idTheLoai={IdTheLoai.Value}");
            else if (IdNXB.HasValue) sb.Append($"idNXB={IdNXB.Value}");
            else
            {
                return RedirectToPage("/ThuVienSachView");
            }

            try
            {
                var result = await httpClient.GetFromJsonAsync<SachKetQuaTimKiemDto>(sb.ToString());
                if (result != null)
                {
                    PageTitle = result.TieuDeTrang;
                    PageDescription = result.MoTaTrang; // Dòng này sẽ hết lỗi sau khi bạn sửa file DTO
                    SachList = result.SachList;
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Lỗi khi tải dữ liệu: {ex.Message}";
            }
            return Page();
        }
    }
}