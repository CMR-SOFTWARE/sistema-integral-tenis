# ADR-0005 — TDD selectivo (test-first solo en lógica de negocio)

- **Fecha**: 2026-07-03
- **Estado**: Aceptada

## Contexto

Lucas quiere aplicar TDD, pero el proyecto es un prototipo construido por una
sola persona que además está aprendiendo el stack frontend. TDD dogmático en
todo (scaffolding, UI) agregaría fricción sin retorno.

## Decisión

**TDD selectivo**:

- **Test-first (rojo → verde → refactor)** en los *services* del backend, donde
  viven reglas con casos claros: regla del menor (edad < 18 → tutor +
  consentimiento obligatorios), unicidad de DNI por tenant, transiciones de
  estado, cálculos de cuotas. Herramientas: xUnit + Moq (repositorio mockeado).
- **Sin test-first** en: entidades/DbContext/migraciones (se validan corriendo
  la migración), repositorios finos sobre EF (si acaso, integración con SQLite),
  controllers de mapeo, y UI React (más adelante, E2E selectivo con Playwright).

## Consecuencias

- Primer ejemplo: `AlumnoService` se construye test-first en la Fase 1.
- La definición de "terminado" del plan v2 (compila + tests de negocio + CI +
  documentado) sigue vigente con este alcance de tests.
