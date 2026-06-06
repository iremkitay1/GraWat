using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;

namespace GraWat.Controllers
{
    public class ChatController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ChatController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { success = false, reply = "Lütfen geçerli bir mesaj yazın." });
            }

            var apiKey = _configuration["Gemini:ApiKey"];
            
            // Eğer yapılandırmada yoksa, ortam değişkenine de bakalım
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            }

            // Demo / Fallback Modu (API Key yoksa)
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("YOUR_GEMINI_API_KEY_HERE", StringComparison.OrdinalIgnoreCase))
            {
                var mockReply = GetMockResponse(request.Message);
                return Json(new { success = true, reply = mockReply, isDemo = true });
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = request.Message }
                            }
                        }
                    },
                    systemInstruction = new
                    {
                        parts = new[]
                        {
                            new { text = "Sen GraWat kozmetik mağazasının güzellik asistanısın. Kısa, kibar, profesyonel ve Türkçe cevaplar ver. Kullanıcılara cilt bakımı, makyaj ve parfümler konusunda rehberlik et. Cevaplarında emojiler kullan." }
                        }
                    }
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody, jsonOptions), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    // API hatası durumunda da uygulamayı patlatmamak için fallback veriyoruz
                    var mockReply = GetMockResponse(request.Message) + "\n\n*(Not: Gemini API isteği başarısız oldu, demo modunda yanıt verildi)*";
                    return Json(new { success = true, reply = mockReply, isDemo = true, error = errorContent });
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                
                string replyText = "";
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && 
                    candidates.GetArrayLength() > 0)
                {
                    var contentElement = candidates[0].GetProperty("content");
                    if (contentElement.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        replyText = parts[0].GetProperty("text").GetString() ?? "";
                    }
                }

                if (string.IsNullOrWhiteSpace(replyText))
                {
                    replyText = "Üzgünüm, şu an yanıt oluşturamadım. Lütfen tekrar dener misiniz?";
                }

                return Json(new { success = true, reply = replyText.Trim() });
            }
            catch (Exception ex)
            {
                var mockReply = GetMockResponse(request.Message) + $"\n\n*(Not: Bağlantı hatası oluştu, demo modunda yanıt verildi: {ex.Message})*";
                return Json(new { success = true, reply = mockReply, isDemo = true });
            }
        }

        private string GetMockResponse(string userMessage)
        {
            var msg = userMessage.ToLower(new System.Globalization.CultureInfo("tr-TR"));

            if (msg.Contains("merhaba") || msg.Contains("selam") || msg.Contains("hey"))
            {
                return "Merhaba! GraWat Kozmetik Güzellik Asistanı olarak size yardımcı olmaktan mutluluk duyarım. 🌸 Bugün cildiniz veya makyajınız için ne tür tavsiyeler istersiniz?";
            }
            if (msg.Contains("cilt") || msg.Contains("nemlendirici") || msg.Contains("krem") || msg.Contains("leke") || msg.Contains("kuru") || msg.Contains("yağlı"))
            {
                return "Sağlıklı bir cilt için temizleme, tonikleme ve nemlendirme adımlarını içeren bir rutin uygulamalısınız. Cilt tipinize uygun ürünleri sitemizin 'Cilt Bakımı' kategorisinde bulabilirsiniz! 🧴 Güneş kremini yaz-kış sürmeyi unutmayın! ☀️";
            }
            if (msg.Contains("makyaj") || msg.Contains("ruj") || msg.Contains("fondöten") || msg.Contains("far") || msg.Contains("maskara"))
            {
                return "GraWat makyaj ürünleriyle güzelliğinizi ön plana çıkarın! Doğal bir görünüm için hafif yapılı fondötenler ve şık ruj renklerimize 'Makyaj' kategorimizden ulaşabilirsiniz. 💄 Hangi makyaj stilini tercih ediyorsunuz?";
            }
            if (msg.Contains("parfüm") || msg.Contains("koku") || msg.Contains("deodorant"))
            {
                return "Kokunuz imzanızdır! Kalıcı ve tazeleyici notalara sahip kadın ve erkek parfümlerimizi 'Parfüm / Deodorant' kategorimizde keşfedebilirsiniz. 🌸 Favori koku aileniz nedir (çiçeksi, odunsu, baharatlı)?";
            }
            if (msg.Contains("sipariş") || msg.Contains("kargo") || msg.Contains("teslim") || msg.Contains("aldım"))
            {
                return "GraWat siparişleriniz büyük bir özenle hazırlanıp en kısa sürede kargoya teslim edilmektedir. Sipariş durumunuzu kontrol etmek için menüden 'Siparişlerim' sayfasına gidebilirsiniz! 📦";
            }
            if (msg.Contains("teşekkür") || msg.Contains("sağ ol") || msg.Contains("teşekkürler"))
            {
                return "Rica ederim, ne demek! GraWat ailesi olarak her zaman yanınızdayız. Kendinize çok iyi bakın, ışıldamaya devam edin! ✨💖";
            }

            return "GraWat Güzellik Asistanı olarak buradayım! 🌸 Size cilt bakımı rutinleri, makyaj tüyoları veya parfümlerimiz hakkında nasıl yardımcı olabilirim? Merak ettiğiniz kozmetik konusunu yazabilirsiniz.";
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
    }
}
