using Microsoft.AspNetCore.Mvc;
using GraWat.Data;
using GraWat.Models;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq.Expressions;

namespace GraWat.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GraWatContext _context;

        // Alt kategorilerin veritabanındaki ürün adlarıyla esnek eşleşmesi için sözlük
        private static readonly Dictionary<string, List<string>> SubcategoryKeywords = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            // Makyaj
            { "Ruj", new List<string> { "ruj" } },
            { "Maskara", new List<string> { "maskara" } },
            { "Fondöten", new List<string> { "fondöten", "fondoten" } },

            // Cilt Bakım
            { "Nemlendirici", new List<string> { "nemlendirici", "krem" } },
            { "Yüz Temizleme Jeli", new List<string> { "temizleme jeli", "yüz temizleme", "jel" } },
            { "Tonik", new List<string> { "tonik" } },

            // Güneş Ürünleri
            { "Yüz Güneş Kremi", new List<string> { "güneş kremi", "gunes kremi", "koruyucu" } },
            { "Vücut Güneş Kremi", new List<string> { "güneş", "gunes", "vücut" } },
            { "Bronzlaştırıcı Yağ", new List<string> { "bronzlaştırıcı", "bronzlastirici", "yağ", "yag", "kakao" } },

            // Saç Bakım
            { "Şampuan", new List<string> { "şampuan", "sampuan" } },
            { "Saç Kremi", new List<string> { "saç kremi", "sac kremi" } },
            { "Saç Maskesi", new List<string> { "saç maskesi", "sac maskesi", "maske" } },
            { "Saç Boyası", new List<string> { "saç boyası", "sac boyasi", "boya" } },

            // Parfüm & Deodorant
            { "Kadın Parfüm", new List<string> { "kadın parfüm", "kadin parfum", "kadın parfümü", "kadin parfumu" } },
            { "Erkek Parfüm", new List<string> { "erkek parfüm", "erkek parfum", "erkek parfümü", "erkek parfumu" } },
            { "Deodorant & Roll-on", new List<string> { "deodorant", "roll-on", "roll on" } },

            // Erkek Bakım
            { "Tıraş Ürünleri", new List<string> { "tıraş", "tiras", "balsam" } },
            { "Sakal & Bıyık Bakımı", new List<string> { "sakal", "bıyık", "biyik" } },
            { "Erkek Cilt Bakımı", new List<string> { "erkek", "cilt", "krem", "yüz kremi" } },

            // Kişisel Bakım
            { "Ağız & Diş Bakımı", new List<string> { "ağız ve diş", "agiz ve dis", "macun", "diş", "dis" } },
            { "Duş Jeli & Banyo", new List<string> { "duş jeli", "dus jeli", "banyo" } },
            { "Kadın Hijyen Ürünleri", new List<string> { "hijyenik ped", "ped", "hijyen" } }
        };

        public HomeController(ILogger<HomeController> logger, GraWatContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index(List<string> kategoriler, string kategori, string searchString, decimal? minPrice, decimal? maxPrice)
        {
            var urunlerQuery = _context.Urunler.AsQueryable();

            // Arama Çubuğu Filtrelemesi (Alt Kategori & Arama Kelimesi Uyumlu)
            if (!string.IsNullOrEmpty(searchString))
            {
                string searchTrimmed = searchString.Trim();
                var searchTerms = new List<string> { searchTrimmed.ToLower() };
                
                // Eğer aranan kelime bir alt kategori ismiyle eşleşiyorsa, onun anahtar kelimelerini de ekle
                string normalizedSearch = NormalizeString(searchTrimmed);
                foreach (var key in SubcategoryKeywords.Keys)
                {
                    if (NormalizeString(key) == normalizedSearch)
                    {
                        searchTerms.AddRange(SubcategoryKeywords[key].Select(k => k.ToLower()));
                    }
                }
                
                searchTerms = searchTerms.Distinct().ToList();
                var searchPredicate = BuildCategoryPredicate(searchTerms);
                urunlerQuery = urunlerQuery.Where(searchPredicate);
                
                ViewBag.ArananKelime = searchString;
            }

            // Çoklu ve Tekil Kategori Filtrelemesi (Çapraz Bağlantı Uyumlu)
            var secilenKategoriler = new List<string>();
            if (kategoriler != null && kategoriler.Any())
            {
                secilenKategoriler.AddRange(kategoriler);
            }
            if (!string.IsNullOrEmpty(kategori))
            {
                secilenKategoriler.Add(kategori);
            }

            if (secilenKategoriler.Any())
            {
                var targetTerms = new List<string>();
                foreach (var cat in secilenKategoriler)
                {
                    targetTerms.Add(cat.Trim().ToLower());
                    string normalizedCat = NormalizeString(cat);
                    
                    var matchedCategory = GetNormalizedCategoryMatch(cat);
                    if (!string.IsNullOrEmpty(matchedCategory))
                    {
                        targetTerms.Add(matchedCategory.ToLower());
                    }

                    foreach (var key in SubcategoryKeywords.Keys)
                    {
                        if (NormalizeString(key) == normalizedCat)
                        {
                            targetTerms.AddRange(SubcategoryKeywords[key].Select(k => k.ToLower()));
                        }
                    }
                }
                targetTerms = targetTerms.Distinct().ToList();

                var categoryPredicate = BuildCategoryPredicate(targetTerms);
                urunlerQuery = urunlerQuery.Where(categoryPredicate);
            }

            // Fiyat Aralığı Filtrelemesi
            if (minPrice.HasValue)
            {
                urunlerQuery = urunlerQuery.Where(u => u.Fiyat >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                urunlerQuery = urunlerQuery.Where(u => u.Fiyat <= maxPrice.Value);
            }

            var urunlerListesi = urunlerQuery.ToList();

            // Favori ürün ID'lerini ViewBag'e aktarma
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.FavoriUrunIds = _context.Favoriler
                    .Where(f => f.KullaniciId == userId)
                    .Select(f => f.UrunId)
                    .ToList();
            }
            else
            {
                ViewBag.FavoriUrunIds = new List<int>();
            }

            // Eğer AJAX isteği ise sadece Kısmi Görünümü (Partial View) dönüyoruz
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ProductList", urunlerListesi);
            }

            return View(urunlerListesi);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // --- YARDIMCI FONKSİYONLAR ---

        private static string NormalizeString(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            return text.ToLowerInvariant()
                .Replace("ş", "s")
                .Replace("ç", "c")
                .Replace("ğ", "g")
                .Replace("ü", "u")
                .Replace("ö", "o")
                .Replace("ı", "i")
                .Replace("&", "and")
                .Replace("/", "-")
                .Replace(" ", "");
        }

        private static string GetNormalizedCategoryMatch(string text)
        {
            var normalized = NormalizeString(text);
            switch (normalized)
            {
                case "makyaj":
                    return "Makyaj";
                case "ciltbakim":
                case "ciltbakimi":
                    return "Cilt Bakım";
                case "gunesurunleri":
                    return "Güneş Ürünleri";
                case "sacbakim":
                case "sacbakimi":
                    return "Saç Bakım";
                case "parfumdeodorant":
                case "parfumodeodorant":
                case "parfumanddeodorant":
                case "parfumd-deodorant":
                    return "Parfüm/Deodorant";
                case "erkekbakim":
                case "erkekbakimi":
                    return "Erkek Bakım";
                case "kisiselbakim":
                    return "Kişisel Bakım";
                default:
                    return string.Empty;
            }
        }

        private static Expression<Func<Urun, bool>> BuildCategoryPredicate(List<string> terms)
        {
            var parameter = Expression.Parameter(typeof(Urun), "u");
            Expression body = Expression.Constant(false);

            var categoryProp = Expression.Property(parameter, nameof(Urun.Kategori));
            var nameProp = Expression.Property(parameter, nameof(Urun.Ad));

            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

            if (toLowerMethod == null || containsMethod == null)
            {
                return u => false;
            }

            var categoryLower = Expression.Call(categoryProp, toLowerMethod);
            var nameLower = Expression.Call(nameProp, toLowerMethod);

            foreach (var term in terms)
            {
                var termConstant = Expression.Constant(term.ToLower());

                var categoryContains = Expression.Call(categoryLower, containsMethod, termConstant);
                var nameContains = Expression.Call(nameLower, containsMethod, termConstant);

                var termMatch = Expression.OrElse(categoryContains, nameContains);
                body = Expression.OrElse(body, termMatch);
            }

            return Expression.Lambda<Func<Urun, bool>>(body, parameter);
        }
    }
}