using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using GraWat.Data;
using GraWat.Models;
using Microsoft.EntityFrameworkCore;

namespace GraWat.Controllers
{
    public class ProfilController : Controller
    {
        private readonly GraWatContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfilController(GraWatContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- FAVORİLERİM VE ÖZEL FAVORİ İÇİ ARAMA METODU ---
        [Authorize]
        public async Task<IActionResult> Favorilerim(string favoriArama, int page = 1)
        {
            // 1. Giriş yapan kullanıcının kimliğini alıyoruz
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // 2. Güvenli ve dinamik sorgu başlangıcı
            var favoriQuery = _context.Favoriler
                .Where(f => f.KullaniciId == userId);

            // 3. ÖZEL ARAMA BUTONU ÇALIŞTIYSA: Sadece favoriler içinde ara
            if (!string.IsNullOrEmpty(favoriArama))
            {
                favoriQuery = favoriQuery.Where(f => f.Urun.Ad.Contains(favoriArama));
                ViewBag.FavoriAramaKelimesi = favoriArama; // Arama kutusunda kelime yazılı kalsın diye
            }

            // 4. Sayfalama Hesaplamaları
            int pageSize = 6;
            int totalItems = await favoriQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            // 5. Verileri asenkron olarak listeye döküp View'a gönderiyoruz
            var favoriUrunlerListesi = await favoriQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => f.Urun)
                .ToListAsync();
                
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

        // --- AJAX FAVORİ EKLE/ÇIKAR ---
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Favorilere eklemek için lütfen giriş yapınız.", redirectUrl = "/Identity/Account/Login" });
                }
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var favori = await _context.Favoriler
                .FirstOrDefaultAsync(f => f.UrunId == id && f.KullaniciId == userId);

            bool isAdded;
            if (favori != null)
            {
                _context.Favoriler.Remove(favori);
                isAdded = false;
            }
            else
            {
                var yeniFavori = new Favoriler
                {
                    UrunId = id,
                    KullaniciId = userId
                };
                _context.Favoriler.Add(yeniFavori);
                isAdded = true;
            }

            await _context.SaveChangesAsync();

            var favoritesCount = await _context.Favoriler.CountAsync(f => f.KullaniciId == userId);

            return Json(new { 
                success = true, 
                isAdded = isAdded, 
                favoritesCount = favoritesCount, 
                message = isAdded ? "Ürün favorilerinize eklendi!" : "Ürün favorilerinizden çıkarıldı!" 
            });
        }
    }
}