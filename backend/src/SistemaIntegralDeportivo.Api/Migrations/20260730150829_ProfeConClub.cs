using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProfeConClub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SedeId",
                table: "MembresiasTenant",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembresiasTenant_SedeId",
                table: "MembresiasTenant",
                column: "SedeId");

            migrationBuilder.AddForeignKey(
                name: "FK_MembresiasTenant_Sedes_SedeId",
                table: "MembresiasTenant",
                column: "SedeId",
                principalTable: "Sedes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MembresiasTenant_Sedes_SedeId",
                table: "MembresiasTenant");

            migrationBuilder.DropIndex(
                name: "IX_MembresiasTenant_SedeId",
                table: "MembresiasTenant");

            migrationBuilder.DropColumn(
                name: "SedeId",
                table: "MembresiasTenant");
        }
    }
}
