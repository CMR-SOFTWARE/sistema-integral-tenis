# CLAUDE.md — Cómo construir S.I.D. (Sistema Integral Deportivo)

Instrucciones para Claude al trabajar en este repo. Son el **acuerdo de trabajo**, no
sugerencias: seguilas salvo que Lucas diga lo contrario.

**Contexto:** SaaS multi-tenant para academias de tenis. Lo opera un profesor que además
lo **revende** a otros profes (es Owner + Admin de la plataforma). Lucas (fundador) viene
de **C#/.NET** y está aprendiendo Node/git.

**El equipo son varias personas**, no solo quien te esté hablando: hay quien sigue el
producto y hay un compañero con una rama de **rediseño del front**
(`feature/correcciones-front`) pendiente de mergear. Antes de encarar un rediseño grande de
una pantalla, avisá: van a chocar en los `.module.css`.

**Antes de proponer una feature, leé [`docs/pedidos-del-profe.md`](docs/pedidos-del-profe.md):**
es el backlog vivo con lo que pidió el cliente, lo que ya está en producción y —sobre todo—
**las decisiones de producto ya tomadas**. No las vuelvas a discutir ni las decidas distinto.

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
  - **RENOMBRAR ES EL CASO PELIGROSO.** Al renombrar una tabla o una entidad, EF scaffoldea
    `DropTable` + `CreateTable` y avisa *"may result in the loss of data"*. Eso **borra los
    datos en producción**: EF no puede saber que es un rename y no una tabla nueva; eso lo
    sabe quien hizo el cambio. Hay que **reescribir la migración a mano** con `RenameTable`,
    `RenameIndex` y `RENAME CONSTRAINT` para la PK y la FK. Ejemplo vivo:
    `Migrations/20260813113840_RenombrarAvisosANoticias.cs`.
  - Antes de mergear una migración que toca datos, **probala contra la base local con datos
    reales adentro**: aplicarla, revertirla y volver a aplicarla. `dotnet ef migrations
    script <desde> <hasta>` muestra el SQL exacto sin tocar nada.
  - No renombres las constraints `NOT NULL` que Postgres 18 crea con nombre
    (`Avisos_Titulo_not_null`): en una versión anterior ni existen y la migración fallaría
    en producción. Son cosméticas, EF nunca las referencia por nombre.
- **Frontend:** React + TypeScript + Vite + **React Query** (caché; las query keys incluyen
  el recurso y el alumno activo cuando aplica) + **CSS Modules**. Los tipos del front son
  **espejo de los DTOs** del back.
- **MOBILE FIRST, siempre.** El profe usa la app **parado en la cancha, con el celular**:
  esa es la pantalla real, la de escritorio es la excepción. Reglas concretas:
  - Los estilos **base** son los del celular; lo de escritorio se agrega con
    `@media (min-width: ...)`, nunca al revés (`max-width` es parche, no diseño).
  - **Nada desborda la pantalla.** Lo ancho —**tablas y grillas**— va en un contenedor con
    `overflow-x: auto`; `overflow: hidden` corta el contenido y lo deja inalcanzable.
    **Ojo: eso es para tablas, NO para barras de filtros.** Una toolbar **envuelve**, porque
    una tira que se desliza esconde la mitad de los controles sin avisar que están ahí.
  - En una barra de herramientas, los controles que se leen juntos (‹ período ›, los dos
    botones de "crear") van en **un contenedor propio**: sueltos con `flex-wrap` se
    desparraman al envolverse, cada uno por su lado.
  - **Al convertir una tabla en tarjetas** con un media query (`display: block`), acordate
    de resetear su `min-width`: si queda, la "tarjeta" mide 560 px en una pantalla de 390 y
    las acciones se van fuera de la vista. Y ordená las celdas por **clase**, no por
    `nth-child`: dos tablas que comparten CSS y tienen distinta cantidad de columnas quedan
    ordenadas distinto.
  - `white-space: nowrap` en una celda le impide angostarse y **estira la fila entera**.
  - **Y esa fila estira la columna: `1fr` es `minmax(auto, 1fr)`,** y ese `auto` toma como
    piso el **ancho mínimo del contenido**. Una sola celda que no se angosta hace crecer
    toda la grilla más allá de la pantalla. En las grillas va **`minmax(0, 1fr)`**.
    - Los dos arreglos se necesitan y hacen cosas distintas: el `minmax(0, …)` evita que
      la **columna** crezca, y el `min-width: 0` deja que **la celda** se angoste una vez
      que la columna dejó de crecer. Con uno solo no alcanza — y `overflow: hidden` **no**
      reemplaza al `min-width`: solo deja que el texto se recorte *después*, no baja el
      mínimo que la celda le pide a la grilla.
    - Ejemplo vivo: `DetalleAlumnoModal.module.css`. La fila del cargo pedía 502 px en una
      pantalla de 390 y se llevaba el modal entero fuera de la vista.
  - **Un modal que desborda no se puede alcanzar.** Centrado con flex, el sobrante se
    reparte a los dos lados y la mitad izquierda queda fuera de la pantalla, adonde no se
    llega ni scrolleando. `components/Modal.module.css` ya está blindado (`min-width: 0`
    en la tarjeta y `justify-content: safe center`), pero eso **tapa el síntoma**: si una
    ficha se ve cortada, el culpable siempre es un hijo que no se angosta.
  - **El verde de marca no sirve para destacar nada**: ya significa "todo bien" (cuota al
    día, clase confirmada, botón principal, pestaña activa). Lo que tiene que llamar la
    atención va en **rojo** (`--color-danger`) o ámbar (`#b7791f`). Ejemplo: la noticia
    importante del portal.
  - Toda pantalla nueva se revisa a **~390 px** antes de darla por terminada. Y si la
    pantalla ya existía, **compará con cómo se veía antes**: lo que ya funcionaba en el
    celular no se rediseña de paso.
- **Comentarios** que explican el **por qué** (no el qué), en español, con la densidad del
  código de al lado.

## 4. Testing — TDD selectivo (ADR-0005)

- **Test-first solo en la lógica de negocio** (los `Service` con reglas), con **xUnit + Moq**
  (repos mockeados).
- **No** se testea scaffolding, repositorios ni UI.
- **Consecuencia que hay que tener presente: como los repos están mockeados, las consultas
  y proyecciones EF NO están cubiertas.** Un `GroupBy` sobre un campo que pasó a ser
  nullable, o un `Select` que EF no sabe traducir, revientan recién en runtime y con los
  455 tests en verde. **Cuando toques una proyección o una query, verificalo levantando la
  API y pegándole** (el README explica cómo, incluidas las credenciales del seed).

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
- **Nada de sintaxis de código en el mensaje de commit.** En bash, un `!` dentro de comillas
  dobles dispara la expansión de historial y **el commit no se hace** (`bash: !.Value: event
  not found`) — y si venía encadenado con un `push`, se pushea una rama vacía.
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
