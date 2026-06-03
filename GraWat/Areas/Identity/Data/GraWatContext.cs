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
        }
    }
}