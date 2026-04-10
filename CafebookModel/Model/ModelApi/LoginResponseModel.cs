using CafebookModel.Model.Data;

namespace CafebookModel.Model.ModelApi
{
    public class LoginResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public NhanVienDto? UserData { get; set; }
    }
}