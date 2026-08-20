# Pedidos del profe — el backlog vivo

> **Empezá por acá si vas a seguir el producto.** Es el estado real de lo que pidió el
> cliente: qué está en producción, qué falta, y **qué decisiones ya se tomaron** para lo
> que falta. Lo último es lo que más duele perder: sin eso se vuelve a discutir algo que
> ya está acordado, o se decide distinto y hay que rehacerlo.
>
> Última actualización: 20/08/2026.

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
| 9 | "Mis torneos" y "Ranking" (próximamente) | ✅ Bloque 2 — el ranking real existe, apagado |
| 1 | Clase suelta que asigna el profe + clase de prueba | ✅ Bloque 3 — 13/08/2026 |
| 3 | "Próximas clases" con más info y clickeable | ✅ Bloque 4 — 14/08/2026 |
| 8 | Shop con carrito | ✅ Bloque 5 — 14/08/2026 |
| 2 | El director habilita al empleado a cobrar | ✅ Bloque 6 — 15/08/2026 |
| 10 | Roles y alta de academias desde Plataforma | ✅ Bloque 6 |
| 11 | Usuarios en Plataforma; Alumnos y Espera en Mi academia | ✅ Bloque 6 — la 2ª mitad se descartó |
| 12 | El director como usuario común; alumno → profesor | ✅ Bloque 6 |

**Los 13 pedidos de la lista de agosto están en producción.** Lo que sigue abierto no
sale de esa lista: está en [Lo que falta](#lo-que-falta).

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

### Bloque 4 — "Próximas clases" del inicio, clickeable (14/08/2026)

> *"Acceso directo: donde dice próximas clases tiene que tener más información, o sea que
> pueda ver quien tiene esa clase, de que club y que profe a cargo. Es más o menos como la
> funcionalidad del calendario. Puede ser clickeable y verse todo ahí para que sea más
> fácil y también que se pueda editar."*

La tarjeta "Próximas clases de hoy" del inicio (`features/dashboard/DashboardPage.tsx`)
ahora suma club, profe a cargo y alumnos, y la fila es un link a
`/agenda?tab=calendario&turno=<id>` que abre el `TurnoModal` que **ya existía** en la
Agenda — mismo patrón de deep-link que `?nuevo=1`, en `CalendarioPage.tsx`. No se duplicó
el modal.

**Lo que agrandó el bloque:** al probarlo, el `TurnoModal` no ofrecía "Editar" para una
clase suelta (a diferencia de una clase de horario recurrente). Se sumó
`ClaseSueltaService.EditarAsync` — reprograma fecha/hora/cancha/duración/profe de una
suelta ya asignada, sin tocar alumno ni cobro — con su propio modal
(`EditarClaseSueltaModal.tsx`), solo para el dueño (mismo criterio que asignarlas).

---

### Bloque 5 — Shop con carrito (14/08/2026)

> *"Armar los servicios del profesor: que sea un shop donde el profe carga su catálogo de
> servicios como raquetas, tubo de pelotitas, encordados y al usuario asociado a esa
> academia pueda tener en la sección servicios (ahora llamada shop) los productos que
> ofrece la academia y el usuario pueda agregarlos a un carrito y realizar el pedido, ese
> pedido se le será añadido a la cuenta del usuario con su cuota mensual."*

`Pedido` pasó de "un servicio" a **una o varias líneas** (`PedidoLinea`, nueva tabla). El
alumno arma el carrito en `features/portal/ServiciosPage.tsx` (stepper de cantidad por
servicio, un total, un solo "Enviar pedido") y lo manda como **un** pedido. La bandeja del
profe nació como un panel plegable arriba de Cuotas (hoy es su propia pestaña, ver más
abajo) y lista todas las líneas de cada pedido. Al **ACEPTAR** nace **un solo cargo** con el total — la decisión de
`docs/modelo-precios.md` §M4 no se tocó: la deuda sigue sin existir hasta que el profe
confirma.

`Pedido` está en producción desde el 17/07/2026 (M4), así que la migración
(`CarritoDePedidos`) se escribió a mano para copiar cada pedido viejo a una línea nueva
antes de borrar las columnas que dejaron de existir — mismo cuidado que
`RenombrarAvisosANoticias`. Se probó contra la base local con pedidos reales antes de
mergear.

**Lo que agrandó el bloque:** el alumno puede **cancelar** su propio pedido mientras siga
Pendiente (`DELETE /portal/pedidos/{id}`) — todavía no generó cargo, así que se borra
directo, sin estado intermedio. Botón "Cancelar" en "Mis pedidos", solo visible en
Pendiente.

---

### Bloque 6 — Plataforma, roles y permisos (15/08/2026)

Los cuatro pedidos que tocaban auth (2, 10, 11 y 12), en el PR #98.

**Pedido 2 — el director habilita al empleado a cobrar.** `MembresiaTenant.PuedeCobrar`
(migración `AgregarPuedeCobrarAMembresiaTenant`), un checkbox en `EditarEmpleadoModal.tsx`.

- Es **un solo permiso**, no dos: habilita clases **y** cuotas juntas. El texto del profe
  decía "una clase o una cuota", pero Finanzas es una pantalla sola y partir el permiso
  obligaba a partirla también.
- Gatea **Finanzas entero**: policy `PuedeCobrar` sobre el `CuotasController` completo
  (`Program.cs`) y `soloConCobro` en la entrada del menú (`nav.ts`). Sin el permiso, el
  empleado no entra a Finanzas en absoluto — no hay entrada parcial.
- **El dueño siempre puede cobrar y no se le puede sacar**: la policy lo deja pasar por
  `rol=owner`, sin mirar membresía (el dueño no tiene una). En `StaffService` su
  `PuedeCobrar` nace en `true`.

> ⚠️ **El permiso viaja en el JWT y el token dura 7 días, sin refresh token.** Dárselo o
> sacárselo a un empleado **no tiene efecto hasta que vuelve a loguearse**. Si el profe
> reporta que "le saqué el permiso y sigue entrando", eso es: que cierre sesión. Vale para
> cualquier permiso que se agregue después — mientras siga siendo un claim, se hereda el
> mismo retardo.

**Pedido 10 — alta de academias desde Plataforma.** `POST /api/admin/clubes`
(`AdminService.CrearClubAsync`): crea el club y la cuenta del director, y lo **salta
directo a Activa, sin pasar por el checkout de Mercado Pago** — reusa
`ActivarTenantAsync`, la misma costura que el webhook real de MP. Devuelve la contraseña
temporal para pasársela al director. Si el celular ya tiene cuenta, corta con un mensaje
propio (el genérico habla de mandar una solicitud desde el portal, que acá no aplica).

**Pedido 11 — el padrón de personas.** `GET /api/admin/personas` +
`features/admin/PersonasPage.tsx`: una proyección de `AspNetUsers` con sus roles por club
(chips Dueño / Staff / Alumno), buscable por nombre, teléfono o mail. La segunda mitad del
pedido ("Alumnos y Espera en Mi academia") **no se hizo a propósito**: ya estaba decidido
que no se reordena el menú del profe.

> **La distinción que hay que sostener** (es la trampa que ya nos mordió una vez): la
> pestaña **"Usuarios"** del profe lista la tabla `Alumnos`, o sea un padrón de **FICHAS**.
> **"Personas"** en Plataforma es el padrón de **PERSONAS** (`AspNetUsers`). No son lo
> mismo y no hay que fusionarlos: el director y los profes no tienen ficha salvo que
> alguien los haya cargado como alumnos, y quien tiene fichas en varios clubes aparece
> varias veces en Alumnos y una sola vez en Personas. Mezclar personas sin ficha en la
> tabla de Alumnos da filas donde la mitad de las acciones no aplican (no hay a quién
> pausar, dar de baja ni cobrarle cuota). Por eso el profe lo pidió en Plataforma. Ver
> `docs/modelo-identidad-roles.md`.

**Pedido 12 — el director como usuario común.** Ya funcionaba con lo que había (probado en
el navegador el 14/08); no hizo falta código nuevo. Lo que sí se sumó, en el PR #99, es que
**el profe entra a su portal tenga ficha o no**: es una persona más y desde ahí se asocia a
un club.

---

### Ranking R.U.T.A. — EN PRODUCCIÓN PERO APAGADO (15/08/2026)

**No sale de la lista de 13.** Es la Fase 3 de `plan-de-accion-v2.md`, que entró junto con
el Bloque 6 en el mismo PR #98 — módulo completo, 6 controllers, 5 migraciones, ~20
pantallas de front.

**Está desplegado y ningún usuario lo ve.** Entró sin haberse probado, así que el mismo día
se puso en pausa: los seis controllers pasaron a policy `Admin`, la entrada del menú lleva
`soloAdmin` y la ruta del portal redirige al Inicio. **Es el pendiente más concreto que
tiene el proyecto: código escrito, desplegado y sin rendir nada.**

**Qué hay construido:**

- **Ranking cross-tenant, a nivel plataforma** (como manda ADR-0007): `JugadorRanking` es
  1:1 con el Usuario global, no con la ficha. Se crea on-demand al inscribirse, con datos
  de perfil opcionales (ciudad, provincia, mano, revés, bio).
- **Singles y dobles**, cada uno con su tabla y su flujo paralelo.
- **Desafíos**: proponer → aceptar/rechazar → cargar quién ganó. Sin resultado en texto
  (no "6-4 6-4"), solo el ganador. Un partido activo por jugador a la vez, y un par de
  jugadores se enfrenta **una sola vez** (índice único sobre el par normalizado).
  Rechazar o cancelar mientras sigue Propuesto **no deja historia**; finalizado, nunca se
  borra.
- **Puntos**: `IPoliticaDePuntosRanking`, pieza intercambiable como `IPoliticaDeCuota`. La
  única implementación (`cf_consolacion_v1`) está marcada **provisoria**: ambos suman, el
  perdedor nunca saca 0.
- **Cierre oficial** los días 1 y 16: congela un snapshot Global + uno por ciudad,
  provincia y país, sobre singles y dobles. Una vez creado es historia, no se edita.
  El desempate es **quién se inscribió antes**, nunca alfabético ni random.
- **Revisiones**: el jugador pide revisar un partido finalizado y le llega a los admins.
  Es un **ticket, no una corrección** — resolverlo solo guarda la respuesta, nunca toca los
  puntos ni el ganador (el service ni siquiera puede escribir esos repos).
- **Notificaciones** (`features/notificaciones`), hoy con el ranking como única fuente —
  por eso la campana también quedó para el admin.

> ⚠️ **El job de cierre oficial SÍ corre en producción.** `RankingCierreOficialJob` es un
> `BackgroundService` con `PeriodicTimer`, y la pausa se aplicó a los controllers, no a él:
> los días 1 y 16 se ejecuta igual. Hoy es inofensivo porque no hay jugadores inscriptos
> (nadie puede inscribirse), así que congela un snapshot vacío. Tenerlo presente al
> habilitar el módulo: el primer cierre real llega solo, sin que nadie lo dispare.

**Cómo se enciende** (está anotado en el propio `RankingController`): sacarle el
`soloAdmin` a la entrada de `nav.ts`, devolver los seis controllers a `[Authorize]` a secas
—ranking, ranking de dobles, desafíos, desafíos de dobles, revisiones y notificaciones— y
abrir la ruta del portal. Ojo con `PortalLayout`, que **no filtraba el nav** (a diferencia
de `AppLayout`): el filtro se le agregó ahí para que el `soloAdmin` hiciera algo.

Hay tests de la lógica (`DesafioServiceTests`, `DesafioDoblesServiceTests`,
`JuegoRevisionServiceTests`, `RankingCierreOficialServiceTests`), pero **nadie lo usó
todavía con datos reales** — que es exactamente lo que falta y el motivo de la pausa.

---

### El Shop de verdad, y corregir un encordado (16/08/2026)

No salió de la lista de 13: son tres cosas que trajo Lucas de **usar** la app.

> *"Ver cómo hacer para que cuando cambia de encordado se pueda actualizar la raqueta."*

El backend ya lo soportaba (`RaquetaService.EditarEncordadoAsync`); faltaba el botón, y del
lado del profe faltaba también el endpoint (`PUT /alumnos/{id}/encordados/{encordadoId}` —
el portal sí lo tenía). `FormEncordado.tsx` sirve para cargar **y** para corregir: no se
duplicó porque la regla del híbrido (las cuerdas horizontales solo si corresponde) se iba a
desincronizar entre las dos copias.

> *"Ver dónde colocar los pedidos para que no se mezcle en una sola pantalla."*

**El Shop entero vive en Mi academia**, que pasó de tres pestañas a cinco: Profesores ·
Sueldos · **Productos** · **Pedidos** · Configuración. El catálogo salió de la tarjeta
`ServiciosCard` de Configuración y la bandeja salió de arriba de Cuotas. La pestaña Pedidos
tiene **badge** con los pendientes (comparte la query key con el aviso del Inicio, así se
piden una vez y bajan juntos) y el aviso del Inicio apunta a `/mi-academia?tab=pedidos`.
En el celular las cinco pestañas **envuelven a dos renglones**: repartidas en una sola fila
quedaban en ~70 px y "Configuración" es una palabra que no se puede cortar.

> *"El profesor debería tener una sección donde pueda cargar sus productos y si quiere fotos.
> La forma en la que hicimos servicios no tiene mucha flexibilidad."*

`Servicio` sumó `Descripcion` y una tabla `FotosServicio` (hasta **5** por producto).

**Las fotos van al storage y en la base queda su URL** — `IAlmacenamientoArchivos`, el mismo
camino que las del perfil del profe, no el base64 en la fila de `Alumno.FotoUrl`. Es la
decisión que hace que esto escale: ~100 caracteres por foto en vez de cientos de KB en cada
carga del catálogo. Se comprime en el navegador antes de subir, se valida que sea imagen
**por sus bytes** (no por lo que declara el cliente) y al borrar la foto se borra el
archivo. `ServicioServiceTests` fija las cuatro cosas.

> El nombre en el código sigue siendo **`Servicio`**: renombrarlo a `Producto` es un rename
> de tabla con datos en producción y no aporta lo suficiente. En pantalla dice *Productos*.

**Decisión abierta, a propósito:** la imagen se sube al storage **antes** de guardar la fila.
Si el guardado en la base falla, el archivo ya está escrito y queda **huérfano** — pasó de
verdad mientras se probaba esto (quedó un `.jpg` suelto en `archivos-locales/productos/` de
un intento que reventó). Se dejó así porque es el mismo comportamiento que las fotos del
perfil del profe y solo ocurre si falla la escritura en la base, que es raro. Si alguna vez
molesta, el arreglo es envolver el `SaveChanges` y borrar el archivo cuando lanza. El caso
inverso —falla el borrado del archivo— **sí** está resuelto: se loguea y no corta la
operación (`ServicioService.BorrarArchivoAsync`), porque un huérfano no lo ve nadie y una
foto rota en la pantalla sí.

**Lo demás que salió en esos dos PRs y no estaba anotado acá:**

- **Cada línea del carrito acepta una aclaración propia** (marca de cuerda, tensión). Va
  por **línea y no por pedido**: mezcladas, el profe tiene que adivinar cuál corresponde a
  qué. La nota **no entra en el concepto del cargo**, que es lo que el alumno ve en su
  cuenta corriente.
- **La publicidad pasó del Inicio al layout del portal**, así se ve en todas las secciones.
  No cuesta consultas de más: la query ya estaba cacheada.
- **El profe entra a su portal tenga ficha o no** (pedido 12: el director es una persona
  más, y desde ahí se asocia a un club). Y el que todavía no está en ningún club **dejó de
  ver una pantalla que tapaba todo**: ahora es una banda arriba del portal completo, igual
  que la del que está en lista de espera.
- **La raqueta acepta un nombre opcional** ("Raqueta 1") para el que tiene dos iguales. Sin
  nombre se sigue mostrando por marca y modelo.

---

### Atajos, permisos y cargarle productos a un alumno (17/08/2026)

Cinco ajustes chicos del uso diario, todos salidos de que el profe usa la app todos los días.

- **Dos accesos directos nuevos** en el Inicio: *Clase suelta* y *Pedidos del Shop*. La
  clase suelta reusa el deep-link que ya existía para "Nuevo horario"
  (`/agenda?tab=calendario&suelta=1`) y es **solo del dueño**, porque el endpoint también.
  Los accesos pasaron de cuatro a seis y la grilla de tres columnas.
- **Se puede editar la ficha de alguien de la lista de espera.** No era una regla, era un
  descuido: `SolicitudesPage` montaba la ficha sin pasarle `onEditar`, y el botón "Editar
  datos" solo se dibuja si esa prop llega. Estar sin horario asignado nunca fue motivo para
  no poder corregirle el teléfono.
- **Atajo de la ficha a Finanzas** (`Ver su cuota →`). La cuota mensual ya se editaba en
  "Editar datos"; lo que faltaba era saltar al **mes** para tocar el monto de ese cargo, que
  es otra cosa.
- **El profe le carga productos a un alumno** eligiendo del catálogo, desde el botón
  **"Agregar cargo" que ya existía en Cuotas**. Nace **Aceptado y con su cargo**: el profe es
  el que resuelve la bandeja, hacerle aceptar su propio pedido sería un paso al pedo. Valida
  que el alumno sea **de su tenant** — el id viene del cliente, a diferencia del portal donde
  sale de la sesión.
  - **Se probó primero como un botón aparte en la ficha del alumno y se dio marcha atrás**:
    eran dos puertas para lo mismo. Ahora "Agregar cargo" pregunta de dónde sale lo que se
    suma —del catálogo, un concepto a mano, o un ajuste— y el catálogo es la opción por
    defecto, que es la que deja el cargo **con su desglose** en vez de un renglón de texto.

**Lo que se reusó en vez de duplicar:** la edición de la ficha salió a un hook propio
(`useEditarAlumno`) que usan las dos pantallas — montar `useAlumnos` entero en la espera
habría traído un listado que esa pantalla no usa. Y en el backend se extrajeron dos privados
de `PedidoService` (armar el pedido, hacer nacer el cargo) que ahora comparten el pedido del
alumno y el que carga el profe.

---

## Lo que falta

**Los 13 pedidos de la lista de agosto están cerrados.** Lo que queda abierto es esto, en
orden de lo que más rinde por lo que cuesta:

### 1. Encender el ranking

Está **construido, desplegado y apagado** desde el 15/08 (ver la sección de arriba). No
hace falta código nuevo para empezar: hace falta **usarlo con datos reales** con la cuenta
de admin —que es justo lo que el `soloAdmin` permite hacer sin que ningún alumno lo vea—
y recién ahí abrirlo. Es el único pendiente que ya está pago.

Ojo con dos cosas al encenderlo: el **job de cierre oficial corre igual** los días 1 y 16
(no está pausado), y la **campana de notificaciones** solo tiene sentido cuando el ranking
esté abierto, porque hoy es su única fuente.

### 2. El módulo de clubes

Bloqueado por una decisión de producto, no por trabajo. Ver la sección de abajo.

### 3. Performance

Los dos pendientes anotados al final de este doc (las fotos de alumno en base64 y los
96 KB de la vista Mes). El segundo es una decisión de producto, no una optimización.

---

### El módulo de clubes (pedido del 17/08/2026)

> *"En mis clubes le debería aparecer los clubes asignados a cada usuario. (…) Automóvil
> Club San Nicolás es un club deportivo con socios y los socios pueden sacar turno en las
> canchas. Además dentro de ese club existe la academia del profesor de tenis, que es una
> entidad aparte, en la cual sus alumnos toman clase EN el club pero en su academia, o sea
> que le rinden cuentas al director de la academia y no al club. (…) También existen los
> clubes que son estrictamente de tenis."*

**No es un pedido nuevo: es un módulo que ya está diseñado y todavía no se construyó.** Casi
todo lo que describe el profe está en [`modelo-identidad-roles.md`](modelo-identidad-roles.md):

| Lo que pide | Dónde ya está decidido |
|---|---|
| El club es una entidad aparte de la academia | §1 y §2 — **`TipoTenant.Club` ya existe en el enum** |
| Los socios pertenecen a uno o varios clubes | §1 y §5 — una membresía por club; tabla `Socio` (Fase 2) |
| Los socios sacan turno en las canchas del club | §3, "El socio que solo juega turnos en su club" |
| Los alumnos toman clase EN el club pero rinden a la academia | §5 — *"la clase ocurre EN un club, pero el alumno ES del tenant del profe"* (relación `ProfesorEnClub`) |

Lo único genuinamente nuevo es el matiz de que hay **clubes multideporte** (donde el tenis es
una actividad más) y **clubes solo de tenis**: se manejan parecido pero no igual.

**DECISIÓN PENDIENTE, y hay que tomarla antes de diseñar nada:** ya existe **Turnos Club**
(`CMR/Reservas_Canchas/Turnos-Club`), una app aparte —Node + Express, multi-club con routing
por slug— que **ya hace reservas de canchas, panel de admin del club y superadmin**. La
pregunta no es cómo modelar el club: es si ese producto se **absorbe** dentro de S.I.D.,
**convive** con él, o se **reescribe**. Contestar eso primero evita construir dos veces lo
mismo. El módulo se planifica aparte, en su propio plan mode.

Hoy "Mis clubes" es un ítem del menú del portal (`nav.ts`, va a `/portal/club`) y el guard de
un solo club por persona sigue en `SolicitudService.CrearAsync`.

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
