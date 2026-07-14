# ADR-0010: El tenant del request sale del token (supersede ADR-0004)

- **Estado:** Aceptada (2026-07-14)
- **Supersede:** ADR-0004 (tenant demo fijo)

## Contexto

Hasta ahora todos los repositorios de gestión operaban sobre un tenant demo
fijo (`AppDbContext.TenantDemoId`, ADR-0004), suficiente mientras existía un
solo profesor. Con el registro de profesores nuevos (cada uno con SU club),
cada request debe operar el tenant correcto: el profe A jamás puede ver ni
escribir datos del profe B.

## Decisión

`ITenantActual` (scoped, vive lo que dura el request) resuelve el tenant con
esta **precedencia**:

1. **Override explícito** (`Establecer(tenantId)`): lo usa el flujo del
   ALUMNO — su token no trae tenant; `PortalService.FichaDeAsync` fija el
   tenant del club de su ficha, y con eso la generación de turnos y la
   liquidación de cuotas operan el club correcto.
2. **Claim `tenant` del JWT**: lo trae el PROFESOR (el club que administra).
   `TokenService` lo emite junto con el claim `profesor` al armar la sesión.
3. **Sin ninguno de los dos → `InvalidOperationException`**: fail-fast a
   propósito. Un request de gestión sin tenant es un bug; NUNCA se cae
   silenciosamente a un tenant por defecto (sería un agujero de datos).

Los 7 repositorios tenant-scoped (Alumno, Grupo, Sede, Horario, Turno, Cargo,
Bloqueo) y `TenantRepository.ObtenerActualAsync` inyectan `ITenantActual`.
`AppDbContext.TenantDemoId` sigue existiendo solo para el seed (`HasData`) y
`AuthSeeder`.

## Consecuencias

- Los tokens emitidos ANTES de este cambio no traen el claim `tenant`. Se
  cambió `Jwt:Audience` (bump a `-v2`) para invalidarlos de una: dan 401 y el
  front ya redirige a login solo.
- Las queries del portal que son por-usuario (`ObtenerPorUserIdAsync`,
  `ListarPorAlumnoEntreAsync`, etc.) siguen siendo globales a propósito: la
  identidad cruza clubes (ADR-0007).
- Un usuario que sea profe Y alumno de otro club funciona: su claim apunta a
  su propio club, pero el portal SIEMPRE pisa con el tenant de la ficha.
