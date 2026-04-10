// Tập tin: WebCafebookApi/Pages/ChinhSachView.cshtml.cs
using CafebookModel.Model.ModelWeb; // THÊM DTO MỚI
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json; // THÊM
using System.Threading.Tasks; // THÊM

namespace WebCafebookApi.Pages
{
    public class ChinhSachViewModel : PageModel
    {
        // THÊM MỚI: Khởi tạo HttpClient
        private readonly IHttpClientFactory _httpClientFactory;
        public ChinhSachViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // THÊM MỚI: Model để chứa dữ liệu
        public ChinhSachDto ChinhSach { get; set; } = new ChinhSachDto();

        public async Task OnGetAsync()
        {
            // THÊM MỚI: Logic tải dữ liệu động
            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                var response = await httpClient.GetFromJsonAsync<ChinhSachDto>("http://localhost:5166/api/web/chinhsach/data");
                if (response != null)
                {
                    ChinhSach = response;
                }
            }
            catch (Exception)
            {
                // Nếu API lỗi, ChinhSach sẽ dùng giá trị mặc định đã khởi tạo
            }
        }
    }
}