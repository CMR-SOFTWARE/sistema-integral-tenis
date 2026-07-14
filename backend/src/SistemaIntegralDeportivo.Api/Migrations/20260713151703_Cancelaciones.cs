using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class Cancelaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanceladoPor",
                table: "Turnos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelacionMotivo",
                table: "TurnoParticipantes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CanceloEl",
                table: "TurnoParticipantes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanceladoPor",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "CancelacionMotivo",
                table: "TurnoParticipantes");

            migrationBuilder.DropColumn(
                name: "CanceloEl",
                table: "TurnoParticipantes");
        }
    }
}
