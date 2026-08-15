using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class RankingOficialYRevisiones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JuegosRevision",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JuegoPendienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    JuegoDoblesPendienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreadoPorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comentario = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    RespuestaAdmin = table.Column<string>(type: "text", nullable: true),
                    ResueltoPorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResueltoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JuegosRevision", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RankingSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Modalidad = table.Column<string>(type: "text", nullable: false),
                    JugadorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Posicion = table.Column<int>(type: "integer", nullable: false),
                    Puntos = table.Column<int>(type: "integer", nullable: false),
                    Rango = table.Column<string>(type: "text", nullable: false),
                    Cf = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<string>(type: "text", nullable: false),
                    ScopeValor = table.Column<string>(type: "text", nullable: true),
                    FechaCorte = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankingSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JuegosRevision_JuegoPendienteId_JuegoDoblesPendienteId_Esta~",
                table: "JuegosRevision",
                columns: new[] { "JuegoPendienteId", "JuegoDoblesPendienteId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_RankingSnapshots_Modalidad_Scope_ScopeValor_FechaCorte",
                table: "RankingSnapshots",
                columns: new[] { "Modalidad", "Scope", "ScopeValor", "FechaCorte" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JuegosRevision");

            migrationBuilder.DropTable(
                name: "RankingSnapshots");
        }
    }
}
