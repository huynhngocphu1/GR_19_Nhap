namespace CafebookModel.Model.ModelWeb
{
    public class KhachHangDto
    {
        public int IdKhachHang { get; set; }
        public string HoTen { get; set; } = string.Empty; // <-- SỬA LỖI CS8618
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? TenDangNhap { get; set; }
        public string? AnhDaiDienUrl { get; set; }
    }
}