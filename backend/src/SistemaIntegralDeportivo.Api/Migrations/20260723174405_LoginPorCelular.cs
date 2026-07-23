using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class LoginPorCelular : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Alumnos",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Dni",
                table: "Alumnos",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "EsMenor",
                table: "Alumnos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Backfill: antes "menor" se derivaba de la fecha. Marcamos EsMenor en las
            // fichas existentes que hoy darían < 18 años (para no perder ese estado).
            migrationBuilder.Sql(
                "UPDATE \"Alumnos\" SET \"EsMenor\" = true " +
                "WHERE \"FechaNacimiento\" IS NOT NULL " +
                "AND \"FechaNacimiento\" > (CURRENT_DATE - INTERVAL '18 years');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsMenor",
                table: "Alumnos");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Alumnos",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Dni",
                table: "Alumnos",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
