using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class MembresiaTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MembresiasTenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Rol = table.Column<string>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEl = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembresiasTenant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembresiasTenant_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MembresiasTenant_TenantId",
                table: "MembresiasTenant",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MembresiasTenant_TenantId_UserId",
                table: "MembresiasTenant",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembresiasTenant_UserId",
                table: "MembresiasTenant",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MembresiasTenant");
        }
    }
}
