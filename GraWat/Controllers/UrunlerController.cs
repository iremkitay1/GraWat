using Microsoft.AspNetCore.Mvc;
using GraWat.Data;
using Microsoft.AspNetCore.Authorization;
using GraWat.Models;

namespace GraWat.Controllers
{
    public class UrunlerController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Veritabanı köprümüzü bağlıyoruz
        public UrunlerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Ürünleri Listeleme Sayfası (Herkese Açık)
        public IActionResult Index()
        {
            var urunler = _context.Urunler.ToList();
            return View(urunler);
        }



        // --- ---

        // 1. Ürün Ekleme Sayfasını Açan Kod (Sadece Admin Girebilir)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // 2. Form Doldurulup "Kaydet" Butonuna Basıldığında Çalışan Kod
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Ad,Kategori,Fiyat,StokAdedi")] Urun urun, IFormFile resimDosyasi)
        {
            if (ModelState.IsValid)
            {
                if (resimDosyasi != null && resimDosyasi.Length > 0)
                {
                    // Dosyaya eşsiz bir isim veriyoruz (Çakışma olmasın diye)
                    var dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(resimDosyasi.FileName);

                    // Klasör yolunu belirliyoruz
                    var yol = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", dosyaAdi);

                    // Dosyayı klasöre kopyalıyoruz
                    using (var stream = new FileStream(yol, FileMode.Create))
                    {
                        await resimDosyasi.CopyToAsync(stream);
                    }

                    // Veritabanına ismini yazıyoruz
                    urun.ResimYolu = dosyaAdi;
                }
                _context.Add(urun); // Veriyi veritabanı sırasına al
                await _context.SaveChangesAsync(); // SQL Server'a kalıcı olarak kaydet
                return RedirectToAction(nameof(Index)); // Kayıt bitince ürün listesine geri dön
            }
            return View(urun);
        }

        // --- 

        // --- TAM KAPSAMLI ADMİN PANELİ: DÜZENLEME (EDIT) İŞLEMLERİ ---
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
                    // Yeni bir resim seçildiyse:
                    var dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(resimDosyasi.FileName);
                    var yol = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", dosyaAdi);

                    using (var stream = new FileStream(yol, FileMode.Create))
                    {
                        await resimDosyasi.CopyToAsync(stream);
                    }

                    // Yeni resmin adını modele veriyoruz
                    urun.ResimYolu = dosyaAdi;
                }
                _context.Update(urun);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); // İşlem bitince listeye dön
            }
            return View(urun);
        }

        // --- TAM KAPSAMLI ADMİN PANELİ: SİLME (DELETE) İŞLEMLERİ ---

        // 1. Silme onay sayfasını açar (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var urun = await _context.Urunler.FindAsync(id);
            if (urun == null) return NotFound();
            return View(urun);
        }

        // 2. Sayfadaki "Sil" butonuna basınca asıl silmeyi yapan kod (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var urun = await _context.Urunler.FindAsync(id);
            if (urun != null)
            {
                _context.Urunler.Remove(urun);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index)); // Silme bitince listeye dön
        }


    }
}