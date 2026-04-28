using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GraWat.Models; // Urun modelini tanıyabilmesi için

namespace GraWat.Data
{
    // Kimlik sistemi için IdentityDbContext'ten miras alıyoruz
    public class ApplicationDbContext : IdentityDbContext
    {
        // İşte Program.cs'den gelen SQL Server ayarlarını içeri alan o eksik kapı (Constructor)
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Veritabanındaki ürünler tablomuz
        public DbSet<Urun> Urunler { get; set; }
        public DbSet<SepetItem> SepetItems { get; set; }

    }
}