using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstrelaClick.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarFiltroSatisfacaoPlaca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FiltroSatisfacaoAtivo",
                schema: "estrelaclick",
                table: "Placas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UrlSacInterno",
                schema: "estrelaclick",
                table: "Placas",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiltroSatisfacaoAtivo",
                schema: "estrelaclick",
                table: "Placas");

            migrationBuilder.DropColumn(
                name: "UrlSacInterno",
                schema: "estrelaclick",
                table: "Placas");
        }
    }
}
