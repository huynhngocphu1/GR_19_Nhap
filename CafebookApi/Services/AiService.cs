using CafebookApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CafebookApi.Services
{
    public class AiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly AiToolService _toolService;
        private static readonly JsonSerializerOptions _jsonOptions;

        // Enum xác định loại API
        private enum AiProvider { Gemini, OpenAI, Ollama }

        static AiService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public AiService(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory, AiToolService toolService)
        {
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
            _toolService = toolService;
        }

        // ============================================================
        // 1. CẤU HÌNH & TỰ ĐỘNG PHÁT HIỆN API (DETECT PROVIDER)
        // ============================================================

        private AiProvider DetectProvider(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint)) return AiProvider.Gemini;

            // Logic nhận diện:
            if (endpoint.Contains("googleapis.com")) return AiProvider.Gemini;
            if (endpoint.Contains("localhost") || endpoint.Contains("127.0.0.1")) return AiProvider.Ollama;

            return AiProvider.OpenAI;
        }

        private async Task<(string ApiKey, string ApiEndpoint, string ModelName, AiProvider Provider)> GetAiSettingsAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<CafebookDbContext>();
                var settings = await context.CaiDats.AsNoTracking().ToListAsync();

                string apiKey = settings.FirstOrDefault(c => c.TenCaiDat == "AI_Chat_API_Key")?.GiaTri ?? "";
                string apiEndpoint = settings.FirstOrDefault(c => c.TenCaiDat == "AI_Chat_Endpoint")?.GiaTri ?? "";
                string modelName = settings.FirstOrDefault(c => c.TenCaiDat == "AI_Chat_API_model")?.GiaTri ?? "";

                var provider = DetectProvider(apiEndpoint);

                // Fallback nếu DB chưa cấu hình model
                if (string.IsNullOrEmpty(modelName))
                {
                    if (provider == AiProvider.Ollama) modelName = "qwen3:1.7b";
                    else if (provider == AiProvider.OpenAI) modelName = "gpt-3.5-turbo";
                }

                return (apiKey, apiEndpoint, modelName, provider);
            }
        }

        // ============================================================
        // 2. LUỒNG XỬ LÝ CHÍNH (MAIN FLOW)
        // ============================================================

        public async Task<string?> GetAnswerAsync(string userQuestion, int? idKhachHang, List<object> chatHistory)
        {
            var (apiKey, apiEndpoint, modelName, provider) = await GetAiSettingsAsync();

            if (string.IsNullOrEmpty(apiEndpoint))
            {
                return "Hệ thống AI chưa được cấu hình Endpoint. Vui lòng liên hệ Admin.";
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(120);

            string requestUrl = apiEndpoint;

            switch (provider)
            {
                case AiProvider.Gemini:
                    requestUrl = $"{apiEndpoint}?key={apiKey}";
                    break;
                case AiProvider.OpenAI:
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    break;
                case AiProvider.Ollama:
                    if (!string.IsNullOrEmpty(apiKey) && apiKey != "ollama")
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    break;
            }

            string systemPrompt = BuildSystemPrompt(idKhachHang);
            var toolsDefinition = GetToolDefinitions(idKhachHang);

            object payload;
            if (provider == AiProvider.Gemini)
            {
                payload = BuildGeminiPayload(systemPrompt, chatHistory, userQuestion, toolsDefinition);
            }
            else
            {
                payload = BuildOpenAIPayload(systemPrompt, chatHistory, userQuestion, toolsDefinition, modelName);
            }

            var aiResponse = await CallApiAsync(client, requestUrl, payload);
            if (aiResponse == null) return "Xin lỗi, hiện tại tôi không thể kết nối đến máy chủ AI (Lỗi kết nối).";

            if (provider == AiProvider.Gemini)
            {
                return await HandleGeminiFlow(client, requestUrl, (dynamic)payload, aiResponse.Value, idKhachHang);
            }
            else
            {
                return await HandleOpenAIFlow(client, requestUrl, (dynamic)payload, aiResponse.Value, idKhachHang, provider);
            }
        }

        // ============================================================
        // 3. XỬ LÝ LUỒNG THEO PROVIDER
        // ============================================================

        private async Task<string?> HandleGeminiFlow(HttpClient client, string url, dynamic payload, JsonElement response, int? idKhachHang)
        {
            var (text, functionCall) = ParseGeminiResponse(response);

            if (!string.IsNullOrEmpty(text) && functionCall == null) return text;

            if (functionCall != null)
            {
                var (toolResult, toolName) = await ExecuteToolCallAsync(functionCall, idKhachHang, AiProvider.Gemini);

                var contents = (List<object>)payload.contents;
                contents.Add(new { role = "model", parts = new[] { new { functionCall = functionCall } } });
                contents.Add(new
                {
                    role = "function",
                    parts = new[] { new { functionResponse = new { name = toolName, response = toolResult } } }
                });

                var finalResponse = await CallApiAsync(client, url, (object)payload);
                if (finalResponse == null) return "Có lỗi khi xử lý thông tin từ hệ thống.";

                var (finalText, _) = ParseGeminiResponse(finalResponse.Value);
                return finalText;
            }
            return null;
        }

        private async Task<string?> HandleOpenAIFlow(HttpClient client, string url, dynamic payload, JsonElement response, int? idKhachHang, AiProvider provider)
        {
            var (text, toolCallObj, toolCallId) = ParseOpenAIResponse(response);

            if (!string.IsNullOrEmpty(text) && toolCallObj == null) return text;

            if (toolCallObj != null && !string.IsNullOrEmpty(toolCallId))
            {
                var (toolResult, toolName) = await ExecuteToolCallAsync(toolCallObj, idKhachHang, provider);

                var messages = (List<object>)payload.messages;
                messages.Add(new
                {
                    role = "assistant",
                    tool_calls = new[] {
                        new {
                            id = toolCallId,
                            type = "function",
                            function = toolCallObj
                        }
                    }
                });

                messages.Add(new
                {
                    role = "tool",
                    tool_call_id = toolCallId,
                    name = toolName,
                    content = JsonSerializer.Serialize(toolResult, _jsonOptions)
                });

                var finalResponse = await CallApiAsync(client, url, (object)payload);
                if (finalResponse == null) return "Có lỗi khi xử lý thông tin từ hệ thống.";

                var (finalText, _, _) = ParseOpenAIResponse(finalResponse.Value);
                return finalText;
            }
            return null;
        }

        // ============================================================
        // 4. PAYLOAD BUILDERS
        // ============================================================

        private object BuildGeminiPayload(string systemPrompt, List<object> history, string userMsg, List<object> tools)
        {
            var contents = new List<object>();
            contents.Add(new { role = "user", parts = new[] { new { text = systemPrompt } } });
            contents.Add(new { role = "model", parts = new[] { new { text = "OK. Em đã hiểu nhiệm vụ." } } });

            if (history != null) contents.AddRange(history);
            contents.Add(new { role = "user", parts = new[] { new { text = userMsg } } });

            return new
            {
                contents = contents,
                tools = new[] { new { functionDeclarations = tools } },
                toolConfig = new { functionCallingConfig = new { mode = "AUTO" } }
            };
        }

        private object BuildOpenAIPayload(string systemPrompt, List<object> history, string userMsg, List<object> tools, string modelName)
        {
            var messages = new List<object>();
            messages.Add(new { role = "system", content = systemPrompt });

            if (history != null)
            {
                foreach (dynamic msg in history)
                {
                    try
                    {
                        string json = JsonSerializer.Serialize((object)msg);
                        var element = JsonDocument.Parse(json).RootElement;
                        string role = "user";
                        string text = "";

                        if (element.TryGetProperty("role", out var roleEl))
                            role = roleEl.GetString() == "model" ? "assistant" : "user";

                        if (element.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                            text = parts[0].GetProperty("text").GetString() ?? "";
                        else if (element.TryGetProperty("content", out var contentEl))
                            text = contentEl.GetString() ?? "";

                        if (!string.IsNullOrEmpty(text)) messages.Add(new { role = role, content = text });
                    }
                    catch { }
                }
            }
            messages.Add(new { role = "user", content = userMsg });

            var openAiTools = tools.Select(t => new { type = "function", function = t }).ToList();

            return new
            {
                model = modelName,
                messages = messages,
                tools = openAiTools,
                temperature = 0.7,
                stream = false
            };
        }

        // ============================================================
        // 5. RESPONSE PARSERS
        // ============================================================

        private (string? text, object? functionCall) ParseGeminiResponse(JsonElement aiResponse)
        {
            try
            {
                if (aiResponse.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var content = candidates[0].GetProperty("content");
                    if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var part = parts[0];
                        if (part.TryGetProperty("functionCall", out var fc)) return (null, fc);
                        if (part.TryGetProperty("text", out var txt)) return (txt.GetString(), null);
                    }
                }
            }
            catch { }
            return (null, null);
        }

        private (string? text, object? toolCallObj, string? toolId) ParseOpenAIResponse(JsonElement aiResponse)
        {
            try
            {
                if (aiResponse.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");

                    if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
                    {
                        var firstTool = toolCalls[0];
                        string id = firstTool.GetProperty("id").GetString() ?? "";
                        var function = firstTool.GetProperty("function");
                        return (null, function, id);
                    }

                    if (message.TryGetProperty("content", out var content))
                    {
                        return (content.GetString(), null, null);
                    }
                }
            }
            catch { }
            return (null, null, null);
        }

        // ============================================================
        // 6. THỰC THI TOOL (FULL LOGIC)
        // ============================================================

        private async Task<(object? toolResult, string toolName)> ExecuteToolCallAsync(object functionCall, int? idKhachHang, AiProvider provider)
        {
            var callJson = JsonSerializer.SerializeToElement(functionCall);
            string toolName = "";
            if (callJson.TryGetProperty("name", out var nameEl)) toolName = nameEl.GetString() ?? "";

            // Parse Arguments an toàn cho cả Gemini và OpenAI
            JsonElement args;
            if (provider == AiProvider.OpenAI || provider == AiProvider.Ollama)
            {
                string argsString = callJson.TryGetProperty("arguments", out var argEl) ? (argEl.GetString() ?? "{}") : "{}";
                try { args = JsonDocument.Parse(argsString).RootElement; } catch { args = JsonDocument.Parse("{}").RootElement; }
            }
            else
            {
                if (!callJson.TryGetProperty("args", out args)) args = JsonDocument.Parse("{}").RootElement;
            }

            // Hàm Helper cục bộ để lấy giá trị từ JSON
            T GetArg<T>(string key, T defaultValue)
            {
                if (args.TryGetProperty(key, out var el))
                {
                    try
                    {
                        var type = typeof(T);
                        if (type == typeof(int)) return (T)(object)el.GetInt32();
                        if (type == typeof(double)) return (T)(object)el.GetDouble();
                        if (type == typeof(string)) return (T)(object)(el.GetString() ?? "");
                    }
                    catch { }
                }
                return defaultValue;
            }

            try
            {
                switch (toolName)
                {
                    // --- NHÓM 1: CÔNG KHAI ---
                    case "GET_THONG_TIN_CHUNG":
                        return (await _toolService.GetThongTinChungAsync(), toolName);

                    case "GET_KHUYEN_MAI":
                        return (await _toolService.GetKhuyenMaiAsync(), toolName);

                    // --- NHÓM 2: F&B ---
                    case "KIEM_TRA_SAN_PHAM":
                        return (await _toolService.KiemTraSanPhamAsync(GetArg("tenSanPham", "")), toolName);

                    case "TIM_MON_THEO_LOAI":
                        return (await _toolService.TimMonTheoLoaiAsync(GetArg("loaiMon", "")), toolName);

                    case "GET_GOI_Y_SAN_PHAM":
                        return (await _toolService.GetGoiYSanPhamAsync(), toolName);

                    // --- NHÓM 3: SÁCH ---
                    case "KIEM_TRA_SACH":
                        return (await _toolService.KiemTraSachAsync(GetArg("tenSach", "")), toolName);

                    case "TIM_SACH_THEO_TAC_GIA":
                        return (await _toolService.TimSachTheoTacGiaAsync(GetArg("tenTacGia", "")), toolName);

                    case "GET_GOI_Y_SACH":
                        return (await _toolService.GetGoiYSachAsync(), toolName);

                    // --- NHÓM 4: ĐẶT BÀN ---
                    case "KIEM_TRA_BAN":
                        return (await _toolService.KiemTraBanTrongAsync(GetArg("soNguoi", 2)), toolName);

                    case "DAT_BAN_THUC_SU":
                        string timeStr = GetArg("thoiGianDat", "");
                        // Parse thời gian, nếu lỗi thì mặc định Now (Service sẽ chặn nếu < Now + 15p)
                        if (!DateTime.TryParse(timeStr, out DateTime thoiGianDat)) thoiGianDat = DateTime.Now;

                        return (await _toolService.DatBanThucSuAsync(
                            GetArg("tenBan", ""),
                            GetArg("soNguoi", 2),
                            thoiGianDat,
                            GetArg("hoTen", ""),
                            GetArg("sdt", ""),
                            GetArg("email", ""), // <-- Đã thêm trường Email
                            GetArg("ghiChu", ""),
                            idKhachHang
                        ), toolName);

                    // --- NHÓM 5: CÁ NHÂN (Cần Login) ---

                    case "GET_DIEM_TICH_LUY":
                        if (!idKhachHang.HasValue) return ("Yêu cầu đăng nhập để xem điểm.", toolName);
                        return (await _toolService.GetDiemTichLuyAsync(idKhachHang.Value), toolName);

                    case "GET_THONG_TIN_CA_NHAN":
                        if (!idKhachHang.HasValue) return ("Yêu cầu đăng nhập để xem thông tin.", toolName);
                        return (await _toolService.GetThongTinCaNhanAsync(idKhachHang.Value), toolName);
/*
                    case "GET_TONG_QUAN_TAI_KHOAN":
                        if (!idKhachHang.HasValue) return ("Yêu cầu đăng nhập để xem thông tin.", toolName);
                        return (await _toolService.GetTongQuanTaiKhoanAsync(idKhachHang.Value), toolName);
*/
                    case "GET_LICH_SU_DAT_BAN":
                        if (!idKhachHang.HasValue) return ("Yêu cầu đăng nhập để xem lịch sử.", toolName);
                        return (await _toolService.GetLichSuDatBanAsync(idKhachHang.Value), toolName);

                    case "HUY_DAT_BAN":
                        if (!idKhachHang.HasValue) return ("Yêu cầu đăng nhập để thực hiện.", toolName);
                        return (await _toolService.HuyDatBanAsync(GetArg("idPhieuDat", 0), GetArg("lyDo", ""), idKhachHang.Value), toolName);

                    case "GET_LICH_SU_THUE_SACH":
                        if (!idKhachHang.HasValue) return ("Yêu cầu đăng nhập để xem sách đang thuê.", toolName);
                        return (await _toolService.GetLichSuThueSachAsync(idKhachHang.Value), toolName);

                    case "GET_LICH_SU_DON_HANG":
                        if (!idKhachHang.HasValue) return ("Yêu cầu đăng nhập để xem đơn hàng.", toolName);
                        return (await _toolService.GetLichSuDonHangAsync(idKhachHang.Value), toolName);

                    case "THEO_DOI_DON_HANG":
                        if (!idKhachHang.HasValue) return ("Yêu cầu đăng nhập để theo dõi đơn.", toolName);
                        return (await _toolService.TheoDoiDonHangAsync(GetArg("idHoaDon", 0), idKhachHang.Value), toolName);

                    default:
                        return (new { Error = $"Tool '{toolName}' không được hỗ trợ trong hệ thống." }, toolName);
                }
            }
            catch (Exception ex)
            {
                return (new { Error = $"Lỗi hệ thống khi gọi tool: {ex.Message}" }, toolName);
            }
        }

        private async Task<JsonElement?> CallApiAsync(HttpClient client, string url, object payload)
        {
            try
            {
                var response = await client.PostAsJsonAsync(url, payload, _jsonOptions);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Error: {response.StatusCode} - {err}");
                    return null;
                }
                return await response.Content.ReadFromJsonAsync<JsonElement>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return null;
            }
        }

        // ============================================================
        // 7. SYSTEM PROMPT (KỊCH BẢN HOÀN THIỆN)
        // ============================================================

        private string BuildSystemPrompt(int? idKhachHang)
        {
            // 1. Xác định trạng thái khách hàng
            string trangThaiKhach = idKhachHang.HasValue && idKhachHang > 0
                ? $"KHÁCH HÀNG: THÀNH VIÊN (ID: {idKhachHang}).\n   -> QUYỀN HẠN: Được phép tra cứu toàn bộ lịch sử (Đặt bàn, Đơn hàng, Thuê sách) và Hủy đặt bàn."
                : "KHÁCH HÀNG: VÃNG LAI (Chưa đăng nhập).\n   -> HẠN CHẾ: KHÔNG ĐƯỢC tiết lộ thông tin cá nhân/lịch sử. Nếu khách yêu cầu chức năng quản lý -> Gợi ý đăng nhập.";

            // 2. System Prompt chi tiết (Kết hợp toàn bộ logic cũ và mới)
            return $@"
            ======================================================================
            SYSTEM PROMPT: TRỢ LÝ ẢO CAFEBOOK
            ======================================================================

            ### 1. ĐỊNH DANH & TÍNH CÁCH
            - **Bạn là:** Nhân viên Lễ tân ảo của Cafebook.
            - **Xưng hô:** ""Dạ/Vâng"" + ""Em"" (với khách) hoặc ""Mình"" (thân thiện). Gọi khách là ""Anh/Chị"" hoặc ""Bạn"".
            - **Nhiệm vụ:** Hỗ trợ tìm kiếm F&B, Sách, Đặt bàn và Quản lý tài khoản, Tư vấn món ngon, sách hay.

            ### 2. NGUYÊN TẮC CỐT LÕI (TUÂN THỦ TUYỆT ĐỐI)
            1. **DỮ LIỆU LÀ VUA:** Mọi thông tin lấy từ TOOLS.
               - Nếu Tool trả về rỗng -> Trả lời: ""Dạ, hiện tại em chưa tìm thấy thông tin này trên hệ thống.""
            2. **ĐỊNH DẠNG CHUẨN:**
               - Tiền tệ: **50.000đ**, **120.000 VNĐ**.
               - Ngày tháng: **dd/MM/yyyy HH:mm**.
            3. **QUY TẮC NÚT BẤM (BUTTON) - [LOGIC MỚI]:**
               - Khi Tool trả về danh sách (Sách/Món ăn) kèm theo trường `Actions`:
                 - **Bước 1 (Text):** Liệt kê danh sách dạng số (1. Tên món - Giá/Mô tả) trong nội dung trả lời.
                 - **Bước 2 (Button):** Duyệt qua mảng `Actions` và tạo nút bấm ở cuối câu trả lời theo cú pháp: `[BUTTON: Tên hiển thị | /duong-dan]`.
               - *Ví dụ:* `[BUTTON: Trà Đào Cam Sả | /san-pham/12]` `[BUTTON: Trà Vải | /san-pham/15]`.
               - Kết quả từ Tool hiện tại sẽ có trường `Actions` (danh sách các nút).
               - Bạn PHẢI đọc trường này và tạo nút ở cuối câu trả lời theo cú pháp: `[BUTTON: Tên hiển thị | /duong-dan]`.
               - *Ví dụ:* Tool trả về `Actions: [{{""Label"": ""Xem chi tiết"", ""Link"": ""/abc""}}]` -> Bạn viết: `[BUTTON: Xem chi tiết | /abc]`.
               - **NGOẠI LỆ:** Kết quả từ `KIEM_TRA_BAN` chỉ được liệt kê dạng văn bản (List), KHÔNG tạo nút.
               **NHÓM chức năng & TRANG:**
               - Thông tin cá nhân: `/Account/ThongTinCaNhanView`
               - Tổng quan tài khoản: `/Account/TaiKhoanTongQuanView`
               - Đổi mật khẩu: `/Account/DoiMatKhauView`
               - Lịch sử đặt bàn: `/Account/LichSuDatBanView`
               - Lịch sử thuê sách: `/Account/LichSuThueSachView`
               - Lịch sử đơn hàng (Danh sách): `/Account/LichSuDonHangView`
               - Chi tiết đơn hàng (Cần ID): `/Account/LichSuDonHangDetailView/id` 
                **NHÓM DỊCH VỤ & SẢN PHẨM:**
               - Thực đơn (Menu): `/ThucDonView`
               - Chi tiết món (Cần ID): `/san-pham/id` 
               - Thư viện sách: `/ThuVienSachView`
               - Chi tiết sách (Cần ID): `/ChiTietSachView/id` 
               - Đặt bàn (Form đặt): `/dat-ban`

               **NHÓM THÔNG TIN CHUNG:**
               - Liên hệ: `/LienHeView`
               - Chính sách/Quyền riêng tư: `/Privacy`
               - Giỏ hàng: `/GioHangView`
            ---

            ### 3. KỊCH BẢN XỬ LÝ THEO TÌNH HUỐNG (USE CASES)

            #### A. TƯ VẤN ẨM THỰC (MENU F&B)
            - **Hỏi chung chung (VD: ""Nay uống gì?""):** Gọi `GET_GOI_Y_SAN_PHAM`.
            - **Hỏi món cụ thể (VD: ""Giá bạc xỉu?""):** Gọi `KIEM_TRA_SAN_PHAM`.
            - **Hỏi theo loại (VD: ""Quán có các loại trà nào?""):** Gọi `TIM_MON_THEO_LOAI` (param: ""Trà"").
            - kèm mô tả ngắn gọn.
              - *Lưu ý:* Nếu món Hết hàng -> Gợi ý món tương tự.

            #### B. TƯ VẤN THƯ VIỆN SÁCH
            - **Tìm sách cụ thể:** Gọi `KIEM_TRA_SACH`.
              -> *Bắt buộc báo:* Vị trí kệ và Số lượng còn lại.
            - **Tìm theo tác giả (VD: ""Sách của Nguyễn Nhật Ánh""):** Gọi `TIM_SACH_THEO_TAC_GIA`.
            - **Gợi ý sách (Khách không biết đọc gì):** Gọi `GET_GOI_Y_SACH`.
            - kèm mô tả ngắn gọn.

            #### C. QUY TRÌNH ĐẶT BÀN (BẮT BUỘC ĐỦ BƯỚC)
            1. **Hỏi nhu cầu:** Số người & Giờ đến.
            2. **Kiểm tra:** Gọi `KIEM_TRA_BAN(soNguoi)`.
            3. **Báo cáo & Chọn:** Liệt kê bàn trống -> Đợi khách chốt tên bàn.
            4. **Chốt đơn:** Gọi `DAT_BAN_THUC_SU`.
               - *Thành viên:* Tự động điền tên/SĐT/Email.
               - *Vãng lai:* Hỏi Tên & SĐT & Email.
            5. **Hỏi xem có ghi chú gì không.

            #### D. QUẢN LÝ TÀI KHOẢN (CHỈ DÀNH CHO THÀNH VIÊN)
            *(Nếu khách chưa đăng nhập: Từ chối khéo và hiển thị nút [BUTTON: Đăng nhập | /Account/Login])*

            1. **Hỏi Điểm Tích Lũy:**
               - Khi khách hỏi: 'Điểm của tôi', 'Tôi có bao nhiêu điểm', 'Xem điểm'.
               - Gọi Tool: `GET_DIEM_TICH_LUY`.
               - Chỉ trả lời ngắn gọn số điểm. (Tuyệt đối KHÔNG tự bịa ra hạng thành viên Bạc/Vàng/Kim Cương).

            2. **Hỏi Thông Tin Cá Nhân:**
               - Khi khách hỏi: 'Thông tin của tôi', 'Xem profile', 'Số điện thoại của tôi'.
               - Gọi Tool: `GET_THONG_TIN_CA_NHAN`.
               - Hiển thị thông tin cơ bản đã được che mờ (Masked).

            3. **Quản lý Đặt bàn:**
               - *Xem lịch sử:* Gọi `GET_LICH_SU_DAT_BAN`.
               Tool trả về: `DatBanHomNay`, `LichSuThangNay`.
               - Nếu có `DatBanHomNay`: Nhắc khách dõng dạc về lịch hẹn hôm nay.
               - Nếu không: Hiển thị danh sách đặt bàn trong tháng này.
               - *Hủy bàn:* - B1: Gọi `GET_LICH_SU_DAT_BAN` để khách xem và lấy ID phiếu.
                 - B2: Hỏi lý do hủy.
                 - B3: Gọi `HUY_DAT_BAN(idPhieuDat, lyDo)`.

            4. **Quản lý Sách:** Gọi `GET_LICH_SU_THUE_SACH`.
                Tool trả về: `CanhBaoTreHan`, `CanhBaoSapHetHan`, `LichSuThangNay`.
                - **Ưu tiên 1:** Nếu có `CanhBaoTreHan`, PHẢI cảnh báo ngay lập tức và báo tổng tiền phạt dự kiến.
                - **Ưu tiên 2:** Nếu có `CanhBaoSapHetHan`, nhắc khách trả sách sớm.
                - **Ưu tiên 3:** Liệt kê các sách thuê trong tháng này.

            5. **Quản lý Đơn hàng F&B:**
               - *Xem lịch sử mua hàng:* Gọi `GET_LICH_SU_DON_HANG`.
               - *Theo dõi đơn vừa đặt:* Gọi `THEO_DOI_DON_HANG` (để xem đang pha chế hay đã xong).

            #### E. TIỆN ÍCH & KHUYẾN MÃI
            - **Hỏi Wifi, Giờ mở cửa, Địa chỉ:** Gọi `GET_THONG_TIN_CHUNG`.
            - **Hỏi Khuyến mãi/Ưu đãi:** Gọi `GET_KHUYEN_MAI`.

            #### F. QUY ĐỊNH ĐIỂM THƯỞNG
            - Nếu khách hỏi cách tính điểm, hãy lấy thông tin từ `GET_THONG_TIN_CHUNG` -> `ChinhSachDiem` để trả lời chính xác quy đổi.
            ---

            ### 4. QUY TẮC CHUYỂN NHÂN VIÊN HỖ TRỢ (QUAN TRỌNG)
            Nếu khách hàng rơi vào các trường hợp sau:
            1. Báo lỗi kỹ thuật (ví dụ: ""Web lỗi"", ""Không đăng nhập được"", ""App bị lag"").
            2. Phàn nàn, gay gắt, chửi bới hoặc không hài lòng với câu trả lời của AI.
            3. Yêu cầu gặp người thật (ví dụ: ""Gặp nhân viên"", ""Chat với admin"", ""Tư vấn viên đâu"").
            4. Cả khách vãng lai lẫn Khách hàng đều liên hệ được với nhân viên.
            -> Bạn CHỈ được phép trả lời duy nhất cụm từ mã lệnh: `[NEEDS_SUPPORT]`
            (Tuyệt đối không giải thích gì thêm, chỉ in đúng mã lệnh này để hệ thống chuyển kênh chat).

            ---


            ### 5. NGỮ CẢNH HIỆN TẠI
            - {trangThaiKhach}
            - Thời gian hệ thống: {DateTime.Now:dd/MM/yyyy HH:mm:ss}
            ======================================================================
            ";
        }

        // ============================================================
        // 8. TOOL DEFINITIONS (ĐẦY ĐỦ)
        // ============================================================

        private List<object> GetToolDefinitions(int? idKhachHang)
        {
            var tools = new List<object>();

            // --- NHÓM 1: CÔNG KHAI (THÔNG TIN & TIỆN ÍCH) ---
            tools.Add(new 
            { 
                name = "GET_THONG_TIN_CHUNG", 
                description = "Lấy thông tin: Giờ mở cửa, Wifi, Địa chỉ, Hotline, Quy định quán.", 
                parameters = new { type = "object", properties = new { }, required = new string[] { } } });
            tools.Add(new 
            { 
                name = "GET_KHUYEN_MAI", 
                description = "Tra cứu các chương trình khuyến mãi đang diễn ra.", 
                parameters = new { type = "object", properties = new { }, required = new string[] { } } });

            // --- NHÓM 2: F&B (ẨM THỰC) ---
            tools.Add(new
            {
                name = "KIEM_TRA_SAN_PHAM",
                description = "Tìm món ăn/đồ uống theo tên.",
                parameters = new { type = "object", properties = new { tenSanPham = new { type = "string" } }, required = new[] { "tenSanPham" } }
            });
            tools.Add(new
            {
                name = "TIM_MON_THEO_LOAI",
                description = "Tìm món theo danh mục (VD: Trà, Cà phê, Bánh ngọt, Đá xay).",
                parameters = new { type = "object", properties = new { loaiMon = new { type = "string" } }, required = new[] { "loaiMon" } }
            });
            tools.Add(new { name = "GET_GOI_Y_SAN_PHAM", description = "Gợi ý ngẫu nhiên một vài món ngon cho khách.", parameters = new { type = "object", properties = new { }, required = new string[] { } } });

            // --- NHÓM 3: SÁCH ---
            tools.Add(new
            {
                name = "KIEM_TRA_SACH",
                description = "Tìm sách theo tên sách.",
                parameters = new { type = "object", properties = new { tenSach = new { type = "string" } }, required = new[] { "tenSach" } }
            });
            tools.Add(new
            {
                name = "TIM_SACH_THEO_TAC_GIA",
                description = "Tìm sách theo tên tác giả.",
                parameters = new { type = "object", properties = new { tenTacGia = new { type = "string" } }, required = new[] { "tenTacGia" } }
            });
            tools.Add(new { name = "GET_GOI_Y_SACH", description = "Gợi ý sách hay ngẫu nhiên.", parameters = new { type = "object", properties = new { }, required = new string[] { } } });

            // --- NHÓM 4: ĐẶT BÀN (QUY TRÌNH 2 BƯỚC) ---
            tools.Add(new
            {
                name = "KIEM_TRA_BAN",
                description = "BƯỚC 1: Kiểm tra danh sách bàn trống phù hợp số người.",
                parameters = new { type = "object", properties = new { soNguoi = new { type = "integer" } }, required = new[] { "soNguoi" } }
            });
            tools.Add(new
            {
                name = "DAT_BAN_THUC_SU",
                description = "BƯỚC 2: Thực hiện đặt bàn (Chỉ gọi khi khách đã chốt bàn).",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        tenBan = new { type = "string" },
                        soNguoi = new { type = "integer" },
                        thoiGianDat = new { type = "string", description = "Format: yyyy-MM-dd HH:mm" },
                        hoTen = new { type = "string" },
                        sdt = new { type = "string" },
                        email = new { type = "string" }, // Trường mới
                        ghiChu = new { type = "string" }
                    },
                    required = new[] { "tenBan", "soNguoi", "thoiGianDat", "hoTen", "sdt" }
                }
            });

            // --- NHÓM 5: CÁ NHÂN (CHỈ KHI ĐÃ ĐĂNG NHẬP) ---
            if (idKhachHang.HasValue && idKhachHang > 0)
            {
                tools.Add(new
                {
                    name = "GET_DIEM_TICH_LUY",
                    description = "Chỉ lấy thông tin về điểm tích lũy của khách hàng.",
                    parameters = new { type = "object", properties = new { }, required = new string[] { } }
                });

                tools.Add(new
                {
                    name = "GET_THONG_TIN_CA_NHAN",
                    description = "Lấy thông tin profile: Họ tên, Email, SĐT, Ngày tham gia (Không bao gồm điểm).",
                    parameters = new { type = "object", properties = new { }, required = new string[] { } }
                });
                tools.Add(new { name = "GET_TONG_QUAN_TAI_KHOAN", description = "Xem điểm tích lũy.", parameters = new { type = "object", properties = new { }, required = new string[] { } } });
                tools.Add(new
                {
                    name = "GET_TONG_QUAN_TAI_KHOAN",
                    description = "Tra cứu thông tin cá nhân, số điểm tích lũy, số điện thoại, email của khách hàng.",
                    parameters = new { type = "object", properties = new { }, required = new string[] { } }
                });
                tools.Add(new 
                { 
                    name = "GET_LICH_SU_DAT_BAN", 
                    description = "Xem lịch sử đặt bàn (Bàn hôm nay, bàn cũ).", 
                    parameters = new { type = "object", properties = new { }, required = new string[] { } } });
                tools.Add(new
                {
                    name = "HUY_DAT_BAN",
                    description = "Hủy phiếu đặt bàn (Cần ID phiếu).",
                    parameters = new { type = "object", properties = new { idPhieuDat = new { type = "integer" }, lyDo = new { type = "string" } }, required = new[] { "idPhieuDat" } }
                });
                tools.Add(new 
                { 
                    name = "GET_LICH_SU_THUE_SACH", 
                    description = "Xem sách đang thuê, trễ hạn, tiền phạt.", 
                    parameters = new { type = "object", properties = new { }, required = new string[] { } } });
                tools.Add(new 
                { name = "GET_LICH_SU_DON_HANG", 
                    description = "Xem lịch sử mua F&B.", 
                    parameters = new { type = "object", properties = new { }, required = new string[] { } } });
                tools.Add(new
                {
                    name = "THEO_DOI_DON_HANG",
                    description = "Xem trạng thái chi tiết đơn hàng cụ thể (Pha chế/Giao hàng).",
                    parameters = new { type = "object", properties = new { idHoaDon = new { type = "integer" } }, required = new[] { "idHoaDon" } }
                });
            }

            return tools;
        }
    }
}