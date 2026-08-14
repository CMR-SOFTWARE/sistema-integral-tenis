# Pedidos del profe — el backlog vivo

> **Empezá por acá si vas a seguir el producto.** Es el estado real de lo que pidió el
> cliente: qué está en producción, qué falta, y **qué decisiones ya se tomaron** para lo
> que falta. Lo último es lo que más duele perder: sin eso se vuelve a discutir algo que
> ya está acordado, o se decide distinto y hay que rehacerlo.
>
> Última actualización: 13/08/2026.

El cliente es el **profesor** que usa la app todos los días (y que además la revende a
otros profes). En agosto de 2026 mandó una lista de 13 pedidos. Esta es esa lista, con lo
que fuimos resolviendo.

> El texto de los pedidos llegó por chat. Está transcripto acá porque **es la única copia
> versionada**: el `Cosas que faltan a la app.txt` original nunca estuvo en el repo.

---

## Estado

| # | Pedido | Estado |
|---|---|---|
| 4 | Filtrar alumnos por profe y por club | ✅ Bloque 1 — 12/08/2026 |
| 5 | En el listado, el club y el profe a cargo | ✅ Bloque 1 |
| 6 | La lista de espera idéntica a la de alumnos | ✅ Bloque 1 |
| 12 (parte) | Que el director no se mezcle en las listas | ✅ Bloque 1 |
| 7 | El que está en espera ve el portal común | ✅ Bloque 2 — 13/08/2026 |
| 13 | Noticias con importancia, editables | ✅ Bloque 2 |
| 9 | "Mis torneos" y "Ranking" (próximamente) | ✅ Bloque 2 |
| 1 | Clase suelta que asigna el profe + clase de prueba | ✅ Bloque 3 — 13/08/2026 |
| **3** | **"Próximas clases" con más info y clickeable** | ⬜ **Bloque 4 — lo próximo** |
| **8** | **Shop con carrito** | ⬜ Bloque 5 |
| **2** | **El director habilita al empleado a cobrar** | ⬜ Bloque 6 |
| **10** | **Roles y alta de academias desde Plataforma** | ⬜ Bloque 6 |
| **11** | **Usuarios en Plataforma; Alumnos y Espera en Mi academia** | ⬜ Bloque 6 |
| **12** | **El director como usuario común; alumno → profesor** | ⬜ Bloque 6 |

**Un bloque por PR.** Cada uno arranca planificando (ver `CLAUDE.md` §2).

---

## Lo que ya salió

### Bloque 1 — las tres listas (12/08/2026)

Filtro por club, `Club · Profe` en cada fila (reemplazó al teléfono), y **la lista de
espera pasó a ser la misma tabla que Alumnos**, con los mismos datos y filtros pero
conservando sus acciones propias.

- `EsperaResponseDto` **hereda** de `AlumnoResponseDto`: es lo que evita que las dos filas
  se desincronicen cuando alguna gane un campo.
- `SolicitudService` se partió en el cálculo de motivos y el mapeo. **El badge de la
  pestaña usa solo el cálculo**, para no encarecer una pantalla que ya estaba optimizada.
- El **director y los profes empleados dejaron de aparecer en la espera** cuando no toman
  clases: están trabajando, no esperando horario.

Front: `features/alumnos/TablaAlumnos.tsx`, `FiltrosAlumnos.tsx` y `useFiltrosAlumnos.ts`
son compartidos por las tres listas.

### Bloque 2 — el portal del alumno (13/08/2026)

- **El que está en lista de espera ya no ve una pantalla muerta.** Antes el Inicio se
  cortaba con una tarjeta sola. Ahora ve el portal completo, con una banda de estado y un
  botón que lo lleva a pedir lugar en una clase — su única salida real. Resultó ser un solo
  `early return`: el resto del portal nunca lo bloqueó (se gatea por "no tenés ficha").
- **`Aviso` se renombró a `Noticia`** en todo el stack, gana `Importante` y edición. Las
  importantes suben al Inicio del alumno en rojo; el resto vive en la sección Noticias.
  `EditarAsync` **no toca `Activo`** a propósito: corregir un título no revive algo que el
  profe bajó (prender y apagar tiene su endpoint).
- "Mi club" pasó a **"Mis clubes"**, dibujado como lista de cards.
- "Mis torneos" y "Ranking" con `components/Placeholder.tsx`.

### Bloque 3 — la clase suelta que asigna el profe (13/08/2026)

Botón "+ Clase suelta" en la Agenda: una clase individual en una fecha puntual, con un
switch que decide si **le genera el cargo** (precio individual de Configuración, impago, a
su cuenta corriente junto con la cuota) o si es **clase de prueba** y no se le cobra.

El camino del alumno (pedir desde el portal → informar el pago → el profe confirma) no se
tocó.

**Lo que agrandó el bloque:** un turno suelto no tenía profe (`ProfesorUserId` salía del
horario, y una clase suelta no tiene horario), así que no aparecía en la agenda del
empleado ni le contaba para el sueldo. Se agregó `Turno.ProfesorUserId`, usado **solo** en
los sueltos. Ver `docs/modelo-agenda.md`.

---

## Lo que falta

### Bloque 4 — "Próximas clases" del inicio (pedido 3)

> *"Acceso directo: donde dice próximas clases tiene que tener más información, o sea que
> pueda ver quien tiene esa clase, de que club y que profe a cargo. Es más o menos como la
> funcionalidad del calendario. Puede ser clickeable y verse todo ahí para que sea más
> fácil y también que se pueda editar."*

**Qué hay hoy:** la tarjeta "Próximas clases de hoy" del inicio del profe
(`features/dashboard/DashboardPage.tsx`) muestra hora, título, cancha, cantidad de
participantes y duración. **No es clickeable.**

**Qué falta:** sumar el club, el profe a cargo y los nombres de los alumnos al DTO de
`DashboardService`, y que la fila abra el modal de turno que **ya existe** en la agenda
(`features/agenda/TurnoModal.tsx`), que es donde están todas las acciones.

Es el más chico de los tres que faltan.

### Bloque 5 — Shop con carrito (pedido 8)

> *"Armar los servicios del profesor: que sea un shop donde el profe carga su catálogo de
> servicios como raquetas, tubo de pelotitas, encordados y al usuario asociado a esa
> academia pueda tener en la sección servicios (ahora llamada shop) los productos que
> ofrece la academia y el usuario pueda agregarlos a un carrito y realizar el pedido, ese
> pedido se le será añadido a la cuenta del usuario con su cuota mensual."*

**Qué hay hoy:** `Servicio` (catálogo del profe) y `Pedido` (uno por servicio, sin
carrito), con la pantalla del alumno en `features/portal/ServiciosPage.tsx` y la bandeja
del profe arriba de Cuotas.

**DECISIÓN TOMADA — no volver a discutirla:** el cargo **sigue naciendo cuando el profe
ACEPTA** el pedido. Es la decisión de `docs/modelo-precios.md` §M4 ("la cuenta corriente
solo tiene deudas reales"): si el cargo entrara solo, el profe se enteraría de una deuda
que quizás no puede cumplir (no tiene el encordado, se le acabaron las pelotas). Lo nuevo
es el **carrito** y que un pedido tenga **varias líneas**; al aceptar nace **un** cargo con
el total.

### Bloque 6 — Plataforma, roles y permisos (pedidos 2, 10, 11 y 12)

> *"El director tiene que tener una opción de modificar si el profe empleado puede cobrar
> una clase o una cuota."*
>
> *"El cliente quiere tener acceso desde mi plataforma a poder setear a cada usuario de la
> app que rol cumple en el sistema. Ejemplo: si una academia se registra a la plataforma
> que él le pueda dar de alta, que tenga acceso a toda la data que se registra a la
> plataforma como clubes, academias, usuarios, etc."*
>
> *"Usuarios deberían aparecer en plataforma, luego la sección alumnos y lista de espera
> debería estar en mi academia y él gestiona todo desde ahí."*
>
> *"El director también tiene que poder ser un usuario común y corriente como los demás,
> como los profesores, el tema es que no se mezclen en la lista de alumnos ni de espera si
> no toman clases en esa academia. (…) Habría que pensar cómo hacer que un usuario que es
> alumno y luego se convierte en profesor, cómo hacer para que la academia le dé de alta
> sin tener que crear otra cuenta."*

Es el bloque más grande y el único que toca auth. **Merece su propio plan.**

**Qué hay hoy:**

- El modelo de roles es **binario**: dueño (`Tenant.OwnerUserId`) y staff
  (`MembresiaTenant` con `RolTenant.Staff`). **No existen permisos finos**, y todo lo de
  plata es policy `Owner` (`CuotasController` entero).
- El panel `Plataforma` (`AdminController`, policy `Admin`) tiene métricas globales, el
  listado de clubes y activar/suspender. Es el único controller cross-tenant.
- El camino **alumno → profesor ya funciona a medias**: `StaffService.AgregarAsync` reusa
  el login si esa persona ya es alumno del tenant, sin crear otra cuenta.
- La parte del pedido 12 sobre las listas **ya salió en el bloque 1**.

**DECISIONES TOMADAS:**

- **"Usuarios deberían aparecer en Plataforma" = el panel admin cross-tenant**, no
  reordenar el menú del profe.
- **La trampa que ya nos mordió una vez:** la pestaña "Usuarios" de hoy lista la tabla
  `Alumnos`, o sea un padrón de **FICHAS**, no de personas. El director y los profes no
  tienen ficha salvo que alguien los haya cargado como alumnos. Mezclar personas sin ficha
  en esa tabla da filas donde la mitad de las acciones no aplican (no hay a quién pausar,
  dar de baja ni cobrarle cuota). El padrón de PERSONAS es un concepto distinto y por eso
  el profe lo pidió en Plataforma. Ver `docs/modelo-alumnos.md`.

---

## Fuera de la lista, pero acordado

- **Multi-club**: Lucas va a habilitar que una persona pertenezca a **varios clubes**. Por
  eso "Mis clubes" ya está dibujado como una lista (hoy siempre de uno). El guard que lo
  impide sigue en `SolicitudService.CrearAsync` ("Ya estás vinculado a un club. Por ahora
  se puede pertenecer a uno solo"), y cuando se saque hay que resolver a qué club pertenece
  cada cargo y qué muestra el Inicio.
- **Rediseño del front**: hay una rama de un compañero (`feature/correcciones-front`) con
  el diseño de la página, pendiente de mergear. Va a haber conflictos en varios
  `.module.css`. Conviene avisar antes de meterse en un rediseño grande de una pantalla.

## Performance — línea aparte, en pausa

No salió de la lista del profe: lo trajo Lucas porque en producción todo tardaba "5
segundos o más". Se hicieron varios PRs y **la agenda quedó 4,5 veces más rápida**.

El número que gobierna todo esto: **cada consulta a la base cuesta ~115 ms** (medido en
producción con `TiempoDeRequestMiddleware`, que loguea por request el total, los ms de
base, la cantidad de consultas y el tamaño de la respuesta). Lo que importa es **cuántas
consultas hace una pantalla**, no cuánto tarda cada una. Ante cualquier pantalla lenta,
mirar esa línea del log primero.

Quedaron dos pendientes:

1. **Las fotos de alumno son base64 en la fila** (`Alumno.FotoUrl`, hasta 700 KB). El
   listado de alumnos las trae todas. Hoy no duele porque casi nadie cargó, pero con 20
   fotos el listado se cae. El camino ya existe: `IAlmacenamientoArchivos` (Supabase
   Storage), que ya se usa para las del profe.
2. **La vista Mes manda 96 KB de JSON.** No es una optimización: es decidir mandar menos
   por turno en la grilla mensual, o sea una decisión de producto.
