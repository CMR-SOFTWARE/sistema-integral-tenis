using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class DesafiosDeDobles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "OrdenInscripcionRankingDobles");

            migrationBuilder.CreateTable(
                name: "JuegosDoblesPendientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Jugador1Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Jugador2Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Rival1Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Rival2Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadoPorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    CreadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AceptadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GanoParejaA = table.Column<bool>(type: "boolean", nullable: true),
                    PuntosGanadores = table.Column<int>(type: "integer", nullable: true),
                    PuntosPerdedores = table.Column<int>(type: "integer", nullable: true),
                    FinalizadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JuegosDoblesPendientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JugadoresRankingDobles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JugadorRankingId = table.Column<Guid>(type: "uuid", nullable: false),
                    PuntosProvisionales = table.Column<int>(type: "integer", nullable: false),
                    PosicionProvisional = table.Column<int>(type: "integer", nullable: true),
                    RangoProvisional = table.Column<string>(type: "text", nullable: true),
                    CfProvisional = table.Column<int>(type: "integer", nullable: true),
                    OrdenInscripcion = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('\"OrdenInscripcionRankingDobles\"')"),
                    MejorPuestoHistorico = table.Column<int>(type: "integer", nullable: true),
                    FechaMejorPuesto = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    InscriptoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JugadoresRankingDobles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PuntosMovimientosDobles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JugadorRankingDoblesId = table.Column<Guid>(type: "uuid", nullable: false),
                    Puntos = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuntosMovimientosDobles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JugadoresRankingDobles_JugadorRankingId",
                table: "JugadoresRankingDobles",
                column: "JugadorRankingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JugadoresRankingDobles_PuntosProvisionales",
                table: "JugadoresRankingDobles",
                column: "PuntosProvisionales");

            migrationBuilder.CreateIndex(
                name: "IX_PuntosMovimientosDobles_JugadorRankingDoblesId_Fecha",
                table: "PuntosMovimientosDobles",
                columns: new[] { "JugadorRankingDoblesId", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JuegosDoblesPendientes");

            migrationBuilder.DropTable(
                name: "JugadoresRankingDobles");

            migrationBuilder.DropTable(
                name: "PuntosMovimientosDobles");

            migrationBuilder.DropSequence(
                name: "OrdenInscripcionRankingDobles");
        }
    }
}
