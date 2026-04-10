/*
* FILE ĐÃ SỬA LỖI LOGIC TRIỆT ĐỂ (V11-FINAL)
*
* GIẢI THÍCH LỖI GỐC:
* Thuộc tính [BindProperty] trên "BookingInfoModel Booking" đã khiến
* ASP.NET Core validate (kiểm tra) model này trên MỌI HÀNH ĐỘNG POST,
* kể cả khi nhấn nút "TimKiem" (vốn không cần Booking).
*
* CÁCH SỬA:
* 1. Đã XÓA [BindProperty] khỏi "public BookingInfoModel Booking".
* 2. Đã thêm [BindProperty] vào tham số của HÀM OnPostBookAsync().
* (public async Task<IActionResult> OnPostBookAsync([Bind(Prefix = "Booking")] BookingInfoModel bookingForm))
* 3. Đã đổi tên OnPostSearchAsync -> OnPostTimKiemAsync để khớp với file .cshtml.
*
* KẾT QUẢ:
* - Nút "TimKiem" sẽ KHÔNG BAO GIỜ validate "Booking" nữa -> Lỗi "Vui lòng nhập Email" sẽ biến mất.
* - Nút "Book" sẽ validate "Booking" như bình thường.
*/

using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Claims;

namespace WebCafebookApi.Pages
{
    public class DatBanViewModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _apiClient;

        public TimeSpan OpeningTime { get; set; } = new(6, 0, 0);
        public TimeSpan ClosingTime { get; set; } = new(23, 0, 0);


        public DatBanViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _apiClient = _httpClientFactory.CreateClient("WebClient");
            _apiClient.BaseAddress = new Uri("http://localhost:5166");
        }

        [BindProperty(SupportsGet = true)]
        public SearchModel Search { get; set; } = new();

        // SỬA LỖI: Đã XÓA [BindProperty] ở đây.
        // Chúng ta sẽ bind nó ở tham số hàm OnPostBookAsync.
        public BookingInfoModel Booking { get; set; } = new();

        public List<KhuVucBanDto> KhuVucList { get; set; } = new();
        public List<int> AvailableTableIds { get; set; } = new();
        public bool IsSearched { get; set; } = false;
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public string? SearchSuccessMessage { get; set; }

        public bool IsLoggedInUserMissingEmail { get; set; } = false;

        public class SearchModel
        {
            [Required(ErrorMessage = "Vui lòng chọn ngày")]
            [DataType(DataType.Date)]
            public DateTime Date { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "Vui lòng chọn giờ")]
            public string Time { get; set; } = "09:00";

            [Required, Range(1, 50, ErrorMessage = "Số người từ 1-50")]
            public int People { get; set; } = 2;
        }

        public class BookingInfoModel
        {
            public int SelectedTableId { get; set; }
            public string? SelectedTableNumber { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập họ tên")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập SĐT")]
            [Phone(ErrorMessage = "SĐT không hợp lệ")]
            public string Phone { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập Email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; } = string.Empty;

            public string? Note { get; set; }
        }

        public async Task OnGetAsync()
        {
            PopulateBookingInfoForUser();
            await LoadOpeningHoursAsync();
            await LoadAllTablesAsync();

            if (User.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(Booking.Email))
            {
                IsLoggedInUserMissingEmail = true;
            }

            if (!Request.Query.ContainsKey("handler"))
            {
                if (Search.Date == DateTime.Today)
                {
                    Search.Time = GetDefaultStartTime(OpeningTime, ClosingTime);
                }
                else
                {
                    Search.Time = OpeningTime.ToString(@"hh\:mm");
                }
            }

            // SỬA LỖI: Đổi "Search" -> "TimKiem" để khớp với URL
            if (Request.Query.ContainsKey("handler") &&
                Request.Query["handler"] == "TimKiem")
            {
                await OnPostTimKiemAsync();
            }
        }

        // SỬA LỖI: Đổi tên hàm thành "OnPostTimKiemAsync" để khớp với .cshtml
        public async Task<IActionResult> OnPostTimKiemAsync()
        {
            // Populate lại thông tin user đăng nhập (nếu có)
            PopulateBookingInfoForUser();
            await LoadOpeningHoursAsync();
            await LoadAllTablesAsync();
            if (User.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(Booking.Email))
            {
                IsLoggedInUserMissingEmail = true;
            }

            // 1. Kiểm tra validation CHỈ CỦA SearchModel
            // (Vì BookingModel không còn [BindProperty], nó sẽ không bị validate ở đây)
            if (!ModelState.IsValid)
            {
                IsSearched = false;
                ErrorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu tìm kiếm không hợp lệ.";
                return Page();
            }

            // 2. Kiểm tra Time
            if (!TimeSpan.TryParse(Search.Time, out TimeSpan time))
            {
                ModelState.AddModelError("Search.Time", "Giờ không hợp lệ, vui lòng nhập dạng HH:mm");
                IsSearched = false;
                ErrorMessage = "Giờ không hợp lệ, vui lòng nhập dạng HH:mm";
                return Page();
            }

            // 3. Gọi API
            var req = new TimBanRequestDto
            {
                NgayDat = Search.Date,
                GioDat = time,
                SoNguoi = Search.People
            };

            var response = await _apiClient.PostAsJsonAsync("/api/web/datban/tim-ban", req);
            IsSearched = true;

            if (response.IsSuccessStatusCode)
            {
                var availableTables = await response.Content.ReadFromJsonAsync<List<BanTrongDto>>() ?? new List<BanTrongDto>();
                AvailableTableIds = availableTables.Select(b => b.IdBan).ToList();
                if (!AvailableTableIds.Any())
                {
                    ErrorMessage = "Rất tiếc, không còn bàn trống phù hợp với lựa chọn của bạn.";
                }
                else
                {
                    SearchSuccessMessage = $"Đã tìm thấy {AvailableTableIds.Count} bàn trống phù hợp.";
                }
            }
            else
            {
                ErrorMessage = await response.Content.ReadAsStringAsync();
            }

            return Page();
        }

        // SỬA LỖI: Thêm [BindProperty] vào tham số hàm
        public async Task<IActionResult> OnPostBookAsync([Bind(Prefix = "Booking")] BookingInfoModel bookingForm)
        {
            // Gán model được bind từ form vào property chính để hiển thị lại
            Booking = bookingForm;

            // Chạy lại logic load trang
            if (User.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(Booking.FullName))
            {
                PopulateBookingInfoForUser();
            }
            if (User.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(Booking.Email))
            {
                IsLoggedInUserMissingEmail = true;
            }
            await LoadOpeningHoursAsync();
            await LoadAllTablesAsync();

            // Luôn set IsSearched = true để hiển thị các bàn
            IsSearched = true;

            // Kiểm tra Time (phải kiểm tra thủ công vì nó thuộc model 'Search')
            if (!TimeSpan.TryParse(Search.Time, out TimeSpan time))
            {
                ModelState.AddModelError("Search.Time", "Giờ không hợp lệ.");
                ErrorMessage = "Giờ đặt bàn không hợp lệ.";
                // Tải lại các bàn trống (nếu có)
                await PopulateAvailableTablesAsync();
                return Page();
            }

            // Giờ kiểm tra ModelState (chỉ check 'Booking' vì nó là tham số, và 'Search' vì nó là [BindProperty])
            if (!ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(ErrorMessage))
                {
                    ErrorMessage = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .FirstOrDefault()?.ErrorMessage ?? "Thông tin không hợp lệ.";
                }
                // Tải lại các bàn trống (nếu có)
                await PopulateAvailableTablesAsync();
                return Page();
            }

            // Nếu mọi thứ hợp lệ, gọi API
            var req = new DatBanWebRequestDto
            {
                IdBan = Booking.SelectedTableId,
                NgayDat = Search.Date,
                GioDat = time,
                SoLuongKhach = Search.People,
                HoTen = Booking.FullName,
                SoDienThoai = Booking.Phone,
                Email = Booking.Email,
                GhiChu = Booking.Note
            };

            var response = await _apiClient.PostAsJsonAsync("/api/web/datban/tao-yeu-cau", req);

            if (response.IsSuccessStatusCode)
            {
                SuccessMessage = "Yêu cầu đặt bàn thành công! Vui lòng chờ xác nhận (qua Email hoặc SĐT).";
                IsSearched = false;
                AvailableTableIds.Clear();
                ModelState.Clear();

                // Đặt lại Search về giá trị mặc định
                Search = new SearchModel();
                await LoadOpeningHoursAsync(); // Tải lại giờ mở cửa để tính toán
                Search.Time = (Search.Date == DateTime.Today)
                    ? GetDefaultStartTime(OpeningTime, ClosingTime)
                    : OpeningTime.ToString(@"hh\:mm");

                // Đặt lại Booking
                Booking = new BookingInfoModel();
                PopulateBookingInfoForUser(); // Điền lại thông tin nếu đăng nhập

                if (User.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(Booking.Email))
                {
                    IsLoggedInUserMissingEmail = true;
                }
            }
            else
            {
                ErrorMessage = await response.Content.ReadAsStringAsync();
                // Tải lại các bàn trống (nếu có)
                await PopulateAvailableTablesAsync();
            }

            return Page();
        }


        /// <summary>
        /// Đây là hàm helper mới, chỉ dùng cho OnPostBookAsync khi bị lỗi
        /// để tải lại danh sách bàn.
        /// </summary>
        private async Task PopulateAvailableTablesAsync()
        {
            if (TimeSpan.TryParse(Search.Time, out TimeSpan time))
            {
                var req = new TimBanRequestDto
                {
                    NgayDat = Search.Date,
                    GioDat = time,
                    SoNguoi = Search.People
                };
                var response = await _apiClient.PostAsJsonAsync("/api/web/datban/tim-ban", req);
                if (response.IsSuccessStatusCode)
                {
                    var tables = await response.Content.ReadFromJsonAsync<List<BanTrongDto>>() ?? new List<BanTrongDto>();
                    AvailableTableIds = tables.Select(b => b.IdBan).ToList();
                }
            }
            // Không set ErrorMessage ở đây, để giữ lỗi gốc (ví dụ: SĐT trùng)
        }


        // --- Helpers (Không đổi) ---
        private void PopulateBookingInfoForUser()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                // Chỉ điền nếu Booking rỗng (tránh ghi đè khi postback lỗi)
                if (string.IsNullOrEmpty(Booking.FullName) && string.IsNullOrEmpty(Booking.Phone))
                {
                    Booking.FullName = User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity.Name ?? "";
                    Booking.Phone = User.FindFirstValue(ClaimTypes.MobilePhone) ?? "";
                    Booking.Email = User.FindFirstValue(ClaimTypes.Email) ?? "";
                }
            }
        }

        private async Task LoadOpeningHoursAsync()
        {
            try
            {
                var hours = await _apiClient.GetFromJsonAsync<OpeningHoursDto>("/api/web/datban/get-opening-hours");
                if (hours != null)
                {
                    OpeningTime = hours.Open;
                    ClosingTime = hours.Close;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Loi khi tai gio mo cua: " + ex.Message);
            }
        }

        private async Task LoadAllTablesAsync()
        {
            if (!KhuVucList.Any())
            {
                try
                {
                    KhuVucList = await _apiClient.GetFromJsonAsync<List<KhuVucBanDto>>("/api/web/datban/get-all-tables-by-area") ?? new List<KhuVucBanDto>();
                }
                catch (Exception ex)
                {
                    ErrorMessage = "Lỗi khi tải danh sách bàn: " + ex.Message;
                }
            }
        }

        private string GetDefaultStartTime(TimeSpan open, TimeSpan close)
        {
            var now = DateTime.Now;
            var nowPlus10Min = now.AddMinutes(10);

            int minutes = nowPlus10Min.Minute;
            int minutesToAdd = (minutes % 30 == 0) ? 0 : (30 - (minutes % 30));
            var defaultTime = nowPlus10Min.AddMinutes(minutesToAdd);

            var defaultTimeSpan = defaultTime.TimeOfDay;

            if (defaultTimeSpan < open) return open.ToString(@"hh\:mm");
            if (defaultTimeSpan >= close) return open.ToString(@"hh\:mm");

            return defaultTime.ToString("HH:mm");
        }
    }
}