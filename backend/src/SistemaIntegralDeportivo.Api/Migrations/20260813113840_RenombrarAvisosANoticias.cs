using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <summary>
    /// Avisos pasa a llamarse Noticias, y gana la marca de destacada.
    ///
    /// ESCRITA A MANO. El scaffold de EF generaba DropTable("Avisos") + CreateTable("Noticias"),
    /// que en producción borraba todo lo que el profe ya había publicado (avisó con
    /// "may result in the loss of data"). EF no puede saber que es un rename y no una tabla
    /// nueva: eso lo sabe quien hizo el cambio. Acá se renombra la tabla, su índice y sus
    /// constraints, así los datos viajan intactos.
    /// </summary>
    public partial class RenombrarAvisosANoticias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Avisos",
                newName: "Noticias");

            migrationBuilder.RenameIndex(
                name: "IX_Avisos_TenantId",
                table: "Noticias",
                newName: "IX_Noticias_TenantId");

            // Postgres conserva el nombre viejo de la PK y la FK al renombrar la tabla, y
            // el snapshot de EF ya las espera con el nombre nuevo. Si quedaran desfasadas,
            // la próxima migración que las toque por nombre fallaría.
            migrationBuilder.Sql(@"ALTER TABLE ""Noticias"" RENAME CONSTRAINT ""PK_Avisos"" TO ""PK_Noticias"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Noticias"" RENAME CONSTRAINT ""FK_Avisos_Tenants_TenantId"" TO ""FK_Noticias_Tenants_TenantId"";");

            migrationBuilder.AddColumn<bool>(
                name: "Importante",
                table: "Noticias",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Importante",
                table: "Noticias");

            migrationBuilder.Sql(@"ALTER TABLE ""Noticias"" RENAME CONSTRAINT ""FK_Noticias_Tenants_TenantId"" TO ""FK_Avisos_Tenants_TenantId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Noticias"" RENAME CONSTRAINT ""PK_Noticias"" TO ""PK_Avisos"";");

            // El índice se renombra ANTES de la tabla: hasta la última línea sigue
            // llamándose "Noticias".
            migrationBuilder.RenameIndex(
                name: "IX_Noticias_TenantId",
                table: "Noticias",
                newName: "IX_Avisos_TenantId");

            migrationBuilder.RenameTable(
                name: "Noticias",
                newName: "Avisos");
        }
    }
}
