using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraWat.Models
{
    [Table("Urunler")]
    public class Urun
    {
        public int Id { get; set; } // Veritabanında her ürünün benzersiz kimliği
        public string Ad { get; set; } // Ruj, Şampuan vb.
        public string Kategori { get; set; } // Cilt Bakımı, Makyaj vb.
        public decimal Fiyat { get; set; }
        public int StokAdedi { get; set; }
        public string? ResimYolu { get; set; }
    }
}