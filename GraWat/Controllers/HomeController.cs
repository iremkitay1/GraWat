using Microsoft.AspNetCore.Mvc;
using GraWat.Data;
using GraWat.Models;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore; // ToListAsync ve AsQueryable için gerekli

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

        public IActionResult Index(string kategori, string searchString)
        {
            var urunlerQuery = _context.Urunler.AsQueryable();

            // Arama Çubuğu Filtrelemesi
            if (!string.IsNullOrEmpty(searchString))
            {
                urunlerQuery = urunlerQuery.Where(u => u.Ad.Contains(searchString));
                ViewBag.ArananKelime = searchString;
            }

            // KRİTİK DÜZELTME: Kategori Filtrelemesi (Büyük/Küçük Harf ve Türkçe Karakter Uyumlu)
            if (!string.IsNullOrEmpty(kategori))
            {
                // Linkten gelen "Cilt Bakım" kelimesini tamamen küçük harfe çeviriyoruz: "cilt bakım"
                string aranan = kategori.Trim().ToLower();

                // Veritabanındaki "Cilt Bakımı" değerini de küçük harfe çevirip 
                // linkten gelen "cilt bakım" kelimesini içeriyor mu diye bakıyoruz:
                urunlerQuery = urunlerQuery.Where(u => u.Kategori.ToLower().Contains(aranan) || aranan.Contains(u.Kategori.ToLower()));
            }

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