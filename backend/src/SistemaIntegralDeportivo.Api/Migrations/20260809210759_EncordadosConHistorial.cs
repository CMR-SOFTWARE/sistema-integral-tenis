using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <summary>
    /// La raqueta pasa a tener historial de encordado. `Tension` y `MarcaEncordado`
    /// eran un solo juego de datos que se pisaba al reencordar: ahora cada encordado
    /// es una fila con su fecha, y puede ser híbrido (dos cuerdas distintas).
    ///
    /// OJO: el scaffold de EF proponía RENOMBRAR `Tension` a `Modelo` —habría dejado
    /// "24 kg" como modelo de raqueta— y borrar `MarcaEncordado` sin más. Por eso el
    /// Up() está escrito a mano: primero se COPIAN los datos al historial y recién
    /// después se borran las columnas viejas.
    /// </summary>
    public partial class EncordadosConHistorial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) El modelo de la raqueta: columna NUEVA y vacía (no es la tensión).
            migrationBuilder.AddColumn<string>(
                name: "Modelo",
                table: "Raquetas",
                type: "text",
                nullable: true);

            // 2) La tabla del historial.
            migrationBuilder.CreateTable(
                name: "Encordados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaquetaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CuerdaVertical = table.Column<string>(type: "text", nullable: false),
                    TensionVertical = table.Column<string>(type: "text", nullable: true),
                    CuerdaHorizontal = table.Column<string>(type: "text", nullable: true),
                    TensionHorizontal = table.Column<string>(type: "text", nullable: true),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    CreadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Encordados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Encordados_Raquetas_RaquetaId",
                        column: x => x.RaquetaId,
                        principalTable: "Raquetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Encordados_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Encordados_RaquetaId_Fecha",
                table: "Encordados",
                columns: new[] { "RaquetaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Encordados_TenantId",
                table: "Encordados",
                column: "TenantId");

            // 3) Copiar lo que había: cada raqueta con tensión o marca de encordado
            //    pasa a tener UN encordado. La fecha real no la sabemos (nunca se
            //    pidió), así que se usa la de carga de la raqueta, que es la
            //    aproximación honesta. CuerdaVertical es NOT NULL: si solo había
            //    tensión, se deja constancia de que la cuerda no se sabe.
            migrationBuilder.Sql(@"
                INSERT INTO ""Encordados""
                    (""Id"", ""TenantId"", ""RaquetaId"", ""CuerdaVertical"", ""TensionVertical"", ""Fecha"", ""CreadoEl"")
                SELECT
                    gen_random_uuid(),
                    r.""TenantId"",
                    r.""Id"",
                    COALESCE(NULLIF(TRIM(r.""MarcaEncordado""), ''), 'Sin especificar'),
                    NULLIF(TRIM(r.""Tension""), ''),
                    r.""CreadoEl""::date,
                    r.""CreadoEl""
                FROM ""Raquetas"" r
                WHERE NULLIF(TRIM(r.""Tension""), '') IS NOT NULL
                   OR NULLIF(TRIM(r.""MarcaEncordado""), '') IS NOT NULL;
            ");

            // 4) Recién ahora, las columnas viejas.
            migrationBuilder.DropColumn(name: "Tension", table: "Raquetas");
            migrationBuilder.DropColumn(name: "MarcaEncordado", table: "Raquetas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tension",
                table: "Raquetas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarcaEncordado",
                table: "Raquetas",
                type: "text",
                nullable: true);

            // Vuelta atrás: se recupera el ÚLTIMO encordado de cada raqueta (lo demás
            // del historial no tiene dónde ir — el modelo viejo guardaba uno solo).
            migrationBuilder.Sql(@"
                UPDATE ""Raquetas"" r
                SET ""Tension"" = e.""TensionVertical"",
                    ""MarcaEncordado"" = e.""CuerdaVertical""
                FROM (
                    SELECT DISTINCT ON (""RaquetaId"")
                        ""RaquetaId"", ""CuerdaVertical"", ""TensionVertical""
                    FROM ""Encordados""
                    ORDER BY ""RaquetaId"", ""Fecha"" DESC, ""CreadoEl"" DESC
                ) e
                WHERE e.""RaquetaId"" = r.""Id"";
            ");

            migrationBuilder.DropTable(name: "Encordados");
            migrationBuilder.DropColumn(name: "Modelo", table: "Raquetas");
        }
    }
}
