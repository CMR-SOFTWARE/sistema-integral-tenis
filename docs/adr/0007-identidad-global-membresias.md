# ADR-0007 — Identidad global + membresías por tenant

- **Fecha**: 2026-07-05
- **Estado**: Aceptada

## Contexto

Preguntas estructurales del producto: la misma persona puede ser alumna de
varios profesores, socia de varios clubes, o ambas cosas — y eso cambia en
el tiempo. Los profesores trabajan en varios clubes y pueden tener
profesores a cargo. Además hay que definir dónde viven la landing, el
ranking y el marketplace, y quién paga suscripción.

## Decisión

- **Usuario global**: una cuenta por persona real, registro gratis. Los
  negocios (tenants Profesor/Club) pagan suscripción; los jugadores no.
- **Membresías tenant-scoped**: lo que una persona ES se modela como
  relaciones persona↔tenant (`Alumno` hoy; `Socio`, `ProfesorEnClub`, rol
  `Staff` en fases futuras), nunca como un "tipo de usuario" global.
- **Vinculación por reclamo o invitación**: el negocio precarga la ficha y
  la persona la reclama al registrarse (match DNI/teléfono → completa
  `UserId`), o la persona solicita unirse / usa código y el negocio aprueba.
- **Ranking y Marketplace a nivel plataforma** (cross-tenant): los puntos y
  publicaciones son de la persona, no de su ficha en un tenant.
- **Experiencia**: landing pública con registro segmentado; home de jugador;
  dashboard de negocio; selector de contexto si hay múltiples membresías.

Detalle completo en `docs/modelo-identidad-roles.md`.

## Consecuencias

- Valida decisiones previas: `Alumno.UserId` nullable, DNI único POR tenant,
  `TipoTenant.Club`. Todo lo nuevo es **aditivo** (cero migración destructiva).
- Las verticales del profesor pueden seguir sin esperar a auth.
- Cuando llegue auth: `Usuario` + reclamo de fichas; el tenant activo se
  resuelve del contexto (reemplaza al tenant demo, ADR-0004).
- La Fase 2 (clubes) replica el patrón de `Alumno` para `Socio`.
