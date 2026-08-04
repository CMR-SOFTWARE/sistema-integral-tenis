using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class PerfilPublicoProfesor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerfilesProfesor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titular = table.Column<string>(type: "text", nullable: true),
                    Subtitulo = table.Column<string>(type: "text", nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    Especialidades = table.Column<List<string>>(type: "text[]", nullable: false),
                    PortadaUrl = table.Column<string>(type: "text", nullable: true),
                    PortadaRuta = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    AvatarRuta = table.Column<string>(type: "text", nullable: true),
                    Publicado = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilesProfesor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfilesProfesor_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FotosPerfil",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilProfesorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Ruta = table.Column<string>(type: "text", nullable: false),
                    PieDeFoto = table.Column<string>(type: "text", nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    CreadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotosPerfil", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FotosPerfil_PerfilesProfesor_PerfilProfesorId",
                        column: x => x.PerfilProfesorId,
                        principalTable: "PerfilesProfesor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HitosTrayectoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilProfesorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Anio = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Detalle = table.Column<string>(type: "text", nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HitosTrayectoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HitosTrayectoria_PerfilesProfesor_PerfilProfesorId",
                        column: x => x.PerfilProfesorId,
                        principalTable: "PerfilesProfesor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FotosPerfil_PerfilProfesorId",
                table: "FotosPerfil",
                column: "PerfilProfesorId");

            migrationBuilder.CreateIndex(
                name: "IX_HitosTrayectoria_PerfilProfesorId",
                table: "HitosTrayectoria",
                column: "PerfilProfesorId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesProfesor_TenantId_Publicado",
                table: "PerfilesProfesor",
                columns: new[] { "TenantId", "Publicado" });

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesProfesor_TenantId_UserId",
                table: "PerfilesProfesor",
                columns: new[] { "TenantId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FotosPerfil");

            migrationBuilder.DropTable(
                name: "HitosTrayectoria");

            migrationBuilder.DropTable(
                name: "PerfilesProfesor");
        }
    }
}
