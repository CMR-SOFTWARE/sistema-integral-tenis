using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class RankingBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "OrdenInscripcionRanking");

            migrationBuilder.CreateTable(
                name: "JugadoresRanking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sexo = table.Column<string>(type: "text", nullable: true),
                    CiudadResidencia = table.Column<string>(type: "text", nullable: true),
                    Provincia = table.Column<string>(type: "text", nullable: true),
                    Pais = table.Column<string>(type: "text", nullable: true),
                    Licencia = table.Column<string>(type: "text", nullable: true),
                    Mano = table.Column<string>(type: "text", nullable: true),
                    Reves = table.Column<string>(type: "text", nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    PerfilPublico = table.Column<bool>(type: "boolean", nullable: false),
                    PermiteContacto = table.Column<bool>(type: "boolean", nullable: false),
                    PuntosProvisionales = table.Column<int>(type: "integer", nullable: false),
                    PosicionProvisional = table.Column<int>(type: "integer", nullable: true),
                    RangoProvisional = table.Column<string>(type: "text", nullable: true),
                    CfProvisional = table.Column<int>(type: "integer", nullable: true),
                    OrdenInscripcion = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('\"OrdenInscripcionRanking\"')"),
                    MejorPuestoHistorico = table.Column<int>(type: "integer", nullable: true),
                    FechaMejorPuesto = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    InscriptoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JugadoresRanking", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinatarioUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Mensaje = table.Column<string>(type: "text", nullable: false),
                    EntidadId = table.Column<Guid>(type: "uuid", nullable: true),
                    Leida = table.Column<bool>(type: "boolean", nullable: false),
                    CreadaEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JugadoresRanking_PuntosProvisionales",
                table: "JugadoresRanking",
                column: "PuntosProvisionales");

            migrationBuilder.CreateIndex(
                name: "IX_JugadoresRanking_UsuarioId",
                table: "JugadoresRanking",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_DestinatarioUserId_Leida",
                table: "Notificaciones",
                columns: new[] { "DestinatarioUserId", "Leida" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JugadoresRanking");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropSequence(
                name: "OrdenInscripcionRanking");
        }
    }
}
