USE [CAFEBOOKDB_v2]
GO

PRINT N'Bắt đầu khôi phục TOÀN BỘ Bảng [CaiDat]...';

-- 1. Xóa toàn bộ dữ liệu Cài Đặt cũ để chèn lại
DELETE FROM [dbo].[CaiDat];
PRINT N'Đã xóa dữ liệu cũ.';

-- 2. Chèn 20 dòng cài đặt CHUNG, SÁCH, AI, ĐIỂM TÍCH LŨY (Từ file của bạn)
INSERT INTO [dbo].[CaiDat] ([tenCaiDat], [giaTri], [moTa]) VALUES
(N'AI_Chat_API_Key', N'sk-xxxxxxxxxxxxxxxxxxxx', N'API Key cho dịch vụ (OpenAI, Gemini...)'),
(N'AI_Chat_Endpoint', N'https://api.openai.com/v1/chat/completions', N'Endpoint của dịch vụ AI Chat'),
(N'DiaChi', N'08 Hà Văn Tín, P. Hòa Khánh Nam, Q. Liên Chiểu, TP. Đà Nẵng', N'Địa chỉ in trên hóa đơn'),
(N'DiemTichLuy_DoiVND', N'1000', N'1 điểm tích lũy bằng ... VND trừ vào hóa đơn'),
(N'DiemTichLuy_NhanVND', N'10000', N'Mỗi ... VND trong hóa đơn được 1 điểm'),
(N'GioiThieu', N'Cafebook là không gian lý tưởng, kết hợp giữa niềm đam mê cà phê và tình yêu sách. Chúng tôi mang đến những hạt cà phê chất lượng cùng hàng ngàn đầu sách chọn lọc, tạo nên một ốc đảo bình yên cho tâm hồn bạn. test', N'Giới thiệu được hiển thị ở trên web'),
(N'LienHe_Email', N'cafebook.hotro@gmail.com', N'Gmail của quán'),
(N'LienHe_Facebook', N'https://www.facebook.com/lamtoan24/', N'Link Facebook quán (Đã sửa lỗi typo Facbook)'),
(N'LienHe_GioMoCua', N'Thứ 2 - CN: 06:00 - 23:00', N'Giờ mở cửa quán'),
(N'LienHe_GoogleMapsEmbed', N'https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3919.106598502801!2d106.7010418153489!3d10.80311546168051!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x317528a459b2184f%3A0x805d52140130f4d3!2zVHLGsOG7nW5nIMSQ4bqhaSBo4buNYyBIw7luZyBCw6BuZw!5e0!3m2!1svi!2s!4v1678888888888!5m2!1svi!2s', N'Link GoogleMapsEmbed Quán'),
(N'LienHe_Instagram', N'https://instagram.com/lamtoan24', N'Link Instagram quán'),
(N'LienHe_Website', N'https://cafebook.vn', N'Website quán'),
(N'LienHe_X', N'https://x.com/', N'Link X quán'),
(N'LienHe_Youtube', N'https://www.youtube.com/@Shu.otaku.t', N'Link Youtube quán'),
(N'LienHe_Zalo', N'https://id.zalo.me/account?continue=https%3A%2F%2Fchat.zalo.me%2F', N'Zalo Quán'),
(N'Sach_PhiThue', N'15000', N'Phí dịch vụ thuê sách được trừ sau khi trả sách'),
(N'Sach_PhiTraTreMoiNgay', N'5000', N'Số tiền (VND) phạt nếu khách trả sách trễ 1 ngày'),
(N'SoDienThoai', N'0376512695', N'Số Điện Thoại Liên Hệ'),
(N'TenQuan', N'Cafe Sách Bookshuheheee', N'Tên quán hiển thị trên hóa đơn, trang web'),
(N'Wifi_MatKhau', N'Shu.0311', N'Mật khẩu Wifi cho khách');
PRINT N'Đã chèn 20 dòng cài đặt chung/web/sách.';

-- 3. Chèn 7 dòng cài đặt Nhân sự (HR) (Từ Module 7)
INSERT INTO [dbo].[CaiDat] (tenCaiDat, giaTri, moTa) VALUES
(N'HR_GioLamChuan', '8', N'Số giờ làm việc chuẩn mỗi ngày (ví dụ: 8 tiếng)'),
(N'HR_HeSoOT', '1.5', N'Hệ số lương khi làm tăng ca (Overtime) (ví dụ: 1.5)'),
(N'HR_PhatDiTre_Phut', '5', N'Số phút cho phép đi trễ. Vượt quá ngưỡng này bắt đầu tính phạt.'),
(N'HR_PhatDiTre_HeSo', '1.0', N'Hệ số phạt khi đi trễ (ví dụ: 1.0 = trễ 1 giờ phạt 1 giờ lương)'),
(N'HR_ChuyenCan_SoNgay', '26', N'Số ngày công yêu cầu để đạt thưởng chuyên cần.'),
(N'HR_ChuyenCan_TienThuong', '500000', N'Số tiền thưởng (VND) khi đạt chuyên cần.'),
(N'HR_PhepNam_MacDinh', '12', N'Số ngày nghỉ phép có lương mặc định cho nhân viên mỗi năm.');
PRINT N'Đã chèn 7 dòng cài đặt nhân sự.';

PRINT N'================================================'
PRINT N'Khôi phục Bảng [CaiDat] thành công. Đã chèn tổng cộng 27 dòng.';
GO