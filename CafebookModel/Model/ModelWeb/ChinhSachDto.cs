// Tập tin: CafebookModel/Model/ModelWeb/ChinhSachDto.cs
namespace CafebookModel.Model.ModelWeb
{
    /// <summary>
    /// DTO riêng cho Trang Chính Sách
    /// </summary>
    public class ChinhSachDto
    {
        // Sử dụng decimal cho các giá trị tiền tệ
        public decimal PhiThue { get; set; } = 15000;
        public decimal PhiTraTreMoiNgay { get; set; } = 5000;

        // Tỉ lệ tích điểm
        public decimal DiemNhanVND { get; set; } = 10000;
        public decimal DiemDoiVND { get; set; } = 1000;
    }
}