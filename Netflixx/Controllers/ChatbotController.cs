using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Netflixx.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly DBContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private List<FilmsModel>? _filmsCache;

        public ChatbotController(DBContext context, IHttpClientFactory clientFactory, IConfiguration config)
        {
            _context = context;
            _httpClient = clientFactory.CreateClient();
            _apiKey = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("Missing OpenAI API key in configuration.");
        }

        // Lấy danh sách phim từ DB (cache trong controller)
        public async Task<List<FilmsModel>> GetFilmsAsync()
        {
            if (_filmsCache == null)
            {
                _filmsCache =  await _context.Films
                            
                            .OrderByDescending(f => f.ReleaseDate)
                            .Take(50)
                            .ToListAsync();
            }
            return _filmsCache;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Khởi động chat (gửi dữ liệu phim + mô tả 1 lần)
        [HttpPost]
        public async Task<IActionResult> StartChat()
        {
            var staticInfo = @"
                Netflixx là một nền tảng xem phim trực tuyến hiện đại, nơi người dùng có thể thưởng thức hàng ngàn bộ phim lẻ chất lượng cao với nội dung phong phú và được cập nhật liên tục. 
                Trang web đặc biệt chú trọng đến trải nghiệm người dùng bằng cách cung cấp kho phim mới nhất, đa dạng thể loại từ hành động, kinh dị, tình cảm, đến hoạt hình và khoa học viễn tưởng.

                Người dùng có thể mua quyền truy cập phim một cách linh hoạt thông qua hệ thống coin – đơn vị tiền ảo nội bộ của Netflixx. 
                Mỗi bộ phim có giá từ 20.000 đến 100.000 coin (tương đương 20.000 – 100.000 VNĐ, 1 coin = 1 VNĐ). Ngoài ra, trang web thường xuyên có các chương trình ưu đãi đặc biệt dành cho người dùng thân thiết.

                Việc nạp coin được thực hiện nhanh chóng, an toàn và tiện lợi thông qua cổng thanh toán trực tuyến hiện đại. Netflixx hỗ trợ  phương thức thanh toán VNPay

                Giao diện thân thiện, tốc độ tải nhanh

                Mọi thắc mắc, người dùng có thể liên hệ với đội ngũ hỗ trợ của Netflixx thông qua phần nhắn tin trực tiếp với bộ phận hỗ trợ ở phần chat hoặc qua email: figureqyn@gmail.com.
                ";

            var films = await GetFilmsAsync();

            var filmText = string.Join("\n\n", films.Select(f =>
                 $"🎬 **{f.Title}** ({f.ReleaseDate?.Year ?? 0})\n" +
                 $"- Thể loại: {f.Genre}\n" +
                 $"- Đạo diễn: {f.Director}\n" +
                 $"- Diễn viên: {f.Actors}\n" +
                 $"- Trạng thái: {f.GetStatusText()}\n" +
                 $"- Độ tuổi: {f.GetRatingText()}\n" +
                 $"- Tóm tắt: {f.Description}"
             ));


            var context = $"{staticInfo}\n\nDanh sách phim:\n{filmText}\n\n." +
                $"Bạn là trợ lý ảo của trang web xem phim trực tuyến Netflixx. " +
                $"Vai trò của bạn là trả lời các câu hỏi liên quan đến trang web, " +
                $"dịch vụ, cách sử dụng, giá phim, cách thanh toán, và các bộ phim" +
                $" đang có mặt trên hệ thống. \r\nHãy ưu tiên sử dụng thông tin được" +
                $" cung cấp trong phần mô tả và danh sách phim để trả lời câu hỏi người dùng" +
                $". Nếu câu hỏi vượt ngoài phạm vi này, bạn có thể trả lời bằng thông" +
                $" tin phổ quát hoặc từ nguồn đáng tin cậy khác.\r\n";

            HttpContext.Session.SetString("chat_context", context);
            HttpContext.Session.SetString("chat_history", "[]");

            return Json(new { status = "ok" });
        }
        
        [HttpPost]

        public async Task<IActionResult> Ask([FromBody] AskRequest req )
        {
            string userInput = req.userInput?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userInput))
            {
                return Json(new { response = "AI: Vui lòng nhập nội dung câu hỏi trước khi gửi!" });
            }

            try
            {
                var context = HttpContext.Session.GetString("chat_context") ?? "";
                var historyJson = HttpContext.Session.GetString("chat_history") ?? "[]";
                var history = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(historyJson)!;

                var messages = new List<Dictionary<string, string>> {
            new() { ["role"] = "system", ["content"] = context }
        };
                messages.AddRange(history);
                messages.Add(new() { ["role"] = "user", ["content"] = userInput }); // <- Chỉ thêm nếu không null

                var body = new
                {
                    model = "gpt-4o",
                    messages = messages,
                    temperature = 0.7
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("OpenAI API ERROR:");
                    Console.WriteLine(responseContent);
                    return Json(new { response = "AI: Xin lỗi, hệ thống đang gặp sự cố khi xử lý. Vui lòng thử lại sau!" });
                }

                dynamic result = JsonConvert.DeserializeObject(responseContent)!;
                string gptReply = result.choices[0].message.content;

                history.Add(new() { ["role"] = "user", ["content"] = userInput });
                history.Add(new() { ["role"] = "assistant", ["content"] = gptReply });

                HttpContext.Session.SetString("chat_history", JsonConvert.SerializeObject(history));

                return Json(new { response = gptReply });
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex.Message);
                return Json(new { response = "AI: Xin lỗi, hệ thống gặp lỗi nghiêm trọng. Vui lòng thử lại!" });
            }
        }

        [HttpPost]
        public IActionResult CloseChat()
        {
            HttpContext.Session.Remove("chat_context");
            HttpContext.Session.Remove("chat_history");
            _filmsCache = null;
            return Json(new { status = "closed" });
        }


        [HttpGet] // Tạo một endpoint mới tại /Chatbot/history
        public IActionResult GetHistory()
        {
            // Lấy chuỗi JSON lịch sử từ Session
            var historyJson = HttpContext.Session.GetString("chat_history") ?? "[]";

            // Deserialize chuỗi JSON thành một đối tượng mà client có thể đọc được
            var history = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(historyJson);

            // Trả về lịch sử dưới dạng JSON
            return Json(history);
        }
    }
}
