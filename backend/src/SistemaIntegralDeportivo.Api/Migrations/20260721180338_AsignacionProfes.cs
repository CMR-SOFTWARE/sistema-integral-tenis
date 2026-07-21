using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AsignacionProfes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProfesorUserId",
                table: "Horarios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProfesorUserId",
                table: "Grupos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProfesorUserId",
                table: "Alumnos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfesorUserId",
                table: "Horarios");

            migrationBuilder.DropColumn(
                name: "ProfesorUserId",
                table: "Grupos");

            migrationBuilder.DropColumn(
                name: "ProfesorUserId",
                table: "Alumnos");
        }
    }
}
