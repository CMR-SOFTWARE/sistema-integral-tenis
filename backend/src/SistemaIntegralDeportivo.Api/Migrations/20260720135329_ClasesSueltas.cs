using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class ClasesSueltas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_Horarios_HorarioId",
                table: "Turnos");

            migrationBuilder.AlterColumn<Guid>(
                name: "HorarioId",
                table: "Turnos",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "ClasesSueltas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AlumnoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SedeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    CreadoEl = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResueltoEl = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CargoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CanchaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TurnoId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClasesSueltas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClasesSueltas_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClasesSueltas_Canchas_CanchaId",
                        column: x => x.CanchaId,
                        principalTable: "Canchas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ClasesSueltas_Cargos_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ClasesSueltas_Sedes_SedeId",
                        column: x => x.SedeId,
                        principalTable: "Sedes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClasesSueltas_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClasesSueltas_Turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClasesSueltas_AlumnoId",
                table: "ClasesSueltas",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_ClasesSueltas_CanchaId",
                table: "ClasesSueltas",
                column: "CanchaId");

            migrationBuilder.CreateIndex(
                name: "IX_ClasesSueltas_CargoId",
                table: "ClasesSueltas",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_ClasesSueltas_SedeId",
                table: "ClasesSueltas",
                column: "SedeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClasesSueltas_TenantId_Estado",
                table: "ClasesSueltas",
                columns: new[] { "TenantId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_ClasesSueltas_TurnoId",
                table: "ClasesSueltas",
                column: "TurnoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_Horarios_HorarioId",
                table: "Turnos",
                column: "HorarioId",
                principalTable: "Horarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_Horarios_HorarioId",
                table: "Turnos");

            migrationBuilder.DropTable(
                name: "ClasesSueltas");

            migrationBuilder.AlterColumn<Guid>(
                name: "HorarioId",
                table: "Turnos",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_Horarios_HorarioId",
                table: "Turnos",
                column: "HorarioId",
                principalTable: "Horarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
