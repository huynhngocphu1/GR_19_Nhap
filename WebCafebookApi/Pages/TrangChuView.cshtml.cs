// Tập tin: WebCafebookApi/Pages/TrangChuView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace WebCafebookApi.Pages
{
    public class TrangChuViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public TrangChuViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public ThongTinChungDto? Info { get; set; }
        public List<KhuyenMaiDto> Promotions { get; set; } = new();
        public List<SanPhamDto> MonNoiBat { get; set; } = new();
        public List<SachDto> SachNoiBat { get; set; } = new();

        public async Task OnGetAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                var response = await httpClient.GetFromJsonAsync<TrangChuDto>("http://localhost:5166/api/web/trangchu/data");

                if (response != null)
                {
                    Info = response.Info;
                    Promotions = response.Promotions;
                    MonNoiBat = response.MonNoiBat;
                    SachNoiBat = response.SachNoiBat;
                }
            }
            catch (Exception)
            {
                Info = new ThongTinChungDto();
            }
        }
    }
}