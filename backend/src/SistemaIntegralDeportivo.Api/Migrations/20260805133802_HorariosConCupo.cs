using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <inheritdoc />
    public partial class HorariosConCupo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Horarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CupoMaximo",
                table: "Horarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Horarios",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AlumnoHorarios",
                columns: table => new
                {
                    AlumnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    HorarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaBaja = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlumnoHorarios", x => new { x.AlumnoId, x.HorarioId });
                    table.ForeignKey(
                        name: "FK_AlumnoHorarios_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlumnoHorarios_Horarios_HorarioId",
                        column: x => x.HorarioId,
                        principalTable: "Horarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesCupo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlumnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    HorarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    CreadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResueltoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCupo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesCupo_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitudesCupo_Horarios_HorarioId",
                        column: x => x.HorarioId,
                        principalTable: "Horarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitudesCupo_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlumnoHorarios_HorarioId",
                table: "AlumnoHorarios",
                column: "HorarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCupo_AlumnoId",
                table: "SolicitudesCupo",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCupo_HorarioId",
                table: "SolicitudesCupo",
                column: "HorarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCupo_TenantId_Estado",
                table: "SolicitudesCupo",
                columns: new[] { "TenantId", "Estado" });

            // ── Los datos que ya existen pasan al modelo nuevo ──
            // Hasta acá el roster vivía en el grupo (o en Horario.AlumnoId para las
            // clases particulares). Se copia al roster propio del horario SIN borrar
            // nada de lo viejo: las tablas de grupos quedan intactas hasta la
            // migración de limpieza, que va recién cuando esto esté verificado
            // contra producción.

            // Clases grupales: cada horario hereda los alumnos de su grupo, con sus
            // fechas — así una baja histórica sigue siendo una baja.
            migrationBuilder.Sql(@"
                INSERT INTO ""AlumnoHorarios"" (""AlumnoId"", ""HorarioId"", ""FechaAlta"", ""FechaBaja"")
                SELECT ag.""AlumnoId"", h.""Id"", ag.""FechaAlta"", ag.""FechaBaja""
                FROM ""Horarios"" h
                JOIN ""AlumnoGrupos"" ag ON ag.""GrupoId"" = h.""GrupoId""
                WHERE h.""GrupoId"" IS NOT NULL
                ON CONFLICT DO NOTHING;");

            // Clases particulares: su único alumno pasa a ser el roster.
            migrationBuilder.Sql(@"
                INSERT INTO ""AlumnoHorarios"" (""AlumnoId"", ""HorarioId"", ""FechaAlta"", ""FechaBaja"")
                SELECT h.""AlumnoId"", h.""Id"", h.""CreadoEl"", NULL
                FROM ""Horarios"" h
                WHERE h.""AlumnoId"" IS NOT NULL
                ON CONFLICT DO NOTHING;");

            // El nombre, la categoría y el cupo del grupo pasan al horario. Si un
            // grupo tenía tres horarios, los tres quedan iguales — que es exactamente
            // como venía funcionando.
            migrationBuilder.Sql(@"
                UPDATE ""Horarios"" h
                SET ""Nombre"" = g.""Nombre"",
                    ""Categoria"" = g.""Categoria"",
                    ""CupoMaximo"" = g.""CupoMaximo""
                FROM ""Grupos"" g
                WHERE h.""GrupoId"" = g.""Id"";");

            // Una clase particular es, en el modelo nuevo, un horario de cupo 1.
            migrationBuilder.Sql(@"
                UPDATE ""Horarios""
                SET ""CupoMaximo"" = 1
                WHERE ""AlumnoId"" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlumnoHorarios");

            migrationBuilder.DropTable(
                name: "SolicitudesCupo");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Horarios");

            migrationBuilder.DropColumn(
                name: "CupoMaximo",
                table: "Horarios");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Horarios");
        }
    }
}
