using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class SueldosEmpleados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ValorHora",
                table: "MembresiasTenant",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorHoraProfe",
                table: "Horarios",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PagosEmpleado",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Anio = table.Column<int>(type: "integer", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    MedioPago = table.Column<string>(type: "text", nullable: false),
                    PagadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosEmpleado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosEmpleado_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagosEmpleado_TenantId_UserId_Anio_Mes",
                table: "PagosEmpleado",
                columns: new[] { "TenantId", "UserId", "Anio", "Mes" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagosEmpleado");

            migrationBuilder.DropColumn(
                name: "ValorHora",
                table: "MembresiasTenant");

            migrationBuilder.DropColumn(
                name: "ValorHoraProfe",
                table: "Horarios");
        }
    }
}
