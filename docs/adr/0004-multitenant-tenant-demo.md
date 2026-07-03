# ADR-0004 — Multi-tenant desde el día 1, con tenant demo fijo hasta el login

- **Fecha**: 2026-06-18
- **Estado**: Aceptada

## Contexto

El producto es SaaS multi-tenant (cada profesor/club es un universo cerrado de
datos). Retrofitear multi-tenancy después es carísimo; pero construir auth
completa antes del primer módulo frenaría el prototipo.

## Decisión

- El **schema es multi-tenant desde la primera migración**: `TenantId` en todas
  las tablas, unicidad compuesta (p. ej. DNI único *por tenant*, no global),
  índices por tenant.
- Mientras no haya login real, **todo opera sobre un tenant demo fijo**
  (`AppDbContext.TenantDemoId`, sembrado por la migración inicial). Ningún
  repository acepta queries sin tenant, así el scoping ya queda ejercitado.
- Cuando llegue auth (ASP.NET Core Identity + JWT), el tenant se resolverá del
  token/URL (tenants por path según plan v2) y solo cambia *de dónde sale* el
  `TenantId`; los repositorios no se tocan.

## Consecuencias

- El prototipo es mono-tenant en la práctica, multi-tenant en el diseño.
- La regla "ningún query sin TenantId" es no negociable ya (arquitectura.md §3.2).
