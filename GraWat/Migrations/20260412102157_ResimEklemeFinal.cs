using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraWat.Migrations
{
    /// <inheritdoc />
    public partial class ResimEklemeFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResimYolu",
                table: "Urunler",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResimYolu",
                table: "Urunler");
        }
    }
}
