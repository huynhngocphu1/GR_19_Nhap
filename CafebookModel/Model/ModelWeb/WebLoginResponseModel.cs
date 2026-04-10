using CafebookModel.Model.Data; // Cho NhanVienDto
using CafebookModel.Model.ModelWeb; // Cho KhachHangDto

namespace CafebookModel.Model.ModelApi
{
    public class WebLoginResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; } // Thêm thuộc tính để chứa JWT
        public KhachHangDto? KhachHangData { get; set; }
        public NhanVienDto? NhanVienData { get; set; } // Tận dụng NhanVienDto từ WPF
    }
}