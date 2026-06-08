using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GraWat.Models;

namespace GraWat.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SepetItem> SepetItems { get; set; }
        public DbSet<Favoriler> Favoriler { get; set; } // Yeni favoriler tablomuz
        public DbSet<Urun> Urunler { get; set; }
        public DbSet<Yorum> Yorumlar { get; set; }
        public DbSet<Siparis> Siparisler { get; set; }
        public DbSet<SiparisKalemi> SiparisKalemleri { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}