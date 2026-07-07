# Arquitectura — Sistema Integral Deportivo

> El contrato del proyecto. Si un PR viola algo de acá, no se mergea.
> Última actualización: 03/07/2026

---

## 1. Las capas y la regla de dependencia

```
HTTP (React)
   │
   ▼
AlumnosController      ← recibe y responde, NO piensa
   │
   ▼
IAlumnoService         ← lógica de negocio, NO sabe de HTTP
   │
   ▼
IAlumnoRepository      ← acceso a datos, el ÚNICO que toca EF
   │
   ▼
AppDbContext (EF Core) → SQLite
```

**Regla de oro: cada capa solo conoce a la de abajo.**

| Capa | Puede | NO puede |
|---|---|---|
| Controller | Recibir DTOs, validar modelo (`[ApiController]`), llamar al service, devolver `ActionResult` | Tocar `AppDbContext`, tener reglas de negocio, devolver entidades crudas |
| Service | Reglas de negocio, orquestar repositorios, mapear entidad→DTO | Leer `HttpContext`/headers, tocar `AppDbContext` directo |
| Repository | Queries con EF Core, scoping por `TenantId` | Reglas de negocio, conocer DTOs de la API |

## 2. SOLID en este proyecto

- **S (una razón para cambiar)**: controller = HTTP, service = negocio, repository = datos. Si un cambio de regla de negocio te hace tocar el controller, algo está mal ubicado.
- **O (abierto/cerrado)**: se extiende agregando clases y registrándolas en DI, no modificando las existentes. Módulo nuevo = archivos nuevos.
- **L (sustitución)**: cualquier implementación de `IAlumnoRepository` debe honrar el contrato (p. ej. siempre scopear por tenant). Los tests con mocks dependen de esto.
- **I (interfaces específicas)**: DTOs por caso de uso (`CreateAlumnoDto` ≠ `UpdateAlumnoDto` ≠ `AlumnoResponseDto`). Nunca un mega-DTO que sirva "para todo".
- **D (inversión de dependencias)**: los services dependen de `IAlumnoRepository` (abstracción), nunca de `AlumnoRepository` (concreto). El DI de ASP.NET Core cablea todo en `Program.cs`; nadie hace `new` de una capa inferior.

## 3. Reglas no negociables

1. **DTO en todo borde**: nada entra sin validarse (`DataAnnotations` + `[ApiController]`); ninguna entidad EF sale cruda por la API (riesgo de filtrar campos sensibles y de acoplar el front al schema).
2. **Multi-tenant transversal**: ningún query sin `TenantId`. Hoy el tenant es fijo (`AppDbContext.TenantDemoId`); cuando haya auth, se resuelve del JWT — el scoping en repositorios no cambia.
3. **Transacciones donde hay leer-decidir-escribir** (reservar turno, tomar turno liberado): transacción explícita con aislamiento correcto. Lección aprendida del proyecto anterior (race condition en claim de slots).
4. **Estados, no borrados**: `Alumno` nunca se borra por flujo normal (`Suspendido`/`Inactivo`). El borrado real solo existe para el derecho al olvido (Ley 25.326).
5. **Consentimientos con timestamp**: cada consentimiento guarda *cuándo* se otorgó (`...El DateTime?`), no solo un bool.
6. **Secretos fuera del repo**: connection strings de prod y API keys van en variables de entorno, jamás commiteadas.

## 4. Anatomía de un módulo (backend)

Un módulo de dominio (ej. Alumnos) son estos archivos, en carpetas por capa:

```
Controllers/AlumnosController.cs      la puerta HTTP
Services/IAlumnoService.cs            el contrato de negocio
Services/AlumnoService.cs             la implementación (acá vive el negocio)
Repositories/IAlumnoRepository.cs     el contrato de datos
Repositories/AlumnoRepository.cs      queries EF Core, scopeadas por tenant
Dtos/CreateAlumnoDto.cs, ...          los bordes tipados
Models/Alumno.cs                      la entidad (compartida en Models/)
```

Registro en `Program.cs`:

```csharp
builder.Services.AddScoped<IAlumnoService, AlumnoService>();
builder.Services.AddScoped<IAlumnoRepository, AlumnoRepository>();
```

**Testing (TDD selectivo)**: los *services* se escriben test-first (xUnit + Moq, repositorio mockeado). Scaffolding, repositorios finos y controllers de mapeo no llevan unit tests; se validan con la migración y end-to-end.

## 5. Flujo de un request (ejemplo: `POST /api/alumnos`)

1. React envía JSON → el proxy de Vite lo lleva a Kestrel.
2. `[ApiController]` valida el `CreateAlumnoDto` (DataAnnotations) → 400 automático si falla.
3. `AlumnosController.Create()` llama `_service.CrearAsync(dto)`.
4. `AlumnoService` aplica reglas: menor de 18 → exige tutor y consentimiento; DNI duplicado en el tenant → error de dominio.
5. `AlumnoRepository.AgregarAsync()` persiste con EF Core (siempre con `TenantId`).
6. El service mapea la entidad a `AlumnoResponseDto` y el controller devuelve `201 Created`.

## 6. Frontend (React)

- **Organización por features** (`src/features/alumnos/`), no por tipo de archivo. Cada feature: página, componentes, hooks y CSS Modules propios.
- **Tokens de diseño** (`src/styles/tokens.css`): la paleta CourtSet vive en variables CSS; ningún componente hardcodea colores.
- **`src/lib/api.ts`** es el único lugar que sabe hablar HTTP; los componentes usan hooks que lo envuelven.
- Layout compartido en `src/components/layout/`; páginas provisorias con `Placeholder`.

## 7. Decisiones registradas

Las decisiones de arquitectura y su porqué viven en [`docs/adr/`](adr/). Ante la duda de "¿por qué esto es así?", buscá ahí antes de cambiar nada.
