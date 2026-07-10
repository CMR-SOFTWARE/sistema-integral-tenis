# ADR-0009 — La cuenta del alumno es una cuenta corriente de cargos

- **Fecha**: 2026-07-10
- **Estado**: Aceptada

## Contexto

Al diseñar Cuotas, Lucas aportó dos realidades más del negocio: (1) hay
alumnos irregulares que pagan **por clase** (y futuros profes podrían
cobrar solo así), y (2) el profe además **vende productos y servicios**
(encordados, tubos de pelotas) que se suman a la liquidación del mes.
Una "cuota mensual" fija no modela nada de eso.

## Decisión

Se modela **Cargo** como línea de cuenta corriente del alumno:
`Clase` (auto desde turnos: valor hora grupal ÷ asignados, o individual
entera), `Producto` (manual con concepto) y `Ajuste` (manual, monto ±).
Cada cargo puede marcarse pagado (fecha del server + medio:
Efectivo/Transferencia/Otro).

- Alumno `Mensual`: "pagar el mes" salda todos sus cargos impagos del
  período de una vez; estado del mes CALCULADO (Pagada / Pendiente /
  Vencida si >10) — nunca almacenado.
- Alumno `PorClase`: paga cargo por cargo.
- Generación perezosa e idempotente de cargos de clase al consultar el
  período (índice único turno+alumno); turno cancelado no genera cargo;
  monto snapshot.
- Precios en la config del tenant: `ValorHoraGrupal` y `ValorClaseIndividual`.

## Consecuencias

- Un solo modelo cubre cuota mensual, pago por clase, ventas y descuentos.
- `Alumno.Arancel` (informativo desde ADR-0008) queda obsoleto: el monto
  real sale de cargos. Se elimina de la UI de alta; la columna puede
  removerse en una migración futura.
- El detalle de la liquidación es auditable línea por línea (base para el
  recibo/WhatsApp futuro).
- Nueva migración: tabla Cargos + `Alumno.ModalidadPago` + precios en Tenant.
