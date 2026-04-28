using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GraWat.Data;
using GraWat.Models;
using System.Security.Claims;
using GraWat.Controllers;

namespace GraWat.Controllers
{
    public class SepetController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SepetController(ApplicationDbContext context)
        {
            _context = context;
        }

        // SEPETİM SAYFASI: Sepettekileri listeler
        public async Task<IActionResult> Index()
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sepet = await _context.SepetItems
              .Include(s => s.Urun)
              .Where(s => s.KullaniciId == kullaniciId)
              .ToListAsync();
            return View(sepet);
        }

        // SEPETE EKLEME FONKSİYONU
        public async Task<IActionResult> Ekle(int id)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. GİRİŞ KONTROLÜ (AJAX DOSTU)
            if (string.IsNullOrEmpty(kullaniciId))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Unauthorized(); // JS bunu yakalayıp Login'e atacak
                }
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // 2. SEPETE EKLEME MANTIĞI (Mevcut kodun)
            var sepetItem = await _context.SepetItems
                .FirstOrDefaultAsync(s => s.UrunId == id && s.KullaniciId == kullaniciId);

            if (sepetItem == null)
            {
                _context.SepetItems.Add(new SepetItem { UrunId = id, KullaniciId = kullaniciId, Adet = 1 });
            }
            else
            {
                sepetItem.Adet++;
            }

            await _context.SaveChangesAsync();

            // 3. YANIT KISMI
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(); // Sayfayı yenileme, sadece "İşlem Tamam" de
            }

            return RedirectToAction("Index", "Home");
        }
        // SEPETTEN ÜRÜN SİLME FONKSİYONU
        public async Task<IActionResult> Sil(int id)
        {
            // Veritabanından o sepet satırını bul
            var sepetItem = await _context.SepetItems.FindAsync(id);

            if (sepetItem != null)
            {
                _context.SepetItems.Remove(sepetItem);
                await _context.SaveChangesAsync(); // Değişikliği SQL'e kaydet
            }

            return RedirectToAction(nameof(Index)); // Tekrar sepet sayfasına dön
        }
        // ADET ARTIRMA (+)
        public async Task<IActionResult> Artir(int id)
        {
            var sepetItem = await _context.SepetItems.FindAsync(id);
            if (sepetItem != null)
            {
                sepetItem.Adet++;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ADET AZALTMA (-)
        public async Task<IActionResult> Azalt(int id)
        {
            var sepetItem = await _context.SepetItems.FindAsync(id);
            if (sepetItem != null)
            {
                if (sepetItem.Adet > 1)
                {
                    sepetItem.Adet--;
                }
                else
                {
                    // Eğer adet 1 ise ve tekrar eksiye basılırsa ürünü tamamen silsin
                    _context.SepetItems.Remove(sepetItem);
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ÖDEME SAYFASI
        public IActionResult Odeme()
        {
            // Sepette ürün var mı kontrolü (İsteğe bağlı)
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SiparisOnayla()
        {
            // 1. İşlem: Kullanıcının kim olduğunu bul
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. İşlem: Bu kullanıcının sepetindeki her şeyi bul
            var kullaniciSepeti = _context.SepetItems.Where(s => s.KullaniciId == kullaniciId);

            // 3. İşlem: Sepeti veritabanından tamamen sil (Sepet 0'a düşsün)
            _context.SepetItems.RemoveRange(kullaniciSepeti);
            await _context.SaveChangesAsync();

            // 4. İşlem: Seni yeni hazırlayacağımız şık sayfaya gönder
            return RedirectToAction(nameof(SiparisBasarili));
        }

        // Bu da yeni sayfamızı açacak olan kapı
        public IActionResult SiparisBasarili()
        {
            return View();
        }
    }
}