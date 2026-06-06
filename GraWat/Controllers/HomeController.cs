using Microsoft.AspNetCore.Mvc;
using GraWat.Data;
using GraWat.Models;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore; // ToListAsync ve AsQueryable için gerekli
using System.Security.Claims;

namespace GraWat.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        // DEĞİŞİKLİK: Artık GraWatContext kullanıyoruz!
        private readonly GraWatContext _context;

        // Constructor: Buradaki tipi de GraWatContext yaptık
        public HomeController(ILogger<HomeController> logger, GraWatContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index(List<string> kategoriler, string kategori, string searchString, decimal? minPrice, decimal? maxPrice)
        {
            var urunlerQuery = _context.Urunler.AsQueryable();

            // Arama Çubuğu Filtrelemesi
            if (!string.IsNullOrEmpty(searchString))
            {
                urunlerQuery = urunlerQuery.Where(u => u.Ad.Contains(searchString));
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
                var lowerKategoriler = secilenKategoriler.Select(k => k.Trim().ToLower()).ToList();
                urunlerQuery = urunlerQuery.Where(u => lowerKategoriler.Any(lk => 
                    u.Kategori.ToLower().Contains(lk) || lk.Contains(u.Kategori.ToLower())
                ));
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
    }
}