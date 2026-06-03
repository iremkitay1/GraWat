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
                .Where(f => f.KullaniciId == userId);

            // 3. ÖZEL ARAMA BUTONU ÇALIŞTIYSA: Sadece favoriler içinde ara
            if (!string.IsNullOrEmpty(favoriArama))
            {
                favoriQuery = favoriQuery.Where(f => f.Urun.Ad.Contains(favoriArama));
                ViewBag.FavoriAramaKelimesi = favoriArama; // Arama kutusunda kelime yazılı kalsın diye
            }

            // 4. Verileri asenkron olarak listeye döküp View'a gönderiyoruz
            var favoriUrunlerListesi = await favoriQuery.Select(f => f.Urun).ToListAsync();
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
    }
}