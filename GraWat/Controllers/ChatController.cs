using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using GraWat.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace GraWat.Controllers
{
    public class ChatController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public ChatController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ApplicationDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _context = context;
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

            // Sohbet geçmişini Session'dan yükle
            var sessionKey = "BeautyAssistantHistory";
            var historyJson = HttpContext.Session.GetString(sessionKey);
            List<GeminiMessage> history = string.IsNullOrEmpty(historyJson)
                ? new List<GeminiMessage>()
                : JsonSerializer.Deserialize<List<GeminiMessage>>(historyJson) ?? new List<GeminiMessage>();

            // Yeni kullanıcı mesajını geçmişe ekle
            history.Add(new GeminiMessage
            {
                role = "user",
                parts = new List<GeminiPart> { new GeminiPart { text = request.Message } }
            });

            // Token limitini aşmamak için geçmişi son 20 mesajla (10 konuşma turu) sınırla
            if (history.Count > 20)
            {
                history = history.Skip(history.Count - 20).ToList();
            }

            // 1. Akıllı Bağlam Filtreleme (LINQ ile sadece ilgili kategorideki ürünleri çekiyoruz)
            var userMsgLower = request.Message.ToLower(new System.Globalization.CultureInfo("tr-TR"));
            var matchedCategories = new List<string>();

            if (userMsgLower.Contains("saç") || userMsgLower.Contains("şampuan") || userMsgLower.Contains("kepek") || userMsgLower.Contains("dökülme") || userMsgLower.Contains("saç kremi") || userMsgLower.Contains("saç maskesi"))
            {
                matchedCategories.Add("Saç Bakım");
            }
            if (userMsgLower.Contains("cilt") || userMsgLower.Contains("yüz") || userMsgLower.Contains("nemlendirici") || userMsgLower.Contains("krem") || userMsgLower.Contains("leke") || userMsgLower.Contains("kuru") || userMsgLower.Contains("yağlı") || userMsgLower.Contains("serum") || userMsgLower.Contains("tonik") || userMsgLower.Contains("akne") || userMsgLower.Contains("yanık") || userMsgLower.Contains("hassasiyet"))
            {
                matchedCategories.Add("Cilt Bakım");
            }
            if (userMsgLower.Contains("güneş") || userMsgLower.Contains("spf") || userMsgLower.Contains("koruyucu") || userMsgLower.Contains("bronz"))
            {
                matchedCategories.Add("Güneş Ürünleri");
            }
            if (userMsgLower.Contains("makyaj") || userMsgLower.Contains("ruj") || userMsgLower.Contains("fondöten") || userMsgLower.Contains("far") || userMsgLower.Contains("maskara") || userMsgLower.Contains("rimel") || userMsgLower.Contains("allık") || userMsgLower.Contains("kapatıcı") || userMsgLower.Contains("eyeliner"))
            {
                matchedCategories.Add("Makyaj");
            }
            if (userMsgLower.Contains("parfüm") || userMsgLower.Contains("koku") || userMsgLower.Contains("deodorant") || userMsgLower.Contains("esans"))
            {
                matchedCategories.Add("Parfüm/Deodorant");
            }

            // 1. Akıllı Bağlam Filtreleme ve Dinamik RAG (Anlık ürün listesi)
            // Stokta olan ürünleri ID'ye göre azalan sırada çekerek sisteme en son eklenenlerin en üstte yer almasını sağlıyoruz.
            // Kullanıcının mesajındaki anahtar kelimelere göre akıllı kategori filtrelemesi uygularız.
            // API Token limitini aşmamak için son 20 ürün ile sınırlandırılmıştır.
            var productsQuery = _context.Urunler.Where(p => p.StokAdedi > 0);
            if (matchedCategories.Any())
            {
                productsQuery = productsQuery.Where(u => matchedCategories.Contains(u.Kategori));
            }

            var products = await productsQuery
                .OrderByDescending(p => p.Id)
                .Take(20)
                .Select(u => new { u.Ad, u.Kategori, u.Fiyat, u.StokAdedi })
                .ToListAsync();

            // 2. Kategori Verisini Dahil Et (RAG Context hazırlığı)
            var productContextBuilder = new StringBuilder();
            productContextBuilder.AppendLine("GraWat Mağazasındaki Mevcut Ürünler (Filtrelenmiş Katalog):");
            if (products.Any())
            {
                foreach (var p in products)
                {
                    var stokDurumu = p.StokAdedi > 0 ? $"Stokta Var ({p.StokAdedi} adet)" : "Tükendi";
                    productContextBuilder.AppendLine($"- Ürün Adı: {p.Ad} | Kategori: {p.Kategori} | Fiyat: {p.Fiyat:F2} TL | Stok Durumu: {stokDurumu}");
                }
            }
            else
            {
                productContextBuilder.AppendLine("(Aradığınız kategoride şu an stoklarımızda ürün bulunmamaktadır.)");
            }
            var productContext = productContextBuilder.ToString();

            // Demo / Fallback Modu (API Key yoksa veya geçersizse)
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("YOUR_GEMINI_API_KEY_HERE", StringComparison.OrdinalIgnoreCase))
            {
                var mockReply = GetMockResponse(request.Message, products.Select(u => u.Ad).ToList(), matchedCategories);
                
                history.Add(new GeminiMessage
                {
                    role = "model",
                    parts = new List<GeminiPart> { new GeminiPart { text = mockReply } }
                });
                HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(history));

                return Json(new { success = true, reply = mockReply, isDemo = true });
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

                // 3. Adımlı Kısıtlayıcı Sistem Komutu (System Persona) Tanımlaması
                var systemPrompt = "Sen GraWat premium kozmetik ve kişisel bakım mağazasının baş uzmanı ve bilimsel güzellik danışmanısın. Kullanıcılarla konuşurken KESİNLİKLE şu sırayı (Adımları) takip etmelisin:\n\n" +
                                   "Adım 1: Kullanıcının sorununa (Örn: güneş yanıklarını önleme, cilt kuruluğu vb.) bilimsel, tatlı, samimi ve profesyonel bir dille tavsiyelerde bulun. Ürünün neden işe yaradığını bilimsel terimlerle (seramidler, nem tutucular vb.) açıkla. Tıbbi teşhis veya tedavi (ilaç, yanık tedavisi vb.) önerme, sadece kozmetik ve koruyucu öneriler ver. Emojileri (✨, 💧, 🌿, 🧴) ölçülü kullan.\n" +
                                   "Adım 2: Sana aşağıda bağlam (context) olarak iletilen GraWat ürün listesinde bu tavsiyeye uygun ürün varsa, bunları şık bir şekilde maddeler (bullet points) halinde listele.\n" +
                                   "Adım 3: Kategori Uyuşmazlığı Kuralı (Strict Rejection): Sana bağlam (context) olarak verilen ürün listesini incele. Eğer kullanıcının sorduğu soruyla (Örn: Cilt bakımı, yüze sürülecek ürünler) bağlamdaki ürünlerin kategorisi (Örn: Ağız bakımı, hijyenik ped, duş jeli) mantıksal olarak uyuşmuyorsa, bu ürünleri KESİNLİKLE önerme! Kullanıcıya cilt bakımı tavsiyelerini ver ve \"Şu an stoklarımızda doğrudan yüz/cilt bakımına uygun bir ürün bulunmuyor\" diyerek konuyu kapat. Alakasız kategorileri birbirine bağlamaya çalışma.\n\n" +
                                   "Önerilerinde sadece aşağıdaki listede yer alan gerçek ve ilgili kategorideki ürünleri kullan, hayali ürün önerme. Saç sorana saç ürünü önermeyi, makyaj sorana makyaj önermeyi unutma.\n\n" +
                                   productContext;

                var requestBody = new
                {
                    contents = history,
                    systemInstruction = new
                    {
                        parts = new[]
                        {
                            new { text = systemPrompt }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topP = 0.9
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
                    var mockReply = GetMockResponse(request.Message, products.Select(u => u.Ad).ToList(), matchedCategories) + "\n\n*(Not: Gemini API isteği başarısız oldu, demo modunda yanıt verildi)*";
                    
                    history.Add(new GeminiMessage
                    {
                        role = "model",
                        parts = new List<GeminiPart> { new GeminiPart { text = mockReply } }
                    });
                    HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(history));

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
                    replyText = "Üzgünüm, şu an uygun bir yanıt oluşturamadım. Lütfen tekrar dener misiniz?";
                }

                // Başarılı yanıtı geçmişe kaydet ve Session'ı güncelle
                history.Add(new GeminiMessage
                {
                    role = "model",
                    parts = new List<GeminiPart> { new GeminiPart { text = replyText.Trim() } }
                });
                HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(history));

                return Json(new { success = true, reply = replyText.Trim() });
            }
            catch (Exception ex)
            {
                var mockReply = GetMockResponse(request.Message, products.Select(u => u.Ad).ToList(), matchedCategories) + $"\n\n*(Not: Bağlantı hatası oluştu, demo modunda yanıt verildi: {ex.Message})*";
                
                history.Add(new GeminiMessage
                {
                    role = "model",
                    parts = new List<GeminiPart> { new GeminiPart { text = mockReply } }
                });
                HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(history));

                return Json(new { success = true, reply = mockReply, isDemo = true });
            }
        }

        private string GetMockResponse(string userMessage, List<string> filteredProductNames, List<string> matchedCategories)
        {
            var msg = userMessage.ToLower(new System.Globalization.CultureInfo("tr-TR"));
            var urunListesiText = filteredProductNames.Any() ? string.Join(", ", filteredProductNames.Take(3)) : "Ürün bulunmuyor";

            // Eğer aranan kategoriye ait ürün yoksa (Stokta ürün kalmadıysa) genel tavsiye ver ve stoku bildir
            if (matchedCategories.Any() && !filteredProductNames.Any())
            {
                var tavsiye = "";
                if (matchedCategories.Contains("Saç Bakım"))
                {
                    tavsiye = "Saç bakımınız için saç tellerini kurutmamak adına çok sıcak suyla yıkamaktan kaçınmalı, banyo sonrası saçınızı nemliyken taramalı ve kırıkları önlemek adına uçlara doğal yağlar uygulamalısınız. 🌿";
                }
                else if (matchedCategories.Contains("Cilt Bakım") || matchedCategories.Contains("Güneş Ürünleri"))
                {
                    tavsiye = "Cilt hassasiyetlerini ve kuruluklarını önlemek için günde en az 2 litre su içmeli, güneşe çıkmadan önce en az SPF 30 koruyucu sürmeli ve tahriş edici alkollü ürünler yerine nemlendirici temizleyiciler kullanmalısınız. 💧";
                    return $"{tavsiye}\n\nŞu an stoklarımızda doğrudan yüz/cilt bakımına uygun bir ürün bulunmuyor.";
                }
                else if (matchedCategories.Contains("Makyaj"))
                {
                    tavsiye = "Doğal bir ten görünümü elde etmek için cildinizi makyaj öncesinde iyice nemlendirmeli, gözenek tıkamayan hafif formülleri tercih etmeli ve makyajınızı gün sonunda mutlaka derinlemesine temizlemelisiniz. 💄";
                }
                else
                {
                    tavsiye = "Kokuların kalıcılığını artırmak için parfümü kuru cilde değil, nemlendirilmiş cilt noktalarına (bilek içi, boyun) sıkmalı ve deodorantları temiz cilde uygulamalısınız. 🌸";
                }

                return $"{tavsiye}\n\nŞu an GraWat stoklarımızda bu spesifik ihtiyaca yönelik bir ürünümüz kalmamış olsa da, bahsettiğim bu adımlara dikkat etmenizi kesinlikle öneririm! ✨";
            }

            if (matchedCategories.Contains("Saç Bakım"))
            {
                return $"Saç bakımınız için GraWat uzmanı olarak saç tellerini besleyecek ve dökülmeyi engelleyecek şu ürünlerimizi öneririm:\n- {urunListesiText}\n\nDüzenli kullanımda saçlarınızın parladığını fark edeceksiniz! 🧴💇‍♀️";
            }
            if (matchedCategories.Contains("Cilt Bakım") || matchedCategories.Contains("Güneş Ürünleri"))
            {
                return $"Cilt bakımınız için nem dengesini sağlayacak ve cildinizi tazeleyecek şu ürünlerimizi tavsiye ederim:\n- {urunListesiText}\n\nCildinizi her zaman temiz tutmayı ve nemlendirmeyi unutmayın! 🧴✨";
            }
            if (matchedCategories.Contains("Makyaj"))
            {
                return $"Göz alıcı ve doğal bir görünüm elde etmeniz için mağazamızdaki şu makyaj ürünlerini öneririm:\n- {urunListesiText}\n\nGüzelliğinize güzellik katacak! 💄✨";
            }
            if (matchedCategories.Contains("Parfüm/Deodorant"))
            {
                return $"Kokunuzla iz bırakmanız için GraWat koleksiyonundaki şu parfümlerimizi keşfetmenizi öneririm:\n- {urunListesiText}\n\nGün boyu taze ve kalıcı koku sunar! 🌸💨";
            }

            // Genel selamlaşma veya diğer durumlar
            if (msg.Contains("merhaba") || msg.Contains("selam") || msg.Contains("hey"))
            {
                return "Merhaba! Ben GraWat premium kozmetik mağazasının baş güzellik uzmanıyım. 🌸 Size cilt, saç bakımı veya makyaj rutinleriniz hakkında profesyonel öneriler sunmak için buradayım. Bugün nasıl yardımcı olabilirim?";
            }
            if (msg.Contains("teşekkür") || msg.Contains("sağ ol") || msg.Contains("teşekkürler"))
            {
                return "Rica ederim! GraWat Baş Güzellik Uzmanı olarak her zaman yanınızdayım. Kendinize çok iyi bakın, ışıldamaya devam edin! ✨💖";
            }

            return $"GraWat Baş Güzellik Uzmanı olarak buradayım! 🌸 Cilt bakımı, saç bakımı, parfümler ve makyaj rutinleriniz hakkında konuşabiliriz. Size en uygun öneriyi sunabilmem için ihtiyaç duyduğunuz alanı paylaşabilirsiniz.";
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
    }

    public class GeminiMessage
    {
        public string role { get; set; } = ""; // "user" or "model"
        public List<GeminiPart> parts { get; set; } = new List<GeminiPart>();
    }

    public class GeminiPart
    {
        public string text { get; set; } = "";
    }
}
