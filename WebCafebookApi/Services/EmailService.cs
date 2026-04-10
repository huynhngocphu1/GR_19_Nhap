// Đảm bảo file này nằm trong: WebCafebookApi/Services/EmailService.cs
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System;

namespace WebCafebookApi.Services
{
    public class EmailService
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
                {
                    client.EnableSsl = _smtpSettings.EnableSsl;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);

                    var mailMessage = new MailMessage
                    {
                        // Lỗi CS1061 xảy ra ở đây nếu _smtpSettings.Username rỗng
                        From = new MailAddress(_smtpSettings.Username, _smtpSettings.FromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email to {toEmail}: {ex}");
                throw new ApplicationException($"Không thể gửi email: {ex.Message}", ex);
            }
        }

        // Hàm tiện ích để gửi mã xác nhận
        public Task SendVerificationCodeAsync(string toEmail, string code)
        {
            string subject = "Cafebook - Mã Xác Nhận Đặt Lại Mật Khẩu";
            string body = $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                <p>Xin chào,</p>
                <p>Bạn nhận được email này vì đã yêu cầu đặt lại mật khẩu cho tài khoản Cafebook của bạn.</p>
                <p>Mã xác nhận của bạn là: <strong style='font-size: 1.5em; color: #D27D2D;'>{code}</strong></p>
                <p>Mã này sẽ hết hạn sau 5 phút.</p>
                <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                <p>Trân trọng,<br/>Đội ngũ Cafebook</p>
                </div>";
            return SendEmailAsync(toEmail, subject, body);
        }
    }
}