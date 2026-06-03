using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GraWat.Models
{
    [Table("Siparisler")]
    public class Siparis
    {
        [Key]
        public int Id { get; set; }

        // Müşterinin kim olduğunu bilmemiz lazım
        [Required]
        public string KullaniciId { get; set; }

        public DateTime SiparisTarihi { get; set; } = DateTime.Now;

        [Required]
        public decimal ToplamTutar { get; set; }

        // Varsayılan olarak sipariş verildiğinde "Hazırlanıyor" diyoruz
        public string Durum { get; set; } = "Hazırlanıyor";

        public string OdemeYontemi { get; set; }

        // =================================================================
        // NESNE YÖNELİMLİ İLİŞKİ: Bire-Çok (One-to-Many) İlişki Alanı
        // Bir siparişin içinde birden fazla ürün (SiparisKalemi) yer alabilir.
        // =================================================================
        public List<SiparisKalemi> SiparisKalemleri { get; set; } = new List<SiparisKalemi>();
    }
}