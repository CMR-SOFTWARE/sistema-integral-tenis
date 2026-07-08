# ADR-0008 — Grupos y Turnos antes que Cuotas (el cargo nace del turno)

- **Fecha**: 2026-07-08
- **Estado**: Aceptada

## Contexto

Al arrancar la vertical Cuotas, Lucas aportó cómo cobra un profe en la
realidad: valor hora dividido por los participantes del turno, con alumnos
que pagan por clase y otros por mes (ver `docs/modelo-precios.md`). La cuota
no es un monto mensual fijo: es la suma de los cargos de las clases tomadas.

## Decisión

Se **reordena** la construcción: primero **Grupos**, después
**Horarios/Turnos**, y recién entonces **Cuotas**, que nace calculando
automáticamente desde los turnos (fórmula en `modelo-precios.md`). Se
descartó hacer una Cuotas provisoria de monto manual: implicaba rediseñarla
al llegar los turnos (retrabajo) y contradecía el pedido explícito de que
"el cálculo se haga solo".

Quedan firmes desde ya: vencimiento día 10 (estado calculado, no
almacenado), pago con fecha + medio (Efectivo/Transferencia/Otro), monto
como snapshot del momento de generación.

## Consecuencias

- El campo `Alumno.Arancel` pasa a ser informativo/transitorio: el monto
  real saldrá de los turnos. Revisar su rol al construir Cuotas.
- El placeholder "Cuotas pendientes" del dashboard espera una vertical más.
- Los pendientes de pricing (valor hora por tipo, ausencias, descuentos,
  parciales) se definen al llegar Cuotas (`modelo-precios.md` §4).
