// Tập tin: WebCafebookApi/Pages/LienHeView.cshtml.cs
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Threading.Tasks;

// SỬA: Đảm bảo namespace chính xác
namespace WebCafebookApi.Pages
{
    public class LienHeViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LienHeViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // SỬA: Dùng DTO mới
        public LienHeDto Info { get; set; } = new LienHeDto();

        [BindProperty]
        public PhanHoiInputModel PhanHoiInput { get; set; } = new PhanHoiInputModel();

        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                // SỬA: Gọi API mới
                var response = await httpClient.GetFromJsonAsync<LienHeDto>("http://localhost:5166/api/web/lienhe/info");
                if (response != null)
                {
                    Info = response;
                }
            }
            catch (Exception)
            {
                Info = new LienHeDto(); // Khởi tạo rỗng nếu lỗi
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            try
            {
                // Logic giả lập gửi phản hồi (vì chưa có API)
                System.Diagnostics.Debug.WriteLine($"--- PHAN HOI MOI ---");
                System.Diagnostics.Debug.WriteLine($"Ten: {PhanHoiInput.Ten}");
                System.Diagnostics.Debug.WriteLine($"Email: {PhanHoiInput.Email}");
                System.Diagnostics.Debug.WriteLine($"Noi Dung: {PhanHoiInput.NoiDung}");
                System.Diagnostics.Debug.WriteLine($"---------------------");

                SuccessMessage = "Cảm ơn bạn! Chúng tôi đã nhận được góp ý.";

                ModelState.Clear();
                PhanHoiInput = new PhanHoiInputModel();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
            }

            await OnGetAsync();
            return Page();
        }

        public class PhanHoiInputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập tên của bạn.")]
            public string Ten { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập email.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập nội dung.")]
            [MinLength(10, ErrorMessage = "Nội dung phải có ít nhất 10 ký tự.")]
            public string NoiDung { get; set; } = string.Empty;
        }
    }
}