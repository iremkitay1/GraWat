using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GraWat.Data;
using GraWat.Models;

namespace GraWat.Controllers
{
    public class SiparisController : Controller
    {
        private readonly GraWatContext _grawatContext;
        private readonly ApplicationDbContext _context;

        public SiparisController(GraWatContext grawatContext, ApplicationDbContext context)
        {
            _grawatContext = grawatContext;
            _context = context;
        }

        // ---------------------------------------------------------
        // 1. MÜŞTERİ PANELİ: Kullanıcının kendi geçmiş siparişlerini listeler.
        // ---------------------------------------------------------
        [Authorize]
        public async Task<IActionResult> Siparislerim()
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var siparisler = await _grawatContext.Siparisler
                .Where(s => s.KullaniciId == kullaniciId)
                .OrderByDescending(s => s.SiparisTarihi)
                .ToListAsync();

            return View(siparisler);
        }

        // ---------------------------------------------------------
        // 2. ADMIN PANELİ: Tüm müşterilerin siparişlerini ve CİRO hesaplarını gösterir.
        // ---------------------------------------------------------
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TumSiparisler()
        {
            var tumSiparisler = await _grawatContext.Siparisler
                .OrderByDescending(s => s.SiparisTarihi)
                .ToListAsync();

            var bugun = DateTime.Today;
            var buAy = DateTime.Now.Month;
            var buYil = DateTime.Now.Year;

            decimal gunlukCiro = tumSiparisler
                .Where(s => s.SiparisTarihi.Date == bugun)
                .Sum(s => s.ToplamTutar);

            decimal aylikCiro = tumSiparisler
                .Where(s => s.SiparisTarihi.Month == buAy && s.SiparisTarihi.Year == buYil)
                .Sum(s => s.ToplamTutar);

            ViewBag.GunlukCiro = gunlukCiro;
            ViewBag.AylikCiro = aylikCiro;

            return View(tumSiparisler);
        }

        // ---------------------------------------------------------
        // 3. ADMIN PANELİ - DETAY: Tek bir siparişin detaylarını gösterir.
        // ---------------------------------------------------------
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SiparisDetay(int id)
        {
            var siparis = await _grawatContext.Siparisler
                .FirstOrDefaultAsync(s => s.Id == id);

            if (siparis == null)
            {
                return NotFound();
            }

            return View(siparis);
        }

        // ---------------------------------------------------------
        // 4. ADMIN PANELİ: Siparişin durumunu manuel günceller (Raw SQL)
        // ---------------------------------------------------------
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SiparisiTamamla(int id)
        {
            await _grawatContext.Database.ExecuteSqlRawAsync(
                "UPDATE Siparisler SET Durum = 'Tamamlandı' WHERE Id = {0}", id
            );

            return RedirectToAction("SiparisDetay", new { id = id });
        }

        // ---------------------------------------------------------
        // 5. ÜRÜN DEĞERLENDİRME SAYFASI (GET): Kesin Çözümlü Versiyon 🚀
        // ---------------------------------------------------------
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Degerlendir(int urunId)
        {
            int siparisId = urunId;

            var siparis = await _grawatContext.Siparisler
                .FirstOrDefaultAsync(s => s.Id == siparisId);

            if (siparis == null)
            {
                return NotFound();
            }

            // 🛠️ Veritabanındaki SiparisKalemleri tablosunda bu sipariş numarasına ait satır sayısını buluyoruz
            var testKalemSayisi = await _grawatContext.SiparisKalemleri
                .Where(k => k.SiparisId == siparisId)
                .CountAsync();

            // Çıkan sonucu ekrana basması için ViewBag'e atıyoruz
            ViewBag.KalemSayisiMesaji = $"Veritabanında Bu Siparişe Ait Kayıt Sayısı: {testKalemSayisi}";

            // Mevcut join sorgun aynen kalsın
            var urunListesi = await _grawatContext.SiparisKalemleri
                .Where(k => k.SiparisId == siparisId)
                .Join(_grawatContext.Urunler,
                      kalem => kalem.UrunId,
                      urun => urun.Id,
                      (kalem, urun) => new UrunYorumItem
                      {
                          UrunId = urun.Id,
                          UrunAd = urun.Ad,
                          Kategori = urun.Kategori
                      })
                .ToListAsync();

            var viewModel = new SiparisDegerlendirmeVM
            {
                SiparisId = siparis.Id,
                Urunler = urunListesi
            };

            return View(viewModel);
        }
        // ---------------------------------------------------------
        // 6. YORUMLARI TOPLU KAYDETME İŞLEMİ (POST)
        // ---------------------------------------------------------
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> YorumKaydet(SiparisDegerlendirmeVM model)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var kullaniciAdi = User.Identity.Name ?? "Değerli Müşterimiz";

            if (model != null && model.Urunler != null && model.Urunler.Any())
            {
                foreach (var item in model.Urunler)
                {
                    var yeniYorum = new Yorum
                    {
                        UrunId = item.UrunId,
                        KullaniciId = kullaniciId,
                        KullaniciAdi = kullaniciAdi,
                        Puan = item.Puan,
                        YorumMetni = item.YorumMetni,
                        Tarih = DateTime.Now
                    };

                    _grawatContext.Yorumlar.Add(yeniYorum);
                }

                await _grawatContext.SaveChangesAsync();
            }

            return RedirectToAction("Siparislerim");
        }
        // ---------------------------------------------------------
        // 7. TEKLİ ÜRÜN YORUMU KAYDETME İŞLEMİ (POST)
        // ---------------------------------------------------------
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> TekliYorumKaydet(int siparisId, int urunId, int puan, string yorumMetni)
        {
            // Giriş yapan kullanıcının bilgilerini alıyoruz
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var kullaniciAdi = User.Identity.Name ?? "Değerli Müşterimiz";

            // Gelen verilerle tek bir Yorum nesnesi oluşturuyoruz
            var yeniYorum = new Yorum
            {
                UrunId = urunId,
                KullaniciId = kullaniciId,
                KullaniciAdi = kullaniciAdi,
                Puan = puan,
                YorumMetni = yorumMetni,
                Tarih = DateTime.Now
            };

            _grawatContext.Yorumlar.Add(yeniYorum);
            await _grawatContext.SaveChangesAsync();

            // Kullanıcı yorumu kaydettiğinde sayfayı yeniliyoruz ki 
            // kaldığı yerden (aynı siparişteki) diğer ürünleri de isterse yorumlayabilsin!
            return RedirectToAction("Degerlendir", new { urunId = siparisId });
        }
    }
}