using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraWat.Models
{
    [Table("Urunler")]
    public class Urun
    {
        [Key]
        public int Id { get; set; } // Veritabanında her ürünün benzersiz kimliği

        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Ürün adı en fazla {1} karakter olabilir.")]
        [Display(Name = "Ürün Adı")]
        public string Ad { get; set; } // Ruj, Şampuan vb.

        [Required(ErrorMessage = "Kategori zorunludur.")]
        [StringLength(50, ErrorMessage = "Kategori adı en fazla {1} karakter olabilir.")]
        [Display(Name = "Kategori")]
        public string Kategori { get; set; } // Cilt Bakımı, Makyaj vb.

        [Required(ErrorMessage = "Fiyat alanı zorunludur.")]
        [Range(0.01, 1000000.00, ErrorMessage = "Lütfen geçerli bir fiyat giriniz (0.01 - 1.000.000 TL).")]
        [Display(Name = "Fiyat")]
        public decimal Fiyat { get; set; }

        [Required(ErrorMessage = "Stok adedi zorunludur.")]
        [Range(0, 1000000, ErrorMessage = "Stok adedi 0 ile 1.000.000 arasında olmalıdır.")]
        [Display(Name = "Stok Adedi")]
        public int StokAdedi { get; set; }

        public string? ResimYolu { get; set; }
    }
}