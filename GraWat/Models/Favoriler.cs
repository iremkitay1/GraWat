namespace GraWat.Models
{
    public class Favoriler
    {
        public int Id { get; set; }
        public int UrunId { get; set; }
        public Urun Urun { get; set; }
        public string KullaniciId { get; set; }
    }
}