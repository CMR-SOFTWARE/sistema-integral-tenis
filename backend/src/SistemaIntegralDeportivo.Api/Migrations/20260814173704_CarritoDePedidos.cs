using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaIntegralDeportivo.Api.Migrations
{
    /// <summary>
    /// "Pedido" pasa de tener un único servicio a una o varias líneas (carrito).
    ///
    /// ESCRITA A MANO. El scaffold de EF generaba DropColumn de ServicioId/NombreServicio/
    /// Precio en "Pedidos" sin copiar nada a la tabla nueva (avisó con "may result in the
    /// loss of data") — Pedido está en producción desde el 17/07/2026, así que esto habría
    /// borrado cualquier pedido real ya cargado. Acá se crea "PedidoLineas" primero y se
    /// copia cada pedido existente como una línea (Cantidad=1) antes de tocar las columnas
    /// viejas, así los datos viajan intactos.
    /// </summary>
    public partial class CarritoDePedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Servicios_ServicioId",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_ServicioId",
                table: "Pedidos");

            migrationBuilder.CreateTable(
                name: "PedidoLineas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PedidoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreServicio = table.Column<string>(type: "text", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoLineas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoLineas_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidoLineas_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoLineas_PedidoId",
                table: "PedidoLineas",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoLineas_ServicioId",
                table: "PedidoLineas",
                column: "ServicioId");

            // Cada pedido viejo (1 servicio) se convierte en UNA línea con cantidad 1,
            // ANTES de que las columnas de origen desaparezcan.
            migrationBuilder.Sql(@"
                INSERT INTO ""PedidoLineas"" (""Id"", ""PedidoId"", ""ServicioId"", ""NombreServicio"", ""PrecioUnitario"", ""Cantidad"")
                SELECT gen_random_uuid(), ""Id"", ""ServicioId"", ""NombreServicio"", ""Precio"", 1
                FROM ""Pedidos"";
            ");

            migrationBuilder.DropColumn(
                name: "NombreServicio",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "Precio",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "ServicioId",
                table: "Pedidos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreServicio",
                table: "Pedidos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Precio",
                table: "Pedidos",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ServicioId",
                table: "Pedidos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Mejor esfuerzo: cada pedido vuelve a tener UN servicio, tomando su PRIMERA
            // línea. Si un pedido quedó con más de una línea (ya se usó el carrito de
            // verdad), revertir pierde las demás — no hay forma de volver a "1 servicio"
            // sin perder algo, por eso esta migración normalmente no se revierte en
            // producción una vez que el carrito está en uso real.
            migrationBuilder.Sql(@"
                UPDATE ""Pedidos"" p
                SET ""ServicioId"" = pl.""ServicioId"", ""NombreServicio"" = pl.""NombreServicio"", ""Precio"" = pl.""PrecioUnitario""
                FROM (
                    SELECT DISTINCT ON (""PedidoId"") ""PedidoId"", ""ServicioId"", ""NombreServicio"", ""PrecioUnitario""
                    FROM ""PedidoLineas""
                    ORDER BY ""PedidoId"", ""Id""
                ) pl
                WHERE p.""Id"" = pl.""PedidoId"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_ServicioId",
                table: "Pedidos",
                column: "ServicioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Servicios_ServicioId",
                table: "Pedidos",
                column: "ServicioId",
                principalTable: "Servicios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "PedidoLineas");
        }
    }
}
