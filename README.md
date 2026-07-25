# Sistema Integral Deportivo (S.I.D)

SaaS **multi-tenant** para profesores de tenis: alumnos, grupos, agenda de
turnos, cuotas (cuenta corriente), servicios, y un **portal del alumno**. Cada
profesor/club es un tenant aislado.

## Stack

- **Backend**: ASP.NET Core Web API + EF Core + **PostgreSQL** (Supabase en prod). Auth con JWT.
- **Frontend**: React + Vite + TypeScript + React Query + CSS Modules.
- **Tests**: xUnit + Moq (TDD selectivo).

## Estructura

```
backend/    API .NET — Controller → Service → Repository (DI nativo, interfaces por capa)
  src/SistemaIntegralDeportivo.Api/   Controllers, Services, Repositories, Models, Dtos, Migrations
  tests/                              xUnit (lógica de negocio de los services)
frontend/   React — src/features/<vertical>/ (pantalla + hook + tipos + css module)
docs/       ADRs (decisiones de arquitectura) + modelo-*.md (el dominio) — doc viva
```

## Cómo correrlo

### Backend (API en `http://localhost:5223`)

Necesitás un **PostgreSQL local** (⚠️ nunca apuntes el entorno de desarrollo a la base de
producción). La connection string va en `user-secrets`, una sola vez:

```bash
cd backend
dotnet user-secrets set "ConnectionStrings:Default" \
  "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=<tu-password>" \
  --project src/SistemaIntegralDeportivo.Api
dotnet run --project src/SistemaIntegralDeportivo.Api   # las migraciones se aplican solas al arrancar
```

### Frontend (Vite en `http://localhost:5173`, proxya `/api` → `:5223`)

```bash
cd frontend
npm install
npm run dev
```

### Tests

```bash
cd backend
dotnet test
```

## Arquitectura en dos líneas

**Controller** (HTTP, sin lógica) → **Service** (las reglas de negocio) →
**Repository** (EF Core, hace el scoping por tenant). El `TenantId` sale del
JWT del profe, o del override del portal cuando entra un alumno (ADR-0010). El
detalle de cada decisión vive en [`docs/adr/`](docs/adr/).

## Producción

- **Base**: Supabase (PostgreSQL). **API**: Railway (aplica las migraciones al desplegar).
  **Front**: Vercel.
- **Merge a `main` = deploy automático** (backend y frontend).

## Cómo trabajamos (seguí esta línea 🙏)

El acuerdo de trabajo completo está en [`CLAUDE.md`](CLAUDE.md). En resumen:

1. **Rama por vertical, ANTES de tocar nada**: `git switch -c feat/<algo>`.
   Nunca se commitea directo a `main`.
2. **De atrás para adelante**: modelo → service con tests → endpoints → frontend.
   Así cada capa se prueba antes de montar la de arriba.
3. **TDD selectivo** (ADR-0005): test-first **solo en la lógica de negocio** (los
   services con reglas). NO en scaffolding, repos ni UI.
4. **Migraciones**: si tocás el modelo →
   `dotnet ef migrations add <Nombre> --project src/SistemaIntegralDeportivo.Api`
   → **revisá el archivo generado**. Se aplican solas al arrancar la API.
5. **PR y merge**: subís la rama (`git push -u origin <rama>`), abrís el PR, esperás el CI
   en verde, lo mergeás, y sincronizás tu local:
   ```bash
   git switch main && git pull && git branch -d <rama>
   ```
6. **Doc viva**: cada regla de dominio nueva se documenta en `docs/modelo-*.md`;
   las decisiones de arquitectura, como un ADR en `docs/adr/`.

## Documentación

- [`CLAUDE.md`](CLAUDE.md) — el acuerdo de trabajo (cómo se construye la app).
- [`docs/adr/`](docs/adr/) — decisiones de arquitectura (por qué .NET, TDD
  selectivo, multi-tenant, cuenta corriente de cargos…).
- [`docs/modelo-precios.md`](docs/modelo-precios.md),
  [`modelo-alumnos.md`](docs/modelo-alumnos.md),
  [`modelo-agenda.md`](docs/modelo-agenda.md),
  [`modelo-identidad-roles.md`](docs/modelo-identidad-roles.md) — el dominio.
- [`LEARNING.md`](LEARNING.md) — notas de aprendizaje.
```
