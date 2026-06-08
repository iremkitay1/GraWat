using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
                    return Json(new { success = false, message = "Sepete ürün eklemek için lütfen giriş yapınız.", redirectUrl = "/Identity/Account/Login" });
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

            var yeniSepetAdedi = await _context.SepetItems
                .Where(s => s.KullaniciId == kullaniciId)
                .SumAsync(s => s.Adet);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Ürün sepete eklendi!", cartCount = yeniSepetAdedi });
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

        // SİPARİŞİ ONAYLAMA (Stok Kontrolü, Stok Düşüşü ve Atomik Save Destekli Versiyon 🚀)
        [HttpPost]
        public async Task<IActionResult> SiparisOnayla(
            string odemeYontemi,
            string? kartIsim = null,
            string? cardNo = null,
            string? expiryDate = null,
            string? cvv = null)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Ödeme yöntemi kapıda ödeme ise kart doğrulamalarını ModelState'den temizliyoruz
            if (odemeYontemi == "Kapıda Nakit" || odemeYontemi == "Kapıda Kart" || odemeYontemi == "Kapıda Kredi Kartı")
            {
                ModelState.Remove("CardNumber");
                ModelState.Remove("cardNo");
                ModelState.Remove("kartIsim");
                ModelState.Remove("expiryDate");
                ModelState.Remove("cvv");
            }
            else
            {
                // Kredi kartı seçildiyse kart alanlarını el ile doğruluyoruz
                if (string.IsNullOrWhiteSpace(kartIsim))
                {
                    ModelState.AddModelError("kartIsim", "Kart üzerindeki isim alanı zorunludur.");
                }
                if (string.IsNullOrWhiteSpace(cardNo) || cardNo.Replace(" ", "").Length < 16)
                {
                    ModelState.AddModelError("cardNo", "Lütfen 16 haneli geçerli bir kart numarası giriniz.");
                }
                if (string.IsNullOrWhiteSpace(expiryDate) || !expiryDate.Contains("/") || expiryDate.Length != 5)
                {
                    ModelState.AddModelError("expiryDate", "Son kullanma tarihi MM/YY formatında olmalıdır.");
                }
                if (string.IsNullOrWhiteSpace(cvv) || cvv.Length != 3)
                {
                    ModelState.AddModelError("cvv", "CVV kodu 3 haneli olmalıdır.");
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Lütfen alanları kontrol ediniz: " + errors });
            }

            // 1. Sepeti fiyatlarla birlikte çekiyoruz
            var kullaniciSepeti = await _context.SepetItems
                .Include(s => s.Urun)
                .Where(s => s.KullaniciId == kullaniciId)
                .ToListAsync();

            if (!kullaniciSepeti.Any())
            {
                return Json(new { success = false, message = "Sepetiniz boş!" });
            }

            // 2. Güvenlik ve Stok Kontrolü (Validation)
            foreach (var sepetUrunu in kullaniciSepeti)
            {
                // Güncel veritabanı stok durumunu sorguluyoruz
                var urun = await _context.Urunler.FindAsync(sepetUrunu.UrunId);
                if (urun == null || urun.StokAdedi < sepetUrunu.Adet)
                {
                    return Json(new { success = false, message = "Üzgünüz, bazı ürünlerin stoğu tükenmiş veya yetersiz." });
                }
            }

            // 3. Atomik Veritabanı İşlemleri (Tek bir _context nesnesi üzerinden transaction kullanarak veri bütünlüğünü koruyoruz 🚀)
            await using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 3.1. Toplam tutarı hesaplıyoruz
                    decimal gercekToplamTutar = kullaniciSepeti.Sum(item => item.Adet * item.Urun.Fiyat);

                    // 3.2. Yeni Siparişi Oluşturuyoruz (Kapıda ödemeler için status 'Bekliyor')
                    var yeniSiparis = new GraWat.Models.Siparis
                    {
                        KullaniciId = kullaniciId,
                        SiparisTarihi = DateTime.Now,
                        ToplamTutar = gercekToplamTutar,
                        Durum = (odemeYontemi == "Kapıda Nakit" || odemeYontemi == "Kapıda Kart" || odemeYontemi == "Kapıda Kredi Kartı") ? "Bekliyor" : "Hazırlanıyor",
                        OdemeYontemi = odemeYontemi ?? "Kredi Kartı"
                    };

                    _context.Siparisler.Add(yeniSiparis);
                    await _context.SaveChangesAsync(); // SiparisId üretilmesi için

                    // 3.3. Sipariş Kalemlerini ve Stok Düşümlerini gerçekleştiriyoruz
                    foreach (var sepetUrunu in kullaniciSepeti)
                    {
                        var urun = await _context.Urunler.FindAsync(sepetUrunu.UrunId);
                        if (urun == null || urun.StokAdedi < sepetUrunu.Adet)
                        {
                            throw new InvalidOperationException("Yetersiz stok veya geçersiz ürün.");
                        }

                        // Stok düşme işlemi (Deduction)
                        urun.StokAdedi -= sepetUrunu.Adet;

                        var yeniKalem = new SiparisKalemi
                        {
                            SiparisId = yeniSiparis.Id,
                            UrunId = sepetUrunu.UrunId,
                            Adet = sepetUrunu.Adet,
                            Fiyat = sepetUrunu.Urun.Fiyat
                        };

                        _context.SiparisKalemleri.Add(yeniKalem);
                    }

                    // Sipariş kalemlerini ve stok güncellemelerini kaydediyoruz
                    await _context.SaveChangesAsync();

                    // 3.4. Sepeti temizliyoruz
                    _context.SepetItems.RemoveRange(kullaniciSepeti);
                    await _context.SaveChangesAsync();

                    // Her şey başarılı olduysa işlemleri onaylayıp kaydediyoruz
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Siparişiniz başarıyla alındı!" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine("Sipariş Onaylama Hatası (Try-Catch): " + ex.ToString());
                    ModelState.AddModelError("Exception", ex.Message);
                    return Json(new { success = false, message = "Sipariş işlenirken bir hata oluştu: " + ex.Message });
                }
            }
        }

        public IActionResult SiparisBasarili()
        {
            return View();
        }
    }
}