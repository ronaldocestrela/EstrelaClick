using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstrelaClick.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarReservaPlacaEEstadoEmGravacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataReservaUtc",
                schema: "estrelaclick",
                table: "Placas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperadorReservaId",
                schema: "estrelaclick",
                table: "Placas",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Placas_FilaPendentes",
                schema: "estrelaclick",
                table: "Placas",
                columns: new[] { "StatusPlaca", "NumeroLote", "DataCriacaoUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Placas_FilaPendentes",
                schema: "estrelaclick",
                table: "Placas");

            migrationBuilder.DropColumn(
                name: "DataReservaUtc",
                schema: "estrelaclick",
                table: "Placas");

            migrationBuilder.DropColumn(
                name: "OperadorReservaId",
                schema: "estrelaclick",
                table: "Placas");
        }
    }
}
