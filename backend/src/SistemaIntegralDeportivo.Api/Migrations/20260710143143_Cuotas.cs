using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class Cuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ValorClaseIndividual",
                table: "Tenants",
                type: "TEXT",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorHoraGrupal",
                table: "Tenants",
                type: "TEXT",
                precision: 12,
                scale: 2,
                nullable: true);

            // Ajustado a mano: el scaffold puso defaultValue "" (string vacío),
            // que rompería la lectura del enum en los alumnos existentes.
            // Todos los alumnos previos a esta migración liquidan Mensual.
            migrationBuilder.AddColumn<string>(
                name: "Modalidad",
                table: "Alumnos",
                type: "TEXT",
                nullable: false,
                defaultValue: "Mensual");

            migrationBuilder.CreateTable(
                name: "Cargos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AlumnoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Concepto = table.Column<string>(type: "TEXT", nullable: false),
                    Monto = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TurnoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PagadoEl = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MedioPago = table.Column<string>(type: "TEXT", nullable: true),
                    CreadoEl = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cargos_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cargos_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cargos_Turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "ValorClaseIndividual", "ValorHoraGrupal" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_AlumnoId",
                table: "Cargos",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_TenantId_AlumnoId_Fecha",
                table: "Cargos",
                columns: new[] { "TenantId", "AlumnoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_TurnoId_AlumnoId",
                table: "Cargos",
                columns: new[] { "TurnoId", "AlumnoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cargos");

            migrationBuilder.DropColumn(
                name: "ValorClaseIndividual",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ValorHoraGrupal",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Modalidad",
                table: "Alumnos");
        }
    }
}
