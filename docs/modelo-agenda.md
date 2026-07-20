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
- **Temporada**: los grupos y horarios se arman una vez y duran la
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
| **Horario** | Plantilla RECURRENTE semanal | "Intermedios, martes 18:00, 60', Cancha 1" |
| **Turno** | Instancia CONCRETA en una fecha | "mar 14/07 18:00, roster: Juan, Sofía, Mateo, Vale" |
| **Participante** | Alumno en el roster de un turno + su asistencia | "Mateo — FALTÓ" |

Un horario apunta a un **grupo** (clase grupal) **o** a un **alumno**
(clase individual) — exactamente uno de los dos.

## 3. Reglas (las del TDD)

1. **Solapamiento por cancha**: dos horarios no pueden superponerse en la
   misma cancha (día + rango horario). En canchas distintas, sí.
2. **Generación perezosa e idempotente**: al consultar una semana se
   materializan los turnos que falten desde los horarios activos; los ya
   existentes no se tocan (los pasados son historia intocable).
3. **Roster congelado al generar**: el turno copia los miembros activos del
   grupo (o el alumno individual) al momento de generarse. Ese roster fija
   el divisor del precio (`modelo-precios.md`): cambios posteriores del
   grupo NO tocan turnos ya generados.
4. **Asistencia default-presente**: todos los del roster figuran presentes;
   el profe marca solo al que faltó. No mueve la plata (registro + input
   para recuperaciones).
5. **Cancelación de turno**: el turno se marca Cancelado (con motivo y
   quién), nunca se borra. El horario sigue vigente para las próximas semanas.
6. **Turnos pasados no se regeneran** al cambiar un horario; los futuros no
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
| **1. Grupal fija** | Se suma a un **grupo** existente | Semanal | Mensual, `valorHora ÷ asignados` |
| **2. Individual fija** | **Horario propio** (él solo) en un día/hora | Semanal | Mensual, `valorClaseIndividual` |
| **3. Clase suelta** | **UNA** clase (probar/esporádico) | No | Paga en el momento, cada vez |

**M5a — Grupal fija (implementado):**

- El alumno ve los **grupos disponibles**: activos, **con cupo**, y de **su
  categoría** (grupo sin categoría asignada = abierto a todos; con categoría =
  debe coincidir — la regla que estaba pendiente, ahora sí aplica al
  auto-pedido). Cada horario muestra su **precio estimado por clase** =
  `valorHoraGrupal × (duración/60) ÷ (miembros + el alumno)`, así ve cuánto
  pagaría ya contándose (÷2/3/4).
- Pide sumarse → `SolicitudGrupo` **Pendiente**. El profe la ve en un panel en
  **Grupos** y la **acepta** (lo suma vía `AsignarAlumnoAsync`, que reconcilia
  el calendario) o la **rechaza**. El profe mantiene el control de quién entra.
- La categoría del grupo filtra el **auto-pedido del alumno**, NO la asignación
  manual del profe (el profe sigue armando sus grupos libre).

**M5b — Individual fija (implementado):**

- El alumno **elige la SEDE** (el lugar) + propone día + hora + duración (no
  elige cancha). El sistema valida: alumno activo, sin deuda vencida, **al menos
  una cancha libre EN ESA SEDE** a esa hora (reusa `ListarPorCanchaYDiaAsync`), y
  no duplicar un pedido igual pendiente. Crea `SolicitudHorario` **Pendiente**
  (con `SedeId`).
- El **profe** ve las solicitudes en un panel en **Horarios** (con la sede que
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

**M5c (clase suelta): pendiente.** Es el único turno que no cuelga de un horario
recurrente (una fecha puntual) + pago en el momento — se resuelve al construirlo.
La pantalla "Reservar" ya la deja como "próximamente".
