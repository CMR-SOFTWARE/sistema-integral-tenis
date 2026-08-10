using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <summary>
    /// "Estar en la lista de espera" deja de ser un estado del alumno y pasa a
    /// derivarse de no tener ninguna clase asignada.
    ///
    /// El schema no cambia (Estado es texto y sigue siéndolo): lo que cambia son los
    /// DATOS. Las filas con 'EnEspera' hay que convertirlas o la app revienta al
    /// leerlas, porque el enum ya no tiene ese valor.
    /// </summary>
    public partial class EsperaDerivadaDeClase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los que estaban en espera pasan a Activo. No pierden nada: siguen sin
            // clase, así que la lista de espera los sigue mostrando, y ahora además
            // aparecen en Usuarios. Tampoco se les empieza a cobrar: la cuota se
            // genera solo para los que tienen clase (CuotaMensualManual).
            migrationBuilder.Sql(
                @"UPDATE ""Alumnos"" SET ""Estado"" = 'Activo' WHERE ""Estado"" = 'EnEspera';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reconstruible de verdad, porque el criterio nuevo ES la definición vieja:
            // volver a marcar EnEspera a los activos que no tienen ninguna clase.
            migrationBuilder.Sql(
                @"UPDATE ""Alumnos"" SET ""Estado"" = 'EnEspera'
                  WHERE ""Estado"" = 'Activo'
                    AND ""Id"" NOT IN (
                        SELECT ah.""AlumnoId""
                        FROM ""AlumnoHorarios"" ah
                        JOIN ""Horarios"" h ON h.""Id"" = ah.""HorarioId""
                        WHERE ah.""FechaBaja"" IS NULL AND h.""Activo"" = TRUE
                    );");
        }
    }
}
