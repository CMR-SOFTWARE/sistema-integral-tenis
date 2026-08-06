# Modelo de agenda — sedes, horarios y turnos

> Documento de diseño de la vertical Horarios/Turnos. Capturado de la
> realidad del profe (08/07/2026). Complementa `modelo-precios.md`
> (la cuota nace del turno).

---

## 1. La realidad que modelamos

- El profe trabaja en **varias sedes** (sus clubes: hoy 2, uno con 2
  canchas). ⚠️ No confundir con los clubes-tenant de la Fase 2: las sedes
  son parte del negocio DEL PROFE (plan v1: "horarios multi-club").
- Tiene **profes a cargo** → puede haber **turnos en simultáneo** en
  canchas distintas. La regla de solapamiento es **por cancha**, no por
  profe. (Quién dicta cada turno se modela cuando llegue Staff/ADR-0007.)
- **Temporada**: los horarios se arman una vez y duran la
  temporada (otoño/invierno/primavera); en verano se rearman. El cambio de
  temporada = editar/desactivar horarios y crear los nuevos. No se modela
  "Temporada" como entidad (por ahora — no sobre-modelar).
- Cada **alumno pertenece a una sede** (informativo, para filtrar y
  organizar; opcional).

## 2. Conceptos

| Concepto | Qué es | Ejemplo |
|---|---|---|
| **Sede** | Club/lugar donde trabaja el profe | "Club Atlético Norte" |
| **Cancha** | Cancha dentro de una sede | "Cancha 1" |
| **Horario** | Plantilla RECURRENTE semanal, con su cupo y su gente | "Intermedios, martes 18:00, 60', Cancha 1, cupo 4" |
| **Miembro** | Alumno en el roster de un horario, con historia | "Pepe, de marzo a junio" |
| **Turno** | Instancia CONCRETA en una fecha | "mar 14/07 18:00, roster: Juan, Sofía, Mateo, Vale" |
| **Participante** | Alumno en el roster de un turno + su asistencia | "Mateo — FALTÓ" |

**El horario es la unidad** (05/08/2026): tiene su `Nombre` (opcional — si va vacío
el título se arma solo), su `CupoMaximo` (null = sin límite), su `Categoria`
sugerida y su propio roster (`AlumnoHorario`, con `FechaAlta`/`FechaBaja`). Una
clase particular ya no es un caso especial: es un horario con un solo alumno.

Antes había **dos conceptos para lo mismo**: el horario apuntaba a un `Grupo`
(clase grupal) **o** a un `Alumno` (individual), un XOR validado a mano, con el
cupo y el roster viviendo en el grupo. Eso se fue: los grupos existentes se
migraron al roster de cada uno de sus horarios.

## 3. Reglas (las del TDD)

1. **Solapamiento por cancha**: dos horarios no pueden superponerse en la
   misma cancha (día + rango horario). En canchas distintas, sí.
2. **Generación perezosa e idempotente**: al consultar una semana se
   materializan los turnos que falten desde los horarios activos; los ya
   existentes no se tocan (los pasados son historia intocable).
3. **Roster congelado al generar**: el turno copia los miembros activos del
   horario al momento de generarse. Ese roster fija el divisor del precio
   (`modelo-precios.md`): cambios posteriores del horario NO tocan turnos ya
   generados. Un horario **sin alumnos no genera turnos**.
4. **Cupo**: no entra nadie si el roster llegó a `CupoMaximo`, y el cupo no
   puede bajar por debajo de los que ya vienen. Sumar a alguien de la lista de
   espera es lo que lo convierte en alumno; el que tiene la **cuota vencida**
   no entra a clases nuevas.
5. **Asistencia default-presente**: todos los del roster figuran presentes;
   el profe marca solo al que faltó. No mueve la plata (registro + input
   para recuperaciones).
6. **Cancelación de turno**: el turno se marca Cancelado (con motivo y
   quién), nunca se borra. El horario sigue vigente para las próximas semanas.
7. **Turnos pasados no se regeneran** al cambiar un horario; los futuros no
   jugados de ese horario sí pueden regenerarse.

## 4. Qué NO entra en esta vertical

- Recuperaciones (decisión del profe — llega con Cuotas o después).
- ~~Reservas por parte del alumno~~ → **resuelto en M5** (ver §5).
- Asignar qué profe dicta cada turno (Staff, ADR-0007).
- Bloqueos de agenda y la pantalla Cancelaciones del mockup (vertical propia).

## 5. Reservar clases desde el portal (M5, 17/07/2026)

El alumno pide clases desde el portal. Hay **3 tipos de clase**:

| Tipo | Qué es | Recurrencia | Pago |
|---|---|---|---|
| **1. Lugar en una clase** | Se suma a un **horario con cupo** existente | Semanal | Mensual, `valorHora ÷ asignados` |
| **2. Individual fija** | **Horario propio** (él solo) en un día/hora | Semanal | Mensual, `valorClaseIndividual` |
| **3. Clase suelta** | **UNA** clase (probar/esporádico) | No | Paga en el momento, cada vez |

**M5a — Lugar en una clase (implementado):**

- El alumno ve las **clases disponibles**: activas, **con lugar**, y de **su
  categoría** (clase sin categoría asignada = abierta a todos; con categoría =
  debe coincidir). Cada una muestra su **precio estimado por clase** =
  `valorHoraGrupal × (duración/60) ÷ (miembros + el alumno)`, así ve cuánto
  pagaría ya contándose (÷2/3/4). La grilla del portal es **una clase por
  slot**: antes había que aplanar grupo → sus horarios.
- Pide sumarse → `SolicitudCupo` **Pendiente**. El profe la ve en un panel
  arriba del **Calendario** y la **acepta** (lo suma vía
  `HorarioService.AgregarAlumnoAsync`, que revalida cupo/estado/deuda y
  reconcilia el calendario) o la **rechaza**. El profe mantiene el control de
  quién entra.
- La categoría de la clase filtra el **auto-pedido del alumno**, NO la
  asignación manual del profe (el profe sigue armando sus clases libre).

**M5b — Individual fija (implementado):**

- El alumno **elige la SEDE** (el lugar) + propone día + hora + duración (no
  elige cancha). El sistema valida: alumno activo, sin deuda vencida, **al menos
  una cancha libre EN ESA SEDE** a esa hora (reusa `ListarPorCanchaYDiaAsync`), y
  no duplicar un pedido igual pendiente. Crea `SolicitudHorario` **Pendiente**
  (con `SedeId`).
- El **profe** ve las solicitudes en un panel arriba del **Calendario** (con la sede que
  pidió el alumno), elige una **cancha libre de esa sede** (dropdown de
  `CanchasLibresParaSolicitudAsync`) y **acepta**: se crea el `Horario`
  individual vía `HorarioService.CrearAsync`. O **rechaza**.
- La **sede en el pedido** resuelve la confusión de clubes con varias sedes: el
  chequeo de disponibilidad ("✓ Hay lugar en {sede}") es **por sede**, así el
  alumno sabe exactamente dónde pide. (Nota de fondo: hoy el tenant es la
  academia/profe y las sedes cuelgan de él; a futuro habría que revisar el
  modelo club↔academia — ver la memoria del proyecto.)
- En el portal, la validación en vivo es del lado del server (por si dos piden
  el mismo hueco); ya no se muestra el calendario de "ocupación" (confundía con
  varias sedes/canchas).

**M5c — Clase suelta (implementado):**

- Una clase individual en una **fecha puntual** (probar/esporádico). Es el
  primer turno que **no cuelga de un horario recurrente**: `Turno.HorarioId`
  pasó a ser **opcional** (null = suelto). La generación perezosa y la
  liquidación **saltean** los sueltos (su cargo nace en la confirmación).
- **Flujo (pago primero, después se habilita):**
  1. El alumno reserva: sede + **fecha** + hora + duración. Se valida cancha
     libre en esa sede ESA fecha (recurrentes del día de la semana **+** otros
     sueltos de esa fecha). Nace la `ClaseSuelta` **Pendiente** + un **Cargo**
     (precio individual, impago).
  2. El alumno **informa el pago** del cargo (reusa M2).
  3. El **profe confirma** desde un panel en **Calendario**: elige una cancha
     libre → nace el **turno suelto** (con el alumno), se marca **pagado** el
     cargo → **Confirmada** (clase habilitada). O **rechaza** (se borra el
     cargo; la clase queda como historia con `CargoId` null).
- El turno suelto aparece en el calendario del profe ("Nombre (suelta)") y en
  "Mis turnos" del alumno. `ClaseSuelta.CargoId` es nullable justo para poder
  rechazar sin arrastrar la clase.

**M5 completo** (lugar en una clase, individual fija, clase suelta). Falta MP
real (el pago hoy es informar→confirmar) — futuro.

## 6. Cambios

- **05/08/2026 — el horario pasa a tener cupo y su propia lista de alumnos.** Se
  eliminó el concepto `Grupo` (y la pestaña Grupos): el cupo, la categoría y el
  roster viven en el `Horario`. La gestión del roster vive en el calendario: se
  toca la clase y ahí se suman o sacan alumnos. `SolicitudGrupo` →
  `SolicitudCupo`. Las tablas `Grupos`/`AlumnoGrupos`/`SolicitudesGrupo` quedan
  en la base como red de seguridad hasta el PR de limpieza.
