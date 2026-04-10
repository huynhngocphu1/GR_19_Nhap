// Tập tin: CafebookModel/Model/ModelWeb/HoTroDto.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization; // <-- THÊM DÒNG NÀY

namespace CafebookModel.Model.ModelWeb
{
    /// <summary>
    /// DTO chính cho trang Hỗ trợ
    /// </summary>
    public class HoTroViewDto
    {
        public List<ChatMessageDto> LichSuChat { get; set; } = new List<ChatMessageDto>();
        public int IdKhachHang { get; set; } // Sẽ = 0 nếu là khách vãng lai
        public string? GuestSessionId { get; set; } // Sẽ có giá trị nếu là khách vãng lai
        public string TenKhachHang { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cho một tin nhắn (cả của khách, AI và NV)
    /// </summary>
    public class ChatMessageDto
    {
        public long IdChat { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public DateTime ThoiGian { get; set; }

        public string LoaiTinNhan { get; set; } = "KhachHang";
        public int? IdThongBaoHoTro { get; set; }
        // === SỬA LỖI: Thêm [JsonIgnore] ===
        // Những thuộc tính này chỉ dùng cho Razor, không dùng cho API
        [JsonIgnore]
        public bool IsUser => LoaiTinNhan == "KhachHang";
        [JsonIgnore]
        public bool IsBot => LoaiTinNhan == "AI" || LoaiTinNhan == "NhanVien";
        [JsonIgnore]
        public string AvatarCssClass => IsBot ? "avatar-bot" : "avatar-user";
        // === KẾT THÚC SỬA LỖI ===
    }

    /// <summary>
    /// DTO khi khách hàng gửi tin nhắn mới
    /// </summary>
    public class SendChatRequestDto
    {
        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        [MaxLength(1000)]
        public string NoiDung { get; set; } = string.Empty;

        // Thêm trường này để JS gửi lên
        public string? GuestSessionId { get; set; }
    }

    /// <summary>
    /// DTO API trả về (gồm 1 hoặc 2 tin nhắn)
    /// </summary>
    public class SendChatResponseDto
    {
        public ChatMessageDto TinNhanCuaKhach { get; set; } = null!;
        public ChatMessageDto? TinNhanPhanHoi { get; set; } // Có thể null nếu AI chuyển tiếp
        public bool DaChuyenNhanVien { get; set; } = false;
        public int? IdThongBaoHoTro { get; set; } // Để JS biết khi nào kích hoạt SignalR
    }
}