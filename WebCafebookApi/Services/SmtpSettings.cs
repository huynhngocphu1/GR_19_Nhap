// Đảm bảo file này nằm trong: WebCafebookApi/Services/SmtpSettings.cs
namespace WebCafebookApi.Services
{
    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
        public string FromName { get; set; } = string.Empty;
    }
}