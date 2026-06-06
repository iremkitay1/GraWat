using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using GraWat.Data;
using GraWat.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

namespace GraWat.Controllers
{
    public class ProfilController : Controller
    {
        private readonly GraWatContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ProfilController(
            GraWatContext context, 
            UserManager<IdentityUser> userManager, 
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // --- FAVORİLERİM VE ÖZEL FAVORİ İÇİN ARAMA METODU ---
        [Authorize]
        public async Task<IActionResult> Favorilerim(string favoriArama)
        {
            // 1. Giriş yapan kullanıcının kimliğini alıyoruz
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // 2. Güvenli ve dinamik sorgu başlangıcı
            var favoriQuery = _context.Favoriler
                .Include(f => f.Urun) // İlişkili ürünü dahil ediyoruz
                .Where(f => f.KullaniciId == userId);

            // 3. ÖZEL ARAMA BUTONU ÇALIŞTIYSA: Sadece favoriler içinde ara
            if (!string.IsNullOrEmpty(favoriArama))
            {
                favoriQuery = favoriQuery.Where(f => f.Urun.Ad.Contains(favoriArama));
                ViewBag.FavoriAramaKelimesi = favoriArama;
            }

            // 4. Verileri asenkron olarak listeye döküyoruz
            var favoriler = await favoriQuery.ToListAsync();
            var favoriUrunlerListesi = favoriler.Select(f => f.Urun).ToList();

            // 5. YAPAY ZEKA DESTEKLİ AKILLI ÖNERİLER (Eğer favorilerde ürün varsa)
            if (favoriUrunlerListesi.Any())
            {
                var favoriUrunlerMetni = string.Join(", ", favoriUrunlerListesi.Select(u => $"{u.Ad} ({u.Kategori})"));
                var prompt = $"Bir müşteri şu ürünleri favorilerine ekledi: {favoriUrunlerMetni}. Bu müşteriye mağazamızdan sepetini büyütmesi için hangi 2 farklı kozmetik/bakım ürününü veya rutini önerirsin? Çok kısa ve çekici bir dille açıkla.";

                var recommendation = await GetAIRecommendationAsync(prompt, favoriUrunlerListesi);
                ViewBag.AIRecommendation = recommendation;
            }

            return View(favoriUrunlerListesi);
        }

        // --- FAVORİDEN KALDIRMA METODU ---
        [Authorize]
        public async Task<IActionResult> FavoriIslemi(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var favori = await _context.Favoriler
                .FirstOrDefaultAsync(f => f.UrunId == id && f.KullaniciId == userId);

            if (favori != null)
            {
                _context.Favoriler.Remove(favori);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Favorilerim));
        }

        private async Task<string> GetAIRecommendationAsync(string prompt, List<Urun> favoriUrunler)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            }

            // Eğer API Anahtarı tanımlı değilse, akıllı fallback (yerel simülasyon) modunu çalıştırıyoruz
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("YOUR_GEMINI_API_KEY_HERE", StringComparison.OrdinalIgnoreCase))
            {
                return GetMockRecommendation(favoriUrunler);
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
                                new { text = prompt }
                            }
                        }
                    },
                    systemInstruction = new
                    {
                        parts = new[]
                        {
                            new { text = "Sen GraWat kozmetik mağazasının profesyonel güzellik uzmanı ve akıllı satış asistanısın. Müşterinin beğendiği ürünlere göre sepetini büyütecek 2 adet cazip ürün veya rutin öner. Yanıtın kısa, ilgi çekici, kibar ve Türkçe olsun. HTML etiketleri yerine basit metin ve emojiler kullan." }
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
                    return GetMockRecommendation(favoriUrunler) + "<br><small class='text-warning'>*(Not: Gemini API isteği başarısız oldu, demo modunda öneri üretildi)*</small>";
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

                return string.IsNullOrWhiteSpace(replyText) ? GetMockRecommendation(favoriUrunler) : replyText.Trim();
            }
            catch
            {
                return GetMockRecommendation(favoriUrunler) + "<br><small class='text-warning'>*(Not: Bağlantı hatası oluştu, demo modunda öneri üretildi)*</small>";
            }
        }

        private string GetMockRecommendation(List<Urun> favoriUrunler)
        {
            // Kategorilere göre akıllı öneri üretme
            var kategoriler = favoriUrunler.Select(u => u.Kategori?.ToLower(new System.Globalization.CultureInfo("tr-TR")) ?? "").ToList();

            if (kategoriler.Any(k => k.Contains("makyaj")))
            {
                return "Makyaj koleksiyonunuzu tamamlamak için harika seçimler! Beğendiğiniz makyaj tonlarının kalıcılığını gün boyu koruyacak bir **Makyaj Sabitleyici Sprey** ve kirpiklerinize olağanüstü hacim kazandıracak **Hacim Veren Siyah Maskaramızı** sepetinize eklemenizi öneririz! 💄✨";
            }
            if (kategoriler.Any(k => k.Contains("cilt") || k.Contains("bakım")))
            {
                return "Cildiniz için mükemmel bir rutin başlangıcı! Tercih ettiğiniz bakım ürünleriyle sinerji yaratarak cildinizin nem dengesini kilitleyecek **Yoğun Nemlendirici Hyalüronik Asit Serumu** ve gözenek görünümünü azaltacak **Sıkılaştırıcı Tonik** ürünümüzü rutininize ekleyebilirsiniz! 🧴🍃";
            }
            if (kategoriler.Any(k => k.Contains("parfüm") || k.Contains("koku") || k.Contains("deodorant")))
            {
                return "Harika koku tercihleri! Kokunuzun kalıcılığını maksimuma çıkarmak için aynı koku ailesinden **Nemlendirici Vücut Losyonumuzu** ve gün boyu tazelik sunan **Doğal Deodorant Spreyimizi** sepetinize eklemelisiniz! 🌸💨";
            }

            return "Seçtiğiniz bu harika ürünlerin yanına çok yakışacak, cildinizi neme doyururken canlandıracak **GraWat Doğal Vitamin C Kremi** ve tazelik veren **Çiçeksi El Kremi** ikilisini sepetinize ekleyerek alışveriş keyfinizi katlayabilirsiniz! 🌸💖";
        }
    }
}