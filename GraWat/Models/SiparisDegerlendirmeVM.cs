using System.Collections.Generic;

namespace GraWat.Models
{
    public class SiparisDegerlendirmeVM
    {
        public int SiparisId { get; set; }
        public List<UrunYorumItem> Urunler { get; set; } = new List<UrunYorumItem>();
    }

    public class UrunYorumItem
    {
        public int UrunId { get; set; }
        public string UrunAd { get; set; }
        public string Kategori { get; set; }
        public int Puan { get; set; }
        public string YorumMetni { get; set; }
    }
}