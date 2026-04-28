using Microsoft.AspNetCore.Mvc;
using GraWat.Data;
using GraWat.Models;
using System.Diagnostics;

namespace GraWat.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // Veritabanı köprümüz

        // Constructor (Kurucu Metot) güncellendi
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // 1. Parametre kısmına searchString ekledik
        public IActionResult Index(string kategori, string searchString)
        {
            // 1. Veritabanındaki ürünlerimizi bir sorgu olarak hazırlıyoruz
            var urunlerQuery = _context.Urunler.AsQueryable();

            // 2. EĞER arama kutusuna bir şey yazılmışsa... (YENİ KISIM)
            if (!string.IsNullOrEmpty(searchString))
            {
                // Ürün adında aranan kelime geçenleri filtrele
                urunlerQuery = urunlerQuery.Where(u => u.Ad.Contains(searchString));

                // Arama kelimesini sayfada gösterebilmek için Viewbag'e atalım
                ViewBag.ArananKelime = searchString;
            }

            // 3. EĞER dışarıdan (çekmeceden) bir kategori ismi gelmişse...
            if (!string.IsNullOrEmpty(kategori))
            {
                urunlerQuery = urunlerQuery.Where(u => u.Kategori == kategori);
            }

            // 4. Sonucu listeye çevirip sayfaya (View) gönderiyoruz
            var urunlerListesi = urunlerQuery.ToList();
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