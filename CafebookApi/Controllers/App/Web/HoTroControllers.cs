// Tập tin: CafebookApi/Controllers/Web/HoTroController.cs
using CafebookApi.Data;
using CafebookApi.Services;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
// using System.Text.RegularExpressions; // <-- XÓA BỎ: Không cần parse tool nữa
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using CafebookApi.Hubs;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/hotro")]
    [ApiController]
    [AllowAnonymous]
    public class HoTroController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly AiService _aiService;
        // private readonly AiToolService _aiToolService; // <-- XÓA BỎ: Controller không cần biết đến ToolService
        private readonly IHubContext<ChatHub> _chatHubContext;

        // CẬP NHẬT CONSTRUCTOR
        public HoTroController(
            CafebookDbContext context,
            AiService aiService,
            // AiToolService aiToolService, // <-- XÓA BỎ
            IHubContext<ChatHub> chatHubContext)
        {
            _context = context;
            _aiService = aiService;
            // _aiToolService = aiToolService; // <-- XÓA BỎ
            _chatHubContext = chatHubContext;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            int.TryParse(idClaim?.Value, out int id);
            return id;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendChatRequestDto request)
        {
            var idKhachHang = GetCurrentUserId();
            string? guestSessionId = (idKhachHang == 0) ? request.GuestSessionId : null;

            if (idKhachHang == 0 && string.IsNullOrEmpty(guestSessionId))
            {
                return Unauthorized("Không thể xác định phiên làm việc.");
            }

            var responseDto = new SendChatResponseDto();

            // 1. Lưu tin nhắn của khách
            var lichSuKhach = await SaveChatHistoryAsync(idKhachHang, guestSessionId, request.NoiDung, request.NoiDung, "KhachHang", null);
            responseDto.TinNhanCuaKhach = MapToChatDto(lichSuKhach);

            // 2. Tải lịch sử chat (cho AI)
            // === THAY ĐỔI: Chuyển sang List<object> ===
            var chatHistory = await GetChatHistoryPayloadAsync(idKhachHang, guestSessionId);

            // 3. Gửi cho AI (Chỉ 1 vòng duy nhất)
            // AiService mới sẽ tự động xử lý việc gọi tool và diễn giải
            // === LOGIC MỚI ===
            string? aiResponse = await _aiService.GetAnswerAsync(request.NoiDung, (idKhachHang > 0) ? idKhachHang : null, chatHistory);

            string phanHoiCuoiCung;
            string loaiTinNhanPhanHoi = "AI";
            ThongBaoHoTro? thongBaoHoTro = null;

            // 4. Phân tích phản hồi của AI (Đơn giản hóa)
            if (aiResponse == null)
            {
                phanHoiCuoiCung = "Hiện tại trợ lý AI đang bận. Câu hỏi của bạn đã được chuyển đến nhân viên hỗ trợ.";
                responseDto.DaChuyenNhanVien = true;
                thongBaoHoTro = await CreateThongBaoHoTro(idKhachHang, guestSessionId, request.NoiDung, "AI Service lỗi");
                await UpdateChatHistoryWithTicketId(idKhachHang, guestSessionId, thongBaoHoTro.IdThongBao);
            }
            else if (aiResponse == "[NEEDS_SUPPORT]")
            {
                phanHoiCuoiCung = "Câu hỏi của bạn đã được ghi nhận. Chúng tôi sẽ chuyển yêu cầu này đến nhân viên hỗ trợ và phản hồi bạn sớm nhất.";
                responseDto.DaChuyenNhanVien = true;
                thongBaoHoTro = await CreateThongBaoHoTro(idKhachHang, guestSessionId, request.NoiDung, "AI phát hiện cần hỗ trợ");
                await UpdateChatHistoryWithTicketId(idKhachHang, guestSessionId, thongBaoHoTro.IdThongBao);
            }
            // === XÓA BỎ BLOCK [TOOL_CALL] ===
            // else if (aiResponse.StartsWith("[TOOL_CALL:"))
            // { ... } // Logic này đã được chuyển vào AiService
            else
            {
                // Trường hợp trả lời bình thường
                // (AiService đã tự gọi tool và diễn giải kết quả nếu cần)
                phanHoiCuoiCung = aiResponse;
            }

            // 5. Lưu phản hồi cuối cùng
            var lichSuPhanHoi = await SaveChatHistoryAsync(
                idKhachHang, guestSessionId, request.NoiDung, phanHoiCuoiCung, loaiTinNhanPhanHoi,
                thongBaoHoTro?.IdThongBao // Gán IdThongBao (nếu có)
            );
            responseDto.TinNhanPhanHoi = MapToChatDto(lichSuPhanHoi);

            // 6. Trả về IdThongBao để JS biết và kích hoạt SignalR
            if (thongBaoHoTro != null)
            {
                responseDto.IdThongBaoHoTro = thongBaoHoTro.IdThongBao;
            }

            return Ok(responseDto);
        }

        // === HÀM CREATETHONGBAO ĐƯỢC NÂNG CẤP (Giữ nguyên) ===
        private async Task<ThongBaoHoTro> CreateThongBaoHoTro(int? idKhachHang, string? guestSessionId, string noiDungYeuCau, string ghiChu)
        {
            int? finalKhachHangId = (idKhachHang.HasValue && idKhachHang > 0) ? idKhachHang : null;
            var thongBaoHoTro = new ThongBaoHoTro
            {
                IdKhachHang = finalKhachHangId,
                GuestSessionId = guestSessionId,
                NoiDungYeuCau = noiDungYeuCau,
                ThoiGianTao = DateTime.Now,
                TrangThai = "Chờ xử lý",
                GhiChu = ghiChu
            };
            _context.ThongBaoHoTros.Add(thongBaoHoTro);
            await _context.SaveChangesAsync();

            // Logic tạo ThongBao chung
            string idHienThi = finalKhachHangId?.ToString() ?? $"KVL ({guestSessionId?.Substring(0, 6)}...)";
            string noiDungThongBao = $"Người dùng ({idHienThi}) cần hỗ trợ: '{noiDungYeuCau.Substring(0, Math.Min(noiDungYeuCau.Length, 100))}'";
            var thongBao = new ThongBao
            {
                NoiDung = noiDungThongBao,
                ThoiGianTao = DateTime.Now,
                LoaiThongBao = "HoTroKhachHang",
                IdLienQuan = thongBaoHoTro.IdThongBao,
                DaXem = false
            };
            _context.ThongBaos.Add(thongBao);
            await _context.SaveChangesAsync();

            // === NÂNG CẤP SIGNALR ===
            await _chatHubContext.Clients.All.SendAsync("ReloadTicketList");
            // ======================

            return thongBaoHoTro;
        }

        // === CÁC HÀM HELPER (Giữ nguyên) ===

        private async Task<ChatLichSu> SaveChatHistoryAsync(int? idKhachHang, string? guestSessionId, string hoi, string traLoi, string loaiTinNhan, int? idThongBao)
        {
            var lichSu = new ChatLichSu
            {
                IdKhachHang = (idKhachHang > 0) ? idKhachHang : null,
                GuestSessionId = guestSessionId,
                NoiDungHoi = hoi,
                NoiDungTraLoi = traLoi,
                ThoiGian = DateTime.Now,
                LoaiChat = "Web_HTTP",
                LoaiTinNhan = loaiTinNhan,
                IdThongBaoHoTro = idThongBao
            };
            _context.ChatLichSus.Add(lichSu);
            await _context.SaveChangesAsync();
            return lichSu;
        }

        // === XÓA BỎ HOÀN TOÀN HÀM ExecuteToolAsync ===
        // private async Task<string> ExecuteToolAsync(string toolCall, int? idKhachHang)
        // { ... }

        // === THAY ĐỔI: Đổi Task<List<dynamic>> sang Task<List<object>> ===
        private async Task<List<object>> GetChatHistoryPayloadAsync(int? idKhachHang, string? guestSessionId)
        {
            var query = (idKhachHang > 0)
                ? _context.ChatLichSus.Where(c => c.IdKhachHang == idKhachHang)
                : _context.ChatLichSus.Where(c => c.GuestSessionId == guestSessionId);

            var history = await query
                .OrderByDescending(c => c.ThoiGian)
                .Take(10) // Chỉ lấy 10 tin nhắn gần nhất
                .OrderBy(c => c.ThoiGian) // Sắp xếp lại
                .ToListAsync();

            // === THAY ĐỔI: Chuyển sang List<object> ===
            var payload = new List<object>();
            foreach (var msg in history)
            {
                // Lưu ý: Với AiService mới, chúng ta không cần gửi
                // các 'functionCall' hay 'functionResponse' cũ nữa.
                // AiService mới sẽ tự xử lý (hoặc không cần) lịch sử gọi tool.
                // Chúng ta chỉ cần gửi lịch sử chat 'user' và 'model' (text).
                if (msg.LoaiTinNhan == "KhachHang")
                {
                    payload.Add(new { role = "user", parts = new[] { new { text = msg.NoiDungTraLoi } } });
                }
                else if (msg.LoaiTinNhan == "AI") // Chỉ lấy các tin nhắn AI là text
                {
                    payload.Add(new { role = "model", parts = new[] { new { text = msg.NoiDungTraLoi } } });
                }
                // Bỏ qua các tin nhắn 'AI_ToolCall' cũ (nếu có)
            }
            return payload;
        }

        private ChatMessageDto MapToChatDto(ChatLichSu entity)
        {
            return new ChatMessageDto
            {
                IdChat = entity.IdChat,
                NoiDung = entity.NoiDungTraLoi,
                ThoiGian = entity.ThoiGian,
                LoaiTinNhan = entity.LoaiTinNhan ?? "AI"
            };
        }

        /// <summary>
        /// Hàm helper để cập nhật các tin nhắn AI (chưa có vé) vào ID vé mới
        /// </summary>
        private async Task UpdateChatHistoryWithTicketId(int? idKhachHang, string? guestSessionId, int newTicketId)
        {
            // Tìm tất cả tin nhắn của phiên chat này
            // 1. Cùng IdKhachHang (nếu có) HOẶC cùng GuestSessionId (nếu có)
            // 2. Và CHƯA được gán vào phiếu nào (IdThongBaoHoTro == null)
            var messagesToUpdate = _context.ChatLichSus.Where(
                c => c.IdThongBaoHoTro == null &&
                ((idKhachHang > 0 && c.IdKhachHang == idKhachHang) ||
                    (!string.IsNullOrEmpty(guestSessionId) && c.GuestSessionId == guestSessionId))
            );

            // Dùng ToListAsync để thực thi truy vấn trước khi cập nhật
            var messagesList = await messagesToUpdate.ToListAsync();

            foreach (var msg in messagesList)
            {
                msg.IdThongBaoHoTro = newTicketId;
            }

            await _context.SaveChangesAsync();
        }
    }
}