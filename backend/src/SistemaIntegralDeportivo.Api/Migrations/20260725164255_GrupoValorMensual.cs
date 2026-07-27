using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class GrupoValorMensual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ValorMensual",
                table: "Grupos",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValorMensual",
                table: "Grupos");
        }
    }
}
