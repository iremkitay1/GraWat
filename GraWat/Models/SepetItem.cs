using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraWat.Models
{
    [Table("SepetItems")]
    public class SepetItem
    {
        public int Id { get; set; }
        public int UrunId { get; set; }
        public Urun Urun { get; set; } // Ürünle bağlantı kuruyoruz
        public string KullaniciId { get; set; } // Giriş yapan kullanıcıyı tanımak için
        public int Adet { get; set; }
    }
}