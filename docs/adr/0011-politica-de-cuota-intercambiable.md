# ADR-0011 — La generación de la cuota es una política intercambiable

- **Fecha**: 2026-07-25
- **Estado**: Aceptada (revisa parte del [ADR-0009](0009-cuenta-corriente-de-cargos.md))

## Contexto

El profe (que además revende la plataforma) **cambia el modelo de cobro seguido**.
Arrancamos cobrando **por clase** (ADR-0009: un `Cargo` de tipo `Clase` por cada turno,
valor hora grupal ÷ asignados). En la práctica no le sirvió: cobra un **valor mensual
fijo** por alumno. Y muy probablemente vuelva a cambiar (por grupo, híbrido, etc.).

El riesgo no es *qué* modelo elegir hoy, sino que **cada cambio sea barato y no rompa**
pagos, morosidad, portal ni reportes. Si la fórmula de cobro está mezclada dentro de
`CuotaService`, cada vuelta de tuerca es cirugía riesgosa.

## Decisión

Aislar **la parte volátil detrás de una costura**: *cómo nacen los cargos del mes* vive
en `IPoliticaDeCuota` (una interfaz), inyectada por DI. `CuotaService` deja de generar
cargos: los **delega** en la política y después solo **lee el ledger** de `Cargo`.

- Implementación actual: **`CuotaMensualManual`** — un cargo `TipoCargo.Cuota` por alumno
  activo por mes = su `Alumno.Arancel` (manual). Idempotente; sin arancel no cobra; solo
  el mes corriente (no factura retroactivo).
- Cambiar el modelo (volver a por-clase, pasar a por-grupo, híbrido…) = **una clase nueva
  que implemente `IPoliticaDeCuota` + una línea en `Program.cs`**. Nada más se toca.
- El **ledger `Cargo` es el contrato estable**: pagos, morosidad (`TieneDeudaVencida` /
  `DebeSuspenderse`), el portal del alumno y los reportes leen cargos sin saber cómo
  nacieron. La volatilidad no se filtra.
- Los **valores son datos** (`Alumno.Arancel`, `Grupo.ValorMensual`), no código: cambiar
  un precio es editar un dato, no deployar.
- Cada política tiene sus **tests** (`CuotaMensualManualTests`); `CuotaServiceTests` prueba
  la lectura/pagos/estado, ajeno a la política.

## Consecuencias

- **Revisa el ADR-0009:** la generación por-clase (`TipoCargo.Clase` automático desde
  turnos, `Tenant.ValorHoraGrupal/Individual`) deja de usarse para la cuota. Esos campos
  quedan **vestigiales** (no se borran: los usan las clases sueltas); el enum `Clase`
  queda como legado histórico. Se suma `TipoCargo.Cuota` y `Grupo.ValorMensual`.
- **`Alumno.Arancel` vuelve a ser protagonista** (el ADR-0009 lo había dado por obsoleto):
  es la fuente de verdad de la cuota mensual.
- **Cutover:** el modelo nuevo aplica de acá en adelante; la política solo genera el mes
  corriente y si el alumno no tiene ya su cargo Cuota → no toca la historia por-clase.
- Costo: una interfaz + una implementación "de más" hoy, que se paga la **primera** vez que
  el cliente cambia de idea. Se evita a propósito sobre-ingeniería (una sola costura, donde
  está la volatilidad real; sin plugins ni event-sourcing).
