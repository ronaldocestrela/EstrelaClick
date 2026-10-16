using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstrelaClick.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarNumeroLotePlaca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NumeroLote",
                schema: "estrelaclick",
                table: "Placas",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "PADRAO");

            migrationBuilder.CreateIndex(
                name: "IX_Placas_NumeroLote",
                schema: "estrelaclick",
                table: "Placas",
                column: "NumeroLote");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Placas_NumeroLote",
                schema: "estrelaclick",
                table: "Placas");

            migrationBuilder.DropColumn(
                name: "NumeroLote",
                schema: "estrelaclick",
                table: "Placas");
        }
    }
}
