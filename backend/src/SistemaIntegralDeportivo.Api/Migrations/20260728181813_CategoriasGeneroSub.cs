using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class CategoriasGeneroSub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tema C: se elimina la categoría "Septima" (varones ahora van 1ra-6ta).
            // Las categorías se guardan como TEXTO, así que agregar A-D no cambia el
            // esquema; lo único que hay que migrar son los datos que sean "Septima" →
            // pasan a "Sexta" (la más baja de varones) en las 3 tablas con categoría.
            migrationBuilder.Sql("UPDATE \"Alumnos\" SET \"Categoria\" = 'Sexta' WHERE \"Categoria\" = 'Septima';");
            migrationBuilder.Sql("UPDATE \"Grupos\" SET \"Categoria\" = 'Sexta' WHERE \"Categoria\" = 'Septima';");
            migrationBuilder.Sql("UPDATE \"AspNetUsers\" SET \"Categoria\" = 'Sexta' WHERE \"Categoria\" = 'Septima';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remap de datos irreversible: no se puede saber qué "Sexta" era "Septima".
        }
    }
}
