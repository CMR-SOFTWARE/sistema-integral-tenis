using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class SolicitudesHorario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesHorario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AlumnoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dia = table.Column<string>(type: "TEXT", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    CreadoEl = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResueltoEl = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CanchaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    HorarioId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesHorario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesHorario_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitudesHorario_Canchas_CanchaId",
                        column: x => x.CanchaId,
                        principalTable: "Canchas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SolicitudesHorario_Horarios_HorarioId",
                        column: x => x.HorarioId,
                        principalTable: "Horarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SolicitudesHorario_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesHorario_AlumnoId",
                table: "SolicitudesHorario",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesHorario_CanchaId",
                table: "SolicitudesHorario",
                column: "CanchaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesHorario_HorarioId",
                table: "SolicitudesHorario",
                column: "HorarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesHorario_TenantId_Estado",
                table: "SolicitudesHorario",
                columns: new[] { "TenantId", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesHorario");
        }
    }
}
