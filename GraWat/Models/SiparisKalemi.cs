using System.ComponentModel.DataAnnotations;

namespace GraWat.Models
{
    public class SiparisKalemi
    {
        public int Id { get; set; }
        public int SiparisId { get; set; }
        public int UrunId { get; set; }
        public int Adet { get; set; }
        public decimal Fiyat { get; set; }

        // İlişkiler
        public virtual Siparis? Siparis { get; set; }
        public virtual Urun? Urun { get; set; }
    }
}