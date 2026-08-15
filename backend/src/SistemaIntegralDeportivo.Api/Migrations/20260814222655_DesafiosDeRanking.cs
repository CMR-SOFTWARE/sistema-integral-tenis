using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class DesafiosDeRanking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JuegosPendientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Jugador1Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Jugador2Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JugadorMenorId = table.Column<Guid>(type: "uuid", nullable: false),
                    JugadorMayorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadoPorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    CreadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AceptadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GanadorId = table.Column<Guid>(type: "uuid", nullable: true),
                    PuntosGanador = table.Column<int>(type: "integer", nullable: true),
                    PuntosPerdedor = table.Column<int>(type: "integer", nullable: true),
                    FinalizadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JuegosPendientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PuntosMovimientos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JugadorRankingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Puntos = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuntosMovimientos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JuegosPendientes_JugadorMenorId_JugadorMayorId",
                table: "JuegosPendientes",
                columns: new[] { "JugadorMenorId", "JugadorMayorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PuntosMovimientos_JugadorRankingId_Fecha",
                table: "PuntosMovimientos",
                columns: new[] { "JugadorRankingId", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JuegosPendientes");

            migrationBuilder.DropTable(
                name: "PuntosMovimientos");
        }
    }
}
