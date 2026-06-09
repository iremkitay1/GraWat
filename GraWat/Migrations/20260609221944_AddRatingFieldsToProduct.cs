using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GraWat.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingFieldsToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Urunler",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Urunler",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Urunler",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Urunler",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Urunler",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "Urunler",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "Urunler",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "Urunler",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "Urunler",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.AlterColumn<string>(
                name: "Kategori",
                table: "Urunler",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Ad",
                table: "Urunler",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "Urunler",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TotalReviews",
                table: "Urunler",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Urunler");

            migrationBuilder.DropColumn(
                name: "TotalReviews",
                table: "Urunler");

            migrationBuilder.AlterColumn<string>(
                name: "Kategori",
                table: "Urunler",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Ad",
                table: "Urunler",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.InsertData(
                table: "Urunler",
                columns: new[] { "Id", "Ad", "Fiyat", "Kategori", "ResimYolu", "StokAdedi" },
                values: new object[,]
                {
                    { 101, "Mat Bitişli Kadife Ruj", 249.90m, "Makyaj", "https://picsum.photos/seed/ruj1/400/400", 100 },
                    { 102, "Suya Dayanıklı Siyah Maskara", 299.90m, "Makyaj", "https://picsum.photos/seed/maskara2/400/400", 80 },
                    { 103, "Aydınlatıcı Etkili Likit Fondöten", 420.00m, "Makyaj", "https://picsum.photos/seed/fondoten3/400/400", 50 },
                    { 104, "Nemlendirici Yüz Kremi (Hyalüronik Asit)", 350.00m, "Cilt Bakım", "https://picsum.photos/seed/krem4/400/400", 120 },
                    { 105, "C Vitamini Aydınlatıcı Serum", 480.00m, "Cilt Bakım", "https://picsum.photos/seed/serum5/400/400", 60 },
                    { 106, "Nazik Arındırıcı Yüz Temizleme Jeli", 189.90m, "Cilt Bakım", "https://picsum.photos/seed/jel6/400/400", 150 },
                    { 107, "Odunsu ve Baharatlı Erkek Parfümü", 850.00m, "Parfüm/Deodorant", "https://picsum.photos/seed/parfum7/400/400", 30 },
                    { 108, "Çiçeksi ve Meyveli Kadın Parfümü", 890.00m, "Parfüm/Deodorant", "https://picsum.photos/seed/parfum8/400/400", 40 },
                    { 109, "Uzun Süre Etkili Ferah Deodorant", 110.00m, "Parfüm/Deodorant", "https://picsum.photos/seed/deodorant9/400/400", 200 }
                });
        }
    }
}
