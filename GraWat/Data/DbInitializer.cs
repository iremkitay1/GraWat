using GraWat.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraWat.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(GraWatContext grawatContext, ApplicationDbContext identityContext)
        {
            // Veritabanını otomatik olarak oluşturup migrate edelim (Garanti olsun)
            await grawatContext.Database.MigrateAsync();
            await identityContext.Database.MigrateAsync();

            // İlişkilerden dolayı yabancı anahtar hatası almamak için 
            // eski verileri temizleme sırası:
            
            // 1. Favorileri temizle
            var favoriler = await grawatContext.Favoriler.ToListAsync();
            if (favoriler.Count > 0)
            {
                grawatContext.Favoriler.RemoveRange(favoriler);
                await grawatContext.SaveChangesAsync();
            }

            var favorilerIdentity = await identityContext.Favoriler.ToListAsync();
            if (favorilerIdentity.Count > 0)
            {
                identityContext.Favoriler.RemoveRange(favorilerIdentity);
                await identityContext.SaveChangesAsync();
            }

            // 2. Sipariş Kalemlerini temizle
            var siparisKalemleri = await grawatContext.SiparisKalemleri.ToListAsync();
            if (siparisKalemleri.Count > 0)
            {
                grawatContext.SiparisKalemleri.RemoveRange(siparisKalemleri);
                await grawatContext.SaveChangesAsync();
            }

            // 3. Siparişleri temizle
            var siparisler = await grawatContext.Siparisler.ToListAsync();
            if (siparisler.Count > 0)
            {
                grawatContext.Siparisler.RemoveRange(siparisler);
                await grawatContext.SaveChangesAsync();
            }

            // 4. Sepet Elemanlarını temizle
            var sepetItems = await identityContext.SepetItems.ToListAsync();
            if (sepetItems.Count > 0)
            {
                identityContext.SepetItems.RemoveRange(sepetItems);
                await identityContext.SaveChangesAsync();
            }

            // 5. Yorumları temizle (Eğer eski dummy ürün ID'lerine bağlıysa)
            var yorumlar = await grawatContext.Yorumlar.ToListAsync();
            if (yorumlar.Count > 0)
            {
                grawatContext.Yorumlar.RemoveRange(yorumlar);
                await grawatContext.SaveChangesAsync();
            }

            // 6. Eski dummy ürünleri temizle
            var urunler = await grawatContext.Urunler.ToListAsync();
            if (urunler.Count > 0)
            {
                grawatContext.Urunler.RemoveRange(urunler);
                await grawatContext.SaveChangesAsync();
            }

            // Yeni Profesyonel Ürünleri Ekle
            var yeniUrunler = new List<Urun>
            {
                // === 1. Makyaj ===
                new Urun { Ad = "Mat Bitişli Kadife Likit Ruj", Kategori = "Makyaj", Fiyat = 249.90m, StokAdedi = 100, ResimYolu = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "Suya Dayanıklı Yoğun Hacim Verici Maskara", Kategori = "Makyaj", Fiyat = 299.90m, StokAdedi = 85, ResimYolu = "https://images.unsplash.com/photo-1596462502278-27bfdc403348?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "Aydınlatıcı Etkili Gözenek Kapatıcı Fondöten", Kategori = "Makyaj", Fiyat = 420.00m, StokAdedi = 50, ResimYolu = "https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?auto=format&fit=crop&w=600&q=80" },

                // === 2. Cilt Bakım ===
                new Urun { Ad = "Hyalüronik Asitli Nemlendirici Yüz Kremi", Kategori = "Cilt Bakım", Fiyat = 350.00m, StokAdedi = 120, ResimYolu = "https://images.unsplash.com/photo-1608248597279-f99d160bfcbc?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "C Vitamini Aydınlatıcı Gözenek Sıkılaştırıcı Serum", Kategori = "Cilt Bakım", Fiyat = 480.00m, StokAdedi = 60, ResimYolu = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "Salisilik Asitli Gözenek Arındırıcı Temizleme Jeli", Kategori = "Cilt Bakım", Fiyat = 189.90m, StokAdedi = 150, ResimYolu = "https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=600&q=80" },

                // === 3. Güneş Ürünleri ===
                new Urun { Ad = "SPF 50+ Leke Karşıtı Koruyucu Güneş Kremi", Kategori = "Güneş Ürünleri", Fiyat = 389.90m, StokAdedi = 90, ResimYolu = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "Yoğun Nemlendirici Güneş Sonrası Yatıştırıcı Jel", Kategori = "Güneş Ürünleri", Fiyat = 199.90m, StokAdedi = 110, ResimYolu = "https://images.unsplash.com/photo-1526947425960-945c6e72858f?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "Doğal Işıltı Veren Hızlı Bronzlaştırıcı Kakao Yağı", Kategori = "Güneş Ürünleri", Fiyat = 285.00m, StokAdedi = 75, ResimYolu = "https://images.unsplash.com/photo-1601049541289-9b1b7bbbfe19?auto=format&fit=crop&w=600&q=80" },

                // === 4. Saç Bakım ===
                new Urun { Ad = "Keratinli Onarıcı Yoğun Saç Maskesi", Kategori = "Saç Bakım", Fiyat = 220.00m, StokAdedi = 140, ResimYolu = "https://images.unsplash.com/photo-1527799851257-35939e0951a4?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "Dökülme Karşıtı Güçlendirici Kafein Şampuanı", Kategori = "Saç Bakım", Fiyat = 185.00m, StokAdedi = 200, ResimYolu = "https://images.unsplash.com/photo-1535585209827-a15fcdbc4c2d?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "Kolajenli Yoğun Nem Verici Saç Kremi", Kategori = "Saç Bakım", Fiyat = 175.90m, StokAdedi = 160, ResimYolu = "https://images.unsplash.com/photo-1519735008726-199715de0a63?auto=format&fit=crop&w=600&q=80" },

                // === 5. Parfüm & Deodorant ===
                new Urun { Ad = "Amber ve Vanilya Notalı Kadın Parfümü EDP 100ml", Kategori = "Parfüm/Deodorant", Fiyat = 980.00m, StokAdedi = 40, ResimYolu = "https://images.unsplash.com/photo-1541643600914-78b084683601?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "Odunsu ve Baharatlı Erkek Parfümü EDP 100ml", Kategori = "Parfüm/Deodorant", Fiyat = 980.00m, StokAdedi = 35, ResimYolu = "https://images.unsplash.com/photo-1594035910387-fea47794261f?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "Koltuk Altı Kararması Karşıtı 48 Saat Etkili Roll-on", Kategori = "Parfüm/Deodorant", Fiyat = 115.00m, StokAdedi = 180, ResimYolu = "https://images.unsplash.com/photo-1616949755610-8c9bbc08f138?auto=format&fit=crop&w=600&q=80" },

                // === 6. Erkek Bakım ===
                new Urun { Ad = "Yatıştırıcı Aloe Vera Özlü Tıraş Sonrası Balsam", Kategori = "Erkek Bakım", Fiyat = 169.90m, StokAdedi = 120, ResimYolu = "https://images.unsplash.com/photo-1617897903246-719242758050?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "Doğal Yağlar İçeren Parlatıcı Sakal Bakım Yağı", Kategori = "Erkek Bakım", Fiyat = 185.00m, StokAdedi = 80, ResimYolu = "https://images.unsplash.com/photo-1503951914875-452162b0f3f1?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "Nem Dengeleyici Matlaştırıcı Erkek Yüz Kremi", Kategori = "Erkek Bakım", Fiyat = 245.00m, StokAdedi = 95, ResimYolu = "https://images.unsplash.com/photo-1515377905703-c4788e51af15?auto=format&fit=crop&w=600&q=80" },

                // === 7. Kişisel Bakım ===
                new Urun { Ad = "Beyazlatıcı Kömür Özlü Doğal Ağız ve Diş Macunu", Kategori = "Kişisel Bakım", Fiyat = 95.00m, StokAdedi = 250, ResimYolu = "https://images.unsplash.com/photo-1559599101-f09722fb4948?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "Egzotik Mango Kokulu Nemlendirici Duş Jeli 500ml", Kategori = "Kişisel Bakım", Fiyat = 89.90m, StokAdedi = 300, ResimYolu = "https://images.unsplash.com/photo-1607006342411-9a3363b63925?auto=format&fit=crop&w=600&q=80" },
                new Urun { Ad = "Hassas Ciltler İçin Organik Pamuklu Günlük Hijyenik Ped", Kategori = "Kişisel Bakım", Fiyat = 79.90m, StokAdedi = 220, ResimYolu = "https://images.unsplash.com/photo-1583947215259-38e31be8751f?auto=format&fit=crop&w=600&q=80" }
            };

            await grawatContext.Urunler.AddRangeAsync(yeniUrunler);
            await grawatContext.SaveChangesAsync();
        }
    }
}
