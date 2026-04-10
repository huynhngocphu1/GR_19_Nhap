using System.Collections.Generic;

namespace CafebookModel.Model.Data
{
    // Lớp này chứa thông tin người dùng sẽ được lưu trữ
    // trong WPF app sau khi đăng nhập thành công.
    public class NhanVienDto
    {
        public int IdNhanVien { get; set; }
        public string? HoTen { get; set; }
        public string? TenVaiTro { get; set; }
        public string? AnhDaiDien { get; set; } 
        public List<string> DanhSachQuyen { get; set; } = new List<string>();
    }
}