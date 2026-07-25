# CLAUDE.md — Cómo construir S.I.D. (Sistema Integral Deportivo)

Instrucciones para Claude al trabajar en este repo. Son el **acuerdo de trabajo**, no
sugerencias: seguilas salvo que Lucas diga lo contrario.

**Contexto:** SaaS multi-tenant para academias de tenis. Lo opera un profesor que además
lo **revende** a otros profes (es Owner + Admin de la plataforma). Lucas (fundador) viene
de **C#/.NET** y está aprendiendo Node/git.

---

## 1. Comunicación

- **Español**, con analogías a **C#/.NET** cuando ayude (Lucas viene de ahí).
- Directo, sin relleno. **Recomendar** una opción, no listar todas.
- Las decisiones de **producto** se consultan (no asumir); las de implementación las toma Claude.

## 2. Proceso de trabajo

- Features grandes → **plan mode** primero: analizar → proponer → `ExitPlanMode` para aprobar.
  Un **tema/batch por vez**.
- **Claude escribe y verifica el código**; Lucas ejecuta git y los comandos locales,
  **guiado un paso a la vez** (está aprendiendo y quiere entender qué hace cada cosa).
- No se cierra nada sin **`dotnet test` (backend) + `npm run build` (front) en verde**.

## 3. Arquitectura

El dominio y las decisiones viven en [`docs/adr/`](docs/adr/) y [`docs/modelo-*.md`](docs/).
Reglas que se respetan siempre:

- **Capas estrictas:** `Controller` (HTTP, sin lógica) → `Service` (reglas de negocio) →
  `Repository` (datos con EF Core, **scopeado por `TenantId`**). El controller nunca tiene
  reglas ni toca la base.
- **Multi-tenant en todo:** el `TenantId` sale del JWT o del override del portal
  (`ITenantActual` / `IUsuarioActual` / `IFichaActual`, scoped por request).
- **DTOs en el borde:** nunca se expone una entidad EF. Enums como texto.
- **Base: PostgreSQL** (Supabase en prod, un Postgres **local** en dev). Las migraciones se
  **auto-aplican al arrancar** la API. Si tocás el modelo → `dotnet ef migrations add <Nombre>`
  → **revisá el archivo generado** antes de correr.
- **Frontend:** React + TypeScript + Vite + **React Query** (caché; las query keys incluyen
  el recurso y el alumno activo cuando aplica) + **CSS Modules**. Los tipos del front son
  **espejo de los DTOs** del back.
- **Comentarios** que explican el **por qué** (no el qué), en español, con la densidad del
  código de al lado.

## 4. Testing — TDD selectivo (ADR-0005)

- **Test-first solo en la lógica de negocio** (los `Service` con reglas), con **xUnit + Moq**
  (repos mockeados).
- **No** se testea scaffolding, repositorios ni UI.

## 5. Git — lo ejecuta Lucas, guiado

- **Lucas corre TODOS los comandos git él mismo.** Claude no toca git (ni `switch`, ni commit,
  ni push, ni PR): solo da los comandos exactos + una explicación corta de cada uno.
- Usar **`git switch`** (no `checkout`). Circuito completo, sin saltear pasos:
  1. `git switch main` → `git pull` → `git switch -c feat/<algo>`
  2. `git add -A` → `git commit …` → `git push -u origin feat/<algo>`
  3. Claude da **título + descripción** del PR; Lucas lo abre en GitHub.
  4. CI verde → Lucas mergea.
  5. **Cierre:** `git switch main` → `git pull` → `git branch -d feat/<algo>`.
- **Commits SIN co-autor** (nada de `Co-Authored-By`). En PowerShell, para el cuerpo usar
  varios `-m` (un párrafo por `-m`).
- **Merge = deploy a producción** (Railway el back, Vercel el front, automático). Cuidado
  redoblado con features destructivas y datos reales.

## 6. Principios de producto

- **Borrado real (hard delete) + baja lógica conviven.** El borrado real siempre con
  **confirmación fuerte** ("no se puede deshacer"), botón **separado** del de baja. Este
  patrón se **replica en cada entidad** que se toque.
- **Datos opcionales** donde el profe no siempre los tiene (email, DNI, fecha de nacimiento).
  El **login es el celular**.
- **Cuenta familiar:** un titular (un login) gestiona varias fichas.
- Todo pensado **multi-tenant**: cada profe/academia es un tenant aislado.

## 7. Seguridad y datos

- **Nunca secrets en el repo:** en dev van en `user-secrets`; en prod, en variables de
  entorno (Railway).
- **Nunca apuntar el entorno de desarrollo a la base de producción.** Dev usa su Postgres
  local; prod es intocable salvo intención explícita.
