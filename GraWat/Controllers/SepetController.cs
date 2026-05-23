using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GraWat.Data;
using GraWat.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace GraWat.Controllers
{
    public class SepetController : Controller
    {
        private readonly ApplicationDbContext _context; // Sepet işlemleri için
        private readonly GraWatContext _grawatContext;  // Sipariş kaydı için

        public SepetController(ApplicationDbContext context, GraWatContext grawatContext)
        {
            _context = context;
            _grawatContext = grawatContext;
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

            if (string.IsNullOrEmpty(kullaniciId))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Unauthorized();
                }
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

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

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }

            return RedirectToAction("Index", "Home");
        }

        // SEPETTEN ÜRÜN SİLME
        public async Task<IActionResult> Sil(int id)
        {
            var sepetItem = await _context.SepetItems.FindAsync(id);
            if (sepetItem != null)
            {
                _context.SepetItems.Remove(sepetItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
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
                    _context.SepetItems.Remove(sepetItem);
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ÖDEME SEÇENEKLERİ SAYFASI
        public IActionResult Odeme()
        {
            return View();
        }

        // SİPARİŞİ ONAYLAMA (Eksiksiz Ürün Detay Kaydeden Versiyon 🚀)
        [HttpPost]
        public async Task<IActionResult> SiparisOnayla(string odemeYontemi)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Sepeti fiyatlarla birlikte çekiyoruz
            var kullaniciSepeti = await _context.SepetItems
                .Include(s => s.Urun)
                .Where(s => s.KullaniciId == kullaniciId)
                .ToListAsync();

            if (!kullaniciSepeti.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            // 2. Toplam tutarı hesaplıyoruz
            decimal gercekToplamTutar = kullaniciSepeti.Sum(item => item.Adet * item.Urun.Fiyat);

            // 3. Yeni Siparişi Oluşturuyoruz
            var yeniSiparis = new GraWat.Models.Siparis
            {
                KullaniciId = kullaniciId,
                SiparisTarihi = DateTime.Now,
                ToplamTutar = gercekToplamTutar,
                Durum = "Hazırlanıyor",
                OdemeYontemi = odemeYontemi ?? "Kredi Kartı"
            };

            // 4. Önce Ana Siparişi Kaydediyoruz (Böylece veritabanı otomatik bir Siparis.Id üretecek)
            _grawatContext.Siparisler.Add(yeniSiparis);
            await _grawatContext.SaveChangesAsync();

            // =================================================================
            // 🚀 EKSİK OLAN KISIM BURASIYDI: Sipariş Kalemlerini Tek Tek Kaydediyoruz
            // =================================================================
            foreach (var sepetUrunu in kullaniciSepeti)
            {
                var yeniKalem = new SiparisKalemi
                {
                    SiparisId = yeniSiparis.Id, // Üstte oluşan siparişin ID'sini bağlıyoruz
                    UrunId = sepetUrunu.UrunId,
                    Adet = sepetUrunu.Adet,
                    Fiyat = sepetUrunu.Urun.Fiyat
                };

                _grawatContext.SiparisKalemleri.Add(yeniKalem);
            }

            // Tüm ürün kalemlerini veritabanına topluca kaydediyoruz
            await _grawatContext.SaveChangesAsync();
            // =================================================================

            // 5. Sepeti ApplicationDbContext (SepetItems Tablosu) üzerinden temizliyoruz
            _context.SepetItems.RemoveRange(kullaniciSepeti);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(SiparisBasarili));
        }

        public IActionResult SiparisBasarili()
        {
            return View();
        }
    }
}