// Tập tin: CafebookApi/Program.cs
using CafebookApi.Data;
using CafebookApi.Services;
using CafebookApi.Hubs; // <-- Đảm bảo có
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Kết nối SQL Server
var connectionString = builder.Configuration.GetConnectionString("CafeBookConnectionString");
builder.Services.AddDbContext<CafebookDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });

    // Policy cho SignalR (quan trọng)
    options.AddPolicy("SignalRPolicy",
        policy =>
        {
            policy.WithOrigins("http://localhost:5156") // <-- THAY BẰNG URL WEB CỦA BẠN (nếu khác)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // <-- Bắt buộc cho SignalR
        });
});

// 3. Đăng ký dịch vụ
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();

// Đăng ký các dịch vụ của bạn (dưới dạng Scoped là đúng)
builder.Services.AddScoped<AiService>();
builder.Services.AddScoped<AiToolService>();

// === THÊM DỊCH VỤ SIGNALR ===
builder.Services.AddSignalR();

builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);
});

// 4. Cấu hình xác thực JWT (Giữ nguyên)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"]!,
        ValidAudience = builder.Configuration["Jwt:Audience"]! ?? builder.Configuration["Jwt:Issuer"]!,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

var app = builder.Build();

// 5. Cấu hình Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();

// Dùng cả hai policy
app.UseCors("AllowAll");
app.UseCors("SignalRPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// === MAP (ÁNH XẠ) CHATHUB ===
//app.MapHub<ChatHub>("/chatHub");
app.MapHub<ChatHub>("/chathub").RequireCors("SignalRPolicy");

app.Run();