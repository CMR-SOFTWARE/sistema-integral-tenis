using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class SolicitudesYCredenciales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DebeCambiarPassword",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Solicitudes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    Mensaje = table.Column<string>(type: "TEXT", nullable: true),
                    AlumnoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreadoEl = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResueltoEl = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitudes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_TenantId_Estado",
                table: "Solicitudes",
                columns: new[] { "TenantId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_UserId_TenantId",
                table: "Solicitudes",
                columns: new[] { "UserId", "TenantId" },
                unique: true,
                filter: "Estado = 'Pendiente'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "DebeCambiarPassword",
                table: "AspNetUsers");
        }
    }
}
