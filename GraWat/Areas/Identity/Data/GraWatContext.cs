using Microsoft.EntityFrameworkCore;
using GraWat.Models;

namespace GraWat.Data
{
    public class GraWatContext : DbContext
    {
        public GraWatContext(DbContextOptions<GraWatContext> options) : base(options) { }

        public DbSet<Urun> Urunler { get; set; }
        public DbSet<Yorum> Yorumlar { get; set; }
        public DbSet<Siparis> Siparisler { get; set; }
        public DbSet<SiparisKalemi> SiparisKalemleri { get; set; }
        public DbSet<Favoriler> Favoriler { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Urun>().HasData(
                // Makyaj (IDs: 101, 102, 103)
                new Urun { Id = 101, Ad = "Mat Bitişli Kadife Ruj", Kategori = "Makyaj", Fiyat = 249.90m, StokAdedi = 100, ResimYolu = "https://picsum.photos/seed/ruj1/400/400" },
                new Urun { Id = 102, Ad = "Suya Dayanıklı Siyah Maskara", Kategori = "Makyaj", Fiyat = 299.90m, StokAdedi = 80, ResimYolu = "https://picsum.photos/seed/maskara2/400/400" },
                new Urun { Id = 103, Ad = "Aydınlatıcı Etkili Likit Fondöten", Kategori = "Makyaj", Fiyat = 420.00m, StokAdedi = 50, ResimYolu = "https://picsum.photos/seed/fondoten3/400/400" },

                // Cilt Bakım (IDs: 104, 105, 106)
                new Urun { Id = 104, Ad = "Nemlendirici Yüz Kremi (Hyalüronik Asit)", Kategori = "Cilt Bakım", Fiyat = 350.00m, StokAdedi = 120, ResimYolu = "https://picsum.photos/seed/krem4/400/400" },
                new Urun { Id = 105, Ad = "C Vitamini Aydınlatıcı Serum", Kategori = "Cilt Bakım", Fiyat = 480.00m, StokAdedi = 60, ResimYolu = "https://picsum.photos/seed/serum5/400/400" },
                new Urun { Id = 106, Ad = "Nazik Arındırıcı Yüz Temizleme Jeli", Kategori = "Cilt Bakım", Fiyat = 189.90m, StokAdedi = 150, ResimYolu = "https://picsum.photos/seed/jel6/400/400" },

                // Parfüm/Deodorant (IDs: 107, 108, 109)
                new Urun { Id = 107, Ad = "Odunsu ve Baharatlı Erkek Parfümü", Kategori = "Parfüm/Deodorant", Fiyat = 850.00m, StokAdedi = 30, ResimYolu = "https://picsum.photos/seed/parfum7/400/400" },
                new Urun { Id = 108, Ad = "Çiçeksi ve Meyveli Kadın Parfümü", Kategori = "Parfüm/Deodorant", Fiyat = 890.00m, StokAdedi = 40, ResimYolu = "https://picsum.photos/seed/parfum8/400/400" },
                new Urun { Id = 109, Ad = "Uzun Süre Etkili Ferah Deodorant", Kategori = "Parfüm/Deodorant", Fiyat = 110.00m, StokAdedi = 200, ResimYolu = "https://picsum.photos/seed/deodorant9/400/400" }
            );
        }
    }
}