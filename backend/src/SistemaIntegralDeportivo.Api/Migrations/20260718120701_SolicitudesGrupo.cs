using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class SolicitudesGrupo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesGrupo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AlumnoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GrupoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    CreadoEl = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResueltoEl = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesGrupo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesGrupo_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitudesGrupo_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitudesGrupo_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesGrupo_AlumnoId",
                table: "SolicitudesGrupo",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesGrupo_GrupoId",
                table: "SolicitudesGrupo",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesGrupo_TenantId_Estado",
                table: "SolicitudesGrupo",
                columns: new[] { "TenantId", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesGrupo");
        }
    }
}
