# LEARNING.md — bitácora de aprendizaje

> Changelog de conceptos nuevos que fuimos incorporando construyendo el
> proyecto. Una entrada por sesión de trabajo. Lo escribo para el Lucas
> del futuro: si algo de esto se olvida, acá está el porqué.

---

## 2026-08-13 — Migraciones peligrosas, bugs latentes y datos derivados

**La migración que te borra la tabla**
- Al renombrar `Aviso` → `Noticia`, `dotnet ef migrations add` generó `DropTable` +
  `CreateTable` y avisó *"may result in the loss of data"*. Eso en producción **borraba
  todas las noticias del profe**. EF no puede saber que es un rename y no una tabla nueva:
  eso lo sabe quien hizo el cambio. Se reescribió a mano con `ALTER TABLE … RENAME`.
- Moraleja general: **el aviso de EF no es ruido**. Y "revisá el archivo generado" no es
  una formalidad — es el único momento en que alguien mira ese SQL antes de que corra
  contra la base del cliente.
- `dotnet ef migrations script <desde> <hasta>` muestra el SQL exacto **sin tocar nada**.
  Es la forma barata de auditar una migración antes de aplicarla.

**Los tests verdes que no prueban lo que importa**
- `SueldoPorHora` agrupaba con `GroupBy(t => t.HorarioId!.Value)`. Nunca falló, pero solo
  porque ningún turno sin horario llegaba con profe. En cuanto se le pudo asignar un profe
  a una clase suelta, ese `.Value` sobre un null tiraba la pantalla de Sueldos entera.
- **Un bug latente es el que está esperando un dato que todavía no existe.** El `!` (el
  null-forgiving de C#) es una promesa que le hacés al compilador: cuando escribís uno,
  estás apostando a que una condición se va a mantener para siempre.
- Los tests mockean los repositorios, así que **las consultas EF no están cubiertas**: 455
  tests en verde y la proyección podía no traducir a SQL. Lo único que lo prueba es
  levantar la API y pegarle.

**Datos derivados vs. datos guardados**
- Las tres listas (Alumnos / Espera / Usuarios) **no tienen ninguna columna** que diga en
  cuál está cada uno: se calculan de tener o no clase. Antes había un `EstadoAlumno.EnEspera`
  y se borró porque se desincronizaba de la realidad.
- Es la misma lección que `esMenor` (que se calcula de la fecha) y que el estado de la
  cuota: **un dato guardado que se puede derivar se vence solo**. La contra es que "no se
  puede editar a mano", y esa limitación se lee como bug desde la pantalla — hay que
  saber defenderla.

## 2026-07-05 — Diseño: identidad global y membresías

- **Persona ≠ relación**: el error clásico es que un usuario "SEA alumno O
  socio". Se modela una identidad global (Usuario, gratis) + membresías por
  tenant (Alumno hoy; Socio/ProfesorEnClub/Staff futuros). Analogía: Slack —
  una cuenta, N workspaces, rol distinto en cada uno.
- El `UserId` nullable que definimos en el modelo de alumnos resultó ser la
  pieza clave del producto entero: el patrón "reclamar ficha" (el negocio te
  precarga, vos reclamás al registrarte) generalizado a todas las membresías.
- Ranking y marketplace son de PLATAFORMA (cross-tenant): los puntos son de
  la persona, no de su ficha en un club. Por eso necesitan identidad global.
- Los negocios pagan suscripción; los jugadores entran gratis (son la red
  que da valor). Ver `docs/modelo-identidad-roles.md` y ADR-0007.

## 2026-07-03 — Fundación .NET completa + cimientos del frontend

**EF Core / migraciones**
- `dotnet ef migrations add X` NO toca la base: genera código C# (`Up()`/`Down()`)
  en `Migrations/`. Recién `dotnet ef database update` lo ejecuta. Equivalen a
  `Add-Migration` / `Update-Database` de la Package Manager Console.
- `HasData()` (seed) exige valores **determinísticos**: Guid y fechas fijas,
  nada de `Guid.NewGuid()` ni `DateTime.Now`, porque el seed forma parte de la
  migración y debe generar siempre el mismo SQL.
- `HasConversion<string>()` guarda enums como texto legible en la base.
- SQLite en modo WAL crea archivos auxiliares `.db-shm`/`.db-wal` → van al
  `.gitignore` junto con el `.db`.
- `DeleteBehavior.SetNull`: borrar un Tutor no borra al Alumno, lo deja sin tutor.

**Git**
- Los warnings `LF will be replaced by CRLF` son inofensivos: conversión de fin
  de línea Unix↔Windows.
- `git add .` solo agrega desde la carpeta actual hacia abajo; `git add -A`
  agrega todo el repo (borrados incluidos). Correr `add` parado en una
  subcarpeta fue la causa de un staging a medias.
- Git detecta renames solo: si borrás `apps/web/X` y aparece `frontend/X` igual,
  al stagear ambos lados lo muestra como `R` y conserva la historia.

**TypeScript / React**
- La plantilla nueva de Vite activa `erasableSyntaxOnly`: prohíbe los
  *parameter properties* de constructor estilo C# (`constructor(public x: int)`).
  Ironía: quise escribir TS "como C#" y justo eso está vedado. Campos explícitos.
- CSS Modules: cada componente importa su `.module.css` y las clases quedan
  con scope local (no chocan entre componentes).
- El proxy de Vite (`server.proxy`) hace que el navegador vea un solo origen en
  dev: `/api` se reenvía a Kestrel (5223) y no hay drama de CORS en desarrollo
  (el CORS de la API queda configurado igual, para cuando el front no pase por
  el proxy).

**Seguridad / deudas**
- Warning NU1903 en `SQLitePCLRaw.lib.e_sqlite3` (transitiva del provider
  SQLite): anotada en ADR-0003, riesgo bajo en prototipo local, se diluye al
  migrar de motor en prod.

## 2026-06-18 — Decisiones de fundación

- **Backend a .NET** (ADR-0001): si NestJS se eligió por parecerse a ASP.NET
  Core, teniendo el original dominado se usa el original.
- Interfaces sí en .NET aunque en TS las habíamos descartado (ADR-0002): el
  costo de la abstracción depende del lenguaje, no es dogma.
- El mockup de Claude Design (`.dc.html`) no es React: es referencia visual a
  reconstruir (ADR-0006).
- Un SaaS sin usuarios vale ~lo que costó construirlo; la tracción (usuarios
  pagando) es lo que multiplica el valor. El marketplace vacío vale cero
  (problema cold-start) → primero validar con la vertical Alumnos.

## 2026-06-15 — Setup inicial (era NestJS, luego descartado)

- Monorepo pnpm con workspaces, scaffold NestJS + Vite/React, primer push a
  GitHub (`CMR-SOFTWARE/sistema-integral-tenis`). La guía quedó archivada en
  `docs/archivo/guia-setup-nestjs.md`.
