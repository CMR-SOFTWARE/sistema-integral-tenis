using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <summary>
    /// Se va lo que quedaba del modelo viejo: las tablas Grupos, AlumnoGrupos y
    /// SolicitudesGrupo, y las columnas Horarios.GrupoId / Horarios.AlumnoId. Los
    /// datos ya se habían copiado al roster del horario en HorariosConCupo
    /// (05/08/2026) y se verificaron en producción; desde entonces nadie los leía.
    ///
    /// Es la contracara de aquella migración, que a propósito NO borró nada para
    /// poder volver atrás. Se hace ahora y no más adelante porque el modelo C# ya
    /// no tiene esas entidades: si el snapshot quedara desincronizado, estos DROP
    /// se colarían dentro de la próxima migración que alguien generara por otro motivo.
    ///
    /// El Down() recrea la ESTRUCTURA, no los datos: para eso está el dump previo.
    /// </summary>
    public partial class EliminarGrupos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Horarios_Alumnos_AlumnoId",
                table: "Horarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Horarios_Grupos_GrupoId",
                table: "Horarios");

            migrationBuilder.DropTable(
                name: "AlumnoGrupos");

            migrationBuilder.DropTable(
                name: "SolicitudesGrupo");

            migrationBuilder.DropTable(
                name: "Grupos");

            migrationBuilder.DropIndex(
                name: "IX_Horarios_AlumnoId",
                table: "Horarios");

            migrationBuilder.DropIndex(
                name: "IX_Horarios_GrupoId",
                table: "Horarios");

            migrationBuilder.DropColumn(
                name: "AlumnoId",
                table: "Horarios");

            migrationBuilder.DropColumn(
                name: "GrupoId",
                table: "Horarios");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AlumnoId",
                table: "Horarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GrupoId",
                table: "Horarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Grupos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    Categoria = table.Column<string>(type: "text", nullable: true),
                    CreadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CupoMaximo = table.Column<int>(type: "integer", nullable: true),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    ProfesorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValorMensual = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grupos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grupos_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlumnoGrupos",
                columns: table => new
                {
                    AlumnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrupoId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaBaja = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlumnoGrupos", x => new { x.AlumnoId, x.GrupoId });
                    table.ForeignKey(
                        name: "FK_AlumnoGrupos_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlumnoGrupos_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesGrupo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlumnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrupoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    ResueltoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesGrupo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesGrupo_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitudesGrupo_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitudesGrupo_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Horarios_AlumnoId",
                table: "Horarios",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_Horarios_GrupoId",
                table: "Horarios",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_AlumnoGrupos_GrupoId",
                table: "AlumnoGrupos",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_Grupos_TenantId_Activo",
                table: "Grupos",
                columns: new[] { "TenantId", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesGrupo_AlumnoId",
                table: "SolicitudesGrupo",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesGrupo_GrupoId",
                table: "SolicitudesGrupo",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesGrupo_TenantId_Estado",
                table: "SolicitudesGrupo",
                columns: new[] { "TenantId", "Estado" });

            migrationBuilder.AddForeignKey(
                name: "FK_Horarios_Alumnos_AlumnoId",
                table: "Horarios",
                column: "AlumnoId",
                principalTable: "Alumnos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Horarios_Grupos_GrupoId",
                table: "Horarios",
                column: "GrupoId",
                principalTable: "Grupos",
                principalColumn: "Id");
        }
    }
}
