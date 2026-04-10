// Tập tin: WebCafebookApi/Program.cs
// (Nội dung đầy đủ của file Program.cs dựa trên context bạn cung cấp)

using Microsoft.AspNetCore.Authentication.Cookies;
using WebCafebookApi.Services;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// SỬA CẤU HÌNH HTTPCLIENT
builder.Services.AddHttpClient("ApiClient", (serviceProvider, client) =>
{
    client.BaseAddress = new Uri("http://localhost:5166");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
    if (httpContextAccessor != null)
    {
        // SỬA: Đọc JWT Token từ Session (do DangNhapView.cshtml.cs lưu vào)
        var token = httpContextAccessor.HttpContext?.Session.GetString("JwtToken");

        if (!string.IsNullOrEmpty(token))
        {
            // Gắn JWT Token thật vào Header
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
});
// (Kết thúc sửa)

builder.Services.AddHttpClient();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AuthToken"; // Cookie này chỉ dùng cho Frontend
        options.LoginPath = "/account/DangNhapView";
        options.AccessDeniedPath = "/AccessDenied";
        options.LogoutPath = "/Account/DangXuat";
    });

builder.Services.AddRazorPages();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<EmailService>();
builder.Services.AddMemoryCache();

var app = builder.Build();

// ... (Pipeline giữ nguyên) ...
//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Đảm bảo đúng thứ tự
app.UseSession(); // Session phải được gọi trước
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.Run();