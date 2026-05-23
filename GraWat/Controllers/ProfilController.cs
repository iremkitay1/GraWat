using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
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
        public async Task<IActionResult> Favorilerim(string favoriArama)
        {
            // 1. Giriş yapan kullanıcının kimliğini alıyoruz
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // 2. Güvenli ve dinamik sorgu başlangıcı
            var favoriUrunlerQuery = _context.Urunler.AsQueryable();

            // 3. ÖZEL ARAMA BUTONU ÇALIŞTIYSA: Sadece favoriler içinde ara
            if (!string.IsNullOrEmpty(favoriArama))
            {
                favoriUrunlerQuery = favoriUrunlerQuery.Where(u => u.Ad.Contains(favoriArama));
                ViewBag.FavoriAramaKelimesi = favoriArama; // Arama kutusunda kelime yazılı kalsın diye
            }

            // 4. Verileri asenkron olarak listeye döküp View'a gönderiyoruz
            var favoriUrunlerListesi = await favoriUrunlerQuery.ToListAsync();
            return View(favoriUrunlerListesi);
        }
    }
}