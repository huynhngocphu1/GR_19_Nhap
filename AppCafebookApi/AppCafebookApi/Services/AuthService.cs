using CafebookModel.Model.Data;
using CafebookModel.Model.ModelApi;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AppCafebookApi.Services
{
    public static class AuthService
    {
        public static NhanVienDto? CurrentUser { get; private set; }
        public static string? AuthToken { get; private set; }

        private static readonly HttpClient _httpClient;

        static AuthService()
        {
            _httpClient = new HttpClient
            {
                // SỬA DÒNG NÀY:
                BaseAddress = new System.Uri("http://127.0.0.1:5166")
            };
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
              new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static async Task<LoginResponseModel> LoginAsync(LoginRequestModel model)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/app/taikhoan/login", model);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseModel>();
                    if (loginResponse != null && loginResponse.Success)
                    {
                        CurrentUser = loginResponse.UserData;
                        AuthToken = loginResponse.Token; // <-- Dòng quan trọng lấy token
                    }
                    return loginResponse ?? new LoginResponseModel { Success = false, Message = "Không thể đọc phản hồi." };
                }

                var errorResponse = await response.Content.ReadFromJsonAsync<LoginResponseModel>();
                return errorResponse ?? new LoginResponseModel { Success = false, Message = $"Lỗi: {response.StatusCode}" };
            }
            catch (HttpRequestException ex)
            {
                return new LoginResponseModel { Success = false, Message = $"Lỗi kết nối API: {ex.Message}" };
            }
        }

        public static LoginResponseModel LoginBackdoor(LoginRequestModel model)
        {
            AuthToken = null;
            return new LoginResponseModel { Success = false };
        }

        public static void Logout()
        {
            CurrentUser = null;
            AuthToken = null;
        }

        public static bool CoQuyen(string idQuyen)
        {
            if (CurrentUser == null || CurrentUser.DanhSachQuyen == null) return false;
            if (CurrentUser.TenVaiTro == "Quản trị viên" || CurrentUser.DanhSachQuyen.Contains("FULL")) return true;
            return CurrentUser.DanhSachQuyen.Contains(idQuyen);
        }

        public static bool CoQuyen(params string[] quyenIds)
        {
            if (CurrentUser == null || CurrentUser.DanhSachQuyen == null) return false;
            if (CurrentUser.TenVaiTro == "Quản trị viên" || CurrentUser.DanhSachQuyen.Contains("FULL")) return true;
            return quyenIds.Any(id => CurrentUser.DanhSachQuyen.Contains(id));
        }
    }
}