using Microsoft.AspNetCore.Mvc;
using GraWat.Data;
using Microsoft.AspNetCore.Authorization;
using GraWat.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GraWat.Controllers
{
    // DİKKAT: Buradaki genel [Authorize(Roles = "Admin")] kilidini KALDIRDIK.
    // Çünkü müşterilerin de Ürünleri görmesi (Index) ve Detayına (Details) bakması gerekiyor.
    public class UrunlerController : Controller
    {
        private readonly GraWatContext _context;

        public UrunlerController(GraWatContext context)
        {
            _context = context;
        }

        // --- 🛍️ MÜŞTERİ SAYFALARI (HERKES GİREBİLİR) ---

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var urun = await _context.Urunler.FirstOrDefaultAsync(m => m.Id == id);
            if (urun == null) return NotFound();

            ViewBag.Yorumlar = _context.Yorumlar
                .Where(x => x.UrunId == id)
                .OrderByDescending(x => x.Tarih)
                .ToList();

            // Doğrulanmış alıcı kontrolü
            bool hasPurchased = false;
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                hasPurchased = await _context.Siparisler
                    .AnyAsync(s => s.KullaniciId == userId && 
                                   s.SiparisKalemleri.Any(sk => sk.UrunId == id));
            }
            ViewBag.HasPurchased = hasPurchased;

            return View("Detay", urun);
        }

        [HttpPost]
        [Authorize] // Sadece giriş yapan müşteriler yorum ekleyebilir (Admin olması şart değil)
        public async Task<IActionResult> YorumEkle(int UrunId, int Puan, string YorumMetni)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Doğrulanmış alıcı kontrolü
            var hasPurchased = await _context.Siparisler
                .AnyAsync(s => s.KullaniciId == userId && 
                               s.SiparisKalemleri.Any(sk => sk.UrunId == UrunId));

            if (!hasPurchased)
            {
                TempData["YorumHata"] = "Sadece tamamlanmış siparişlerdeki ürünleri değerlendirebilirsiniz.";
                return RedirectToAction(nameof(Details), new { id = UrunId });
            }

            if (!string.IsNullOrEmpty(YorumMetni))
            {
                var yeniYorum = new Yorum
                {
                    UrunId = UrunId,
                    Puan = Puan,
                    YorumMetni = YorumMetni,
                    KullaniciId = userId,
                    KullaniciAdi = User.Identity.Name.Split('@')[0],
                    Tarih = DateTime.Now
                };

                _context.Yorumlar.Add(yeniYorum);
                await _context.SaveChangesAsync();

                // Ürünün puan ortalamasını ve toplam değerlendirme adedini güncelle
                var product = await _context.Urunler.FindAsync(UrunId);
                if (product != null)
                {
                    var productReviews = await _context.Yorumlar.Where(y => y.UrunId == UrunId).ToListAsync();
                    product.TotalReviews = productReviews.Count;
                    product.AverageRating = productReviews.Any() ? productReviews.Average(r => r.Puan) : 0.0;
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Details), new { id = UrunId });
        }


        // --- 👑 ADMİN PANELİ İŞLEMLERİ (KİLİTLİ ALANLAR) ---
        // Aşağıdaki tüm işlemler sadece Adminlere özeldir!

        [Authorize(Roles = "Admin")]
        public IActionResult Index(int? siparisId)
        {
            if (siparisId != null)
            {
                var urunIdleri = _context.SiparisKalemleri
                                        .Where(sk => sk.SiparisId == siparisId)
                                        .Select(sk => sk.UrunId)
                                        .ToList();

                var filtrelenmişUrunler = _context.Urunler
                                                .Where(u => urunIdleri.Contains(u.Id))
                                                .OrderByDescending(u => u.Id)
                                                .ToList();

                return View(filtrelenmişUrunler);
            }

            var tumUrunler = _context.Urunler.OrderByDescending(u => u.Id).ToList();
            return View(tumUrunler);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Ad,Kategori,Fiyat,StokAdedi")] Urun urun, IFormFile? resimDosyasi)
        {
            if (ModelState.IsValid)
            {
                if (resimDosyasi != null && resimDosyasi.Length > 0)
                {
                    var dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(resimDosyasi.FileName);
                    var yol = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", dosyaAdi);
                    using (var stream = new FileStream(yol, FileMode.Create))
                    {
                        await resimDosyasi.CopyToAsync(stream);
                    }
                    urun.ResimYolu = dosyaAdi;
                }
                _context.Add(urun);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(urun);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var urun = await _context.Urunler.FindAsync(id);
            if (urun == null) return NotFound();
            return View(urun);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Ad,Kategori,Fiyat,StokAdedi,ResimYolu")] Urun urun, IFormFile? resimDosyasi)
        {
            if (id != urun.Id) return NotFound();
            if (ModelState.IsValid)
            {
                if (resimDosyasi != null && resimDosyasi.Length > 0)
                {
                    var dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(resimDosyasi.FileName);
                    var yol = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", dosyaAdi);
                    using (var stream = new FileStream(yol, FileMode.Create))
                    {
                        await resimDosyasi.CopyToAsync(stream);
                    }
                    urun.ResimYolu = dosyaAdi;
                }
                _context.Update(urun);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(urun);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var urun = await _context.Urunler.FindAsync(id);
            if (urun == null) return NotFound();
            return View(urun);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var urun = await _context.Urunler.FindAsync(id);
            if (urun != null)
            {
                _context.Urunler.Remove(urun);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Yorumlar()
        {
            var yorumlar = await _context.Yorumlar
                .OrderByDescending(y => y.Tarih)
                .ToListAsync();

            return View(yorumlar);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> YorumSil(int id)
        {
            var yorum = await _context.Yorumlar.FindAsync(id);
            if (yorum != null)
            {
                var urunId = yorum.UrunId;
                _context.Yorumlar.Remove(yorum);
                await _context.SaveChangesAsync();

                // Yorum silindikten sonra ürünün ortalamasını ve değerlendirme sayısını yeniden hesapla
                var product = await _context.Urunler.FindAsync(urunId);
                if (product != null)
                {
                    var productReviews = await _context.Yorumlar.Where(y => y.UrunId == urunId).ToListAsync();
                    product.TotalReviews = productReviews.Count;
                    product.AverageRating = productReviews.Any() ? productReviews.Average(r => r.Puan) : 0.0;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Yorumlar));
        }
    }
}