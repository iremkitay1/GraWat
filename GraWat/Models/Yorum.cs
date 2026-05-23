using System.ComponentModel.DataAnnotations;

namespace GraWat.Models
{
    public class Yorum
    {
        public int Id { get; set; }

        // Hangi ürüne yapıldı?
        public int UrunId { get; set; }

        // Kim yaptı?
        public string KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } // Ekranda göstermek için

        [Required(ErrorMessage = "Lütfen bir puan verin.")]
        [Range(1, 5)]
        public int Puan { get; set; } // 1 ile 5 arası yıldız

        [Required(ErrorMessage = "Lütfen yorumunuzu yazın.")]
        public string YorumMetni { get; set; }

        public DateTime Tarih { get; set; } = DateTime.Now;
    }
}