using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class SolicitudHorarioSede : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La SEDE ahora es obligatoria en el pedido. Las solicitudes viejas
            // (creadas antes de esta columna) no tienen sede válida y romperían
            // la FK al reconstruir la tabla en SQLite. Son pedidos individuales
            // pendientes/históricos sin sede: se descartan (rama sin mergear;
            // en prod la tabla todavía no tiene datos).
            migrationBuilder.Sql("DELETE FROM \"SolicitudesHorario\";");

            migrationBuilder.AddColumn<Guid>(
                name: "SedeId",
                table: "SolicitudesHorario",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesHorario_SedeId",
                table: "SolicitudesHorario",
                column: "SedeId");

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudesHorario_Sedes_SedeId",
                table: "SolicitudesHorario",
                column: "SedeId",
                principalTable: "Sedes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudesHorario_Sedes_SedeId",
                table: "SolicitudesHorario");

            migrationBuilder.DropIndex(
                name: "IX_SolicitudesHorario_SedeId",
                table: "SolicitudesHorario");

            migrationBuilder.DropColumn(
                name: "SedeId",
                table: "SolicitudesHorario");
        }
    }
}
