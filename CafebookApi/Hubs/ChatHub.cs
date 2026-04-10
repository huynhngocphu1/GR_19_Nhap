// Tập tin: CafebookApi/Hubs/ChatHub.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace CafebookApi.Hubs
{
    //[Authorize]
    public class ChatHub : Hub
    {
        private readonly CafebookDbContext _context;

        // SỬA LỖI: Xóa AiService và AiToolService khỏi constructor
        // Hub không thể inject dịch vụ Scoped một cách an toàn
        public ChatHub(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Khi client (khách hoặc NV) kết nối, gọi hàm này để tham gia "phòng" chat.
        /// </summary>
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Khách hàng (HoTroView.cshtml) gửi tin nhắn TRỰC TIẾP cho nhân viên
        /// (Sau khi AI đã chuyển)
        /// </summary>
        public async Task SendMessageFromClient(string groupName, string noiDung, int? idKhachHang, string? guestSessionId, int? idThongBaoHoTro)
        {
            // 1. Lưu tin nhắn của khách
            var msgKhach = await SaveChatHistoryAsync(idKhachHang, guestSessionId, null, noiDung, "KhachHang", idThongBaoHoTro);

            // 2. Gửi tin nhắn này cho TẤT CẢ client trong nhóm
            await Clients.Group(groupName).SendAsync("ReceiveMessage", MapToChatDto(msgKhach));
        }

        /// <summary>
        /// Nhân viên (HoTroKhachHangView.cshtml) gửi tin nhắn
        /// (Thay thế cho HoTroKhachHangControllers/reply)
        /// </summary>
        public async Task SendMessageFromStaff(string groupName, string noiDung, int idThongBao, int? idKhachHang, string? guestSessionId)
        {
            int idNhanVien = 1; // Tạm hardcode

            // 1. Lưu tin nhắn của nhân viên
            var msgNV = await SaveChatHistoryAsync(idKhachHang, guestSessionId, idNhanVien, noiDung, "NhanVien", idThongBao);

            // 2. Cập nhật trạng thái phiếu
            var ticket = await _context.ThongBaoHoTros.FindAsync(idThongBao);
            if (ticket != null && ticket.TrangThai != "Đã xử lý")
            {
                ticket.TrangThai = "Đã trả lời";
                ticket.IdNhanVien = idNhanVien;
                ticket.ThoiGianPhanHoi = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            // 3. Gửi tin nhắn này cho TẤT CẢ client trong nhóm
            await Clients.Group(groupName).SendAsync("ReceiveMessage", MapToChatDto(msgNV));

            // 4. Gửi tín hiệu reload danh sách ticket
            await Clients.All.SendAsync("ReloadTicketList");
        }


        // --- CÁC HÀM HELPER (Tái sử dụng) ---

        private async Task<ChatLichSu> SaveChatHistoryAsync(int? idKhachHang, string? guestSessionId, int? idNhanVien, string traLoi, string loaiTinNhan, int? idThongBao)
        {
            var lichSu = new ChatLichSu
            {
                IdKhachHang = (idKhachHang > 0) ? idKhachHang : null,
                GuestSessionId = guestSessionId,
                IdNhanVien = idNhanVien,
                NoiDungHoi = "Chat Realtime",
                NoiDungTraLoi = traLoi,
                ThoiGian = DateTime.Now,
                LoaiChat = "Web_SignalR", // Đánh dấu là tin nhắn Realtime
                LoaiTinNhan = loaiTinNhan,
                IdThongBaoHoTro = idThongBao
            };
            _context.ChatLichSus.Add(lichSu);
            await _context.SaveChangesAsync();
            return lichSu;
        }

        private ChatMessageDto MapToChatDto(ChatLichSu entity)
        {
            return new ChatMessageDto
            {
                IdChat = entity.IdChat,
                NoiDung = entity.NoiDungTraLoi,
                ThoiGian = entity.ThoiGian,
                LoaiTinNhan = entity.LoaiTinNhan ?? "AI",
                IdThongBaoHoTro = entity.IdThongBaoHoTro
            };
        }
    }
}