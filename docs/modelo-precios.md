# Modelo de precios y cuotas — cómo cobra el profe

> Documento de diseño del dominio de facturación. Capturado de la realidad
> de los clubes donde trabaja Lucas (08/07/2026). Define el ORDEN de
> construcción: Grupos → Turnos → Cuotas. Ver ADR-0008.

---

## 1. Cómo cobra un profesor (la realidad)

- El profe define un **valor hora** de clase.
- Si la clase es **grupal**, el valor hora se **divide entre los presentes
  del turno**: grupo de 4 → cada uno paga ¼ del valor hora.
- Cada alumno tiene una **modalidad de pago**:
  - **Por clase**: paga cada clase que toma.
  - **Mensual**: paga el mes completo (sus clases del mes).
- Lo que un alumno debe **no es un monto fijo**: depende de cuántas clases
  tomó y de cuántos eran en cada una.

## 2. La fórmula

```
Deuda de un alumno en el período =
    Σ  (valorHora ÷ cantidadDeASIGNADOSalTurno)
    por cada turno del período al que estuvo asignado
```

**El divisor son los ASIGNADOS, no los presentes** (corrección de Lucas,
08/07/2026): en un grupo de 4, si falta uno, todos pagan ¼ igual — incluido
el ausente. La cuota se debe del 1 al 10 pase lo que pase.

- **La asistencia NO mueve la plata**: es registro + input para que el
  profe decida recuperaciones. El cargo nace del roster del turno.
- **Recuperación**: decisión discrecional del profe (si el alumno canceló,
  puede reasignarle otro turno). No es automática.

**Consecuencia estructural**: la cuota es HIJA del turno. Sin saber quién
estaba asignado a qué turno y cuántos eran, no hay cálculo automático
posible. Por eso el orden de construcción es:

1. **Grupos** (grupos fijos — el modelo ya existe en la base)
2. **Horarios/Turnos** (la agenda: quién juega, cuándo, cuántos)
3. **Cuotas** (nace calculando sola desde los turnos)

## 3. Decisiones ya tomadas para Cuotas (siguen válidas)

| Tema | Decisión |
|---|---|
| Vencimiento | Día 10 del mes: 1-10 Pendiente, 11+ sin pagar → **Vencida**. El estado se CALCULA (no se guarda — misma lección que `esMenor`) |
| Confirmar pago | Registra **fecha** (server) + **medio**: Efectivo / Transferencia / Otro. Pago total, sin parciales (se evalúa después) |
| Monto | **Snapshot** al generarse el cargo (si el valor hora cambia, los cargos ya generados no se tocan) |

## 4. Decisiones de la vertical Cuotas (resueltas con Lucas)

**RESUELTO (10/07/2026):**

- **Valor hora DUAL** (config del profe): valor hora **grupal** (se divide
  entre los asignados del turno) y valor de clase **individual** (lo paga
  entero el alumno). La individual suele valer distinto por hora.
- **Modalidad de pago por alumno**: `Mensual` (default — liquida el mes,
  vence el 10) o `PorClase` (minoría irregular: paga cargo por cargo; y
  futuros profes podrían cobrar solo así).
- **La cuenta del mes es una CUENTA CORRIENTE de CARGOS**, no solo clases:
  - Cargo **Clase** (auto desde turnos): grupal ÷ asignados o individual entera.
  - Cargo **Producto/Servicio** (manual): encordados, tubos de pelotas, etc.
    Lo carga el profe; a futuro el alumno lo pedirá desde su perfil (portal).
  - Cargo **Ajuste** (manual, monto +/- con motivo): hermanos, beca, redondeo.
- **Turno CANCELADO no genera cargo** (la clase no ocurrió).
- **Idempotencia**: un cargo de clase por (turno, alumno) — nunca duplicado.
- **Monto snapshot**: si el profe cambia los precios, los cargos ya
  generados no se tocan.
- ~~Alumno que falta: ¿paga igual?~~ → paga igual, siempre. El divisor son
  los asignados, no los presentes (ver §2).

**RESUELTO (17/07/2026, servicios + pedidos — M4):**

- ~~¿Cómo pide el alumno un servicio (encordado, tubo) desde el portal?~~ → El
  profe arma su **catálogo de servicios** (`Servicio`: nombre + precio +
  activo, en Configuración; cada profe tiene el suyo). El alumno **pide** uno
  → nace un **`Pedido`** en estado **Pendiente** con snapshot del nombre y
  precio (si el profe cambia el precio después, el pedido conserva lo que el
  alumno vio). **La deuda NO existe todavía.**
- ~~¿Cuándo se le cobra?~~ → **Cuando el profe ACEPTA.** Aceptar el pedido
  genera un `Cargo` tipo Producto (con el snapshot) que entra en la cuenta
  corriente y se cobra con la maquinaria de M2 (informar→confirmar).
  **Rechazar** descarta el pedido sin deuda. Así la cuenta corriente solo
  tiene deudas reales — nunca algo que el profe quizás no hace.
- **Dónde lo resuelve el profe**: un **panel arriba de Cuotas** (mismo patrón
  que el de morosos) — acepta/rechaza ahí, y el cargo aparece en la
  liquidación de abajo. Un contador en el Dashboard avisa cuántos hay
  pendientes. Nada de pantallas nuevas desconectadas de la plata.
- **Baja lógica del servicio** (`Activo=false`): desactivar uno del catálogo
  no rompe los pedidos históricos que lo referencian (snapshot + FK Restrict).
- **Reusa M2**: el cargo que nace del pedido se cobra igual que cualquier
  otro (el alumno informa la transferencia, el profe confirma).

**RESUELTO (17/07/2026, pago informado desde el portal — M2):**

- ~~¿Cómo paga el alumno desde el portal sin Mercado Pago?~~ → **Informa la
  transferencia y el profe confirma.** El alumno transfiere y toca "Ya
  transferí" (el mes completo o un cargo puntual); el cargo pasa a un estado
  intermedio **informado** (`Cargo.PagoInformadoEl`), que NO es pagado: la
  plata sigue impaga hasta que el profe la ve. El profe **confirma** (= pagar,
  `PagadoEl`) o **rechaza** ("no me llegó" → vuelve a impago sin informar). La
  verdad de la plata la sigue poniendo el profe, nunca el cliente — y deja el
  terreno listo para que, con MP real, la confirmación sea un webhook.
- **Estado "Informado"** (calculado, nunca guardado): una liquidación con
  saldo cuyos impagos están TODOS informados muestra "Informado" (tapa a
  "Vencida": ya no es un moroso silencioso, hay una acción del profe
  pendiente). Convive con Pagada/Pendiente/Vencida. **La morosidad
  (`TieneDeudaVencida`, `DebeSuspenderse`) sigue midiéndose por `PagadoEl`**:
  informar no descuenta deuda hasta que el profe confirme.
- **Datos de transferencia** (`Tenant.AliasCbu` + `TitularPago`, en
  Configuración): el portal se los muestra al alumno en el modal de "Ya
  transferí" para que sepa a dónde mandar la plata.
- **Fuera de alcance (queda para M4)**: que el alumno se **auto-agregue** un
  servicio (encordado, tubo) desde un catálogo con precios precargados del
  profe. M2 solo permite informar el pago de cargos que YA existen (la clase,
  el mes, o un producto que el profe cargó a mano). M4 reusa esta misma
  maquinaria informar→confirmar.

**RESUELTO (16/07/2026, estado del alumno ↔ calendario):**

- ~~¿Qué pasa con los turnos de un alumno pausado o dado de baja?~~ → **El
  estado manda sobre el calendario**. Antes no pasaba nada: el pausado seguía
  en sus turnos futuros, se le generaban cargos de clases a las que no iba y
  —peor— al contar en `turno.Participantes.Count` **abarataba la clase al
  resto** (÷4 en vez de ÷3). Ahora:
  - **Pausado** (lesión, viaje): sale de sus turnos futuros y sus cargos
    IMPAGOS de esos turnos se borran (no va → no paga). Se le **guarda el
    lugar**: sigue en sus grupos y su horario individual queda activo. Al
    reactivarlo **vuelve solo** a los turnos futuros de sus grupos (los
    individuales los regenera la generación perezosa).
  - **Baja**: lo mismo + **libera el lugar** (sale de sus grupos → el cupo
    queda disponible; sus horarios individuales se desactivan → la cancha se
    libera). Si vuelve, el profe lo reasigna.
  - **Lo PAGADO es intocable**: un turno futuro con su cargo ya pagado no se
    toca (mismo criterio que `HorarioService.DesactivarAsync` y bloqueos).
  - La **generación perezosa** ya no incluye en el roster a quien no esté
    `Activo` (ni genera el turno individual de un pausado).
  - **Entrar o salir de un grupo también sincroniza el calendario**: al
    sumar a un alumno a un grupo, se lo repone en los turnos futuros YA
    generados de ese grupo (y se recalcula el divisor); al quitarlo, se lo
    saca de esos turnos. Antes esto se olvidaba y el que volvía tras una baja
    no aparecía en el calendario ni se le generaba cuota (bug de Lucas,
    16/07/2026). Toda la lógica vive en un solo lugar
    —`AlumnoService.SincronizarCalendarioAsync`, la reconciliación
    idempotente del estado↔calendario— que llaman por igual el cambio de
    estado, la baja y `GrupoService` al tocar una membresía. Un solo lugar =
    el agujero no se repite.
- ~~¿La morosidad saca del calendario?~~ → **Día 15 (5 días de gracia después
  del vencimiento del 10): NO es automático.** El profe ve un panel en Cuotas
  con los morosos y decide: puede recordarles por WhatsApp o **sacarlos del
  calendario** (los pausa, con todo lo de arriba). Cuando pagan, los reactiva
  y vuelven. Regla: `CuotaService.DebeSuspenderse(cargosImpagos, hoy)` con
  `DiaSuspension = 15` (convive con `DiaVencimiento = 10`, que sigue
  bloqueando altas nuevas y pintando las señales visuales).

**RESUELTO (13/07/2026, vertical Cancelaciones):**

- ~~¿Qué pasa cuando el ALUMNO cancela desde el portal?~~ → Es un **AVISO**
  (falta con aviso): el turno sigue en pie para el resto, **el divisor NO
  cambia y su cargo queda** — coherente con "la asistencia no mueve la
  plata". Se registra en su participación (`TurnoParticipante.CanceloEl` +
  motivo) y lo marca ausente. Puede avisar **hasta la hora de inicio** (sin
  mínimo de anticipación: como no mueve plata, solo informa). La
  recuperación sigue siendo a discreción del profe (ve el aviso en la
  pantalla Cancelaciones, con WhatsApp para coordinar).
- ~~Turno cancelado ENTERO, ¿y el cargo ya generado?~~ → Cancelación del
  profe (a mano o vía bloqueo) = la clase no ocurre = **nadie paga**: los
  cargos IMPAGOS del turno se eliminan; los ya PAGADOS no se tocan (plata
  cobrada es intocable). `Turno.CanceladoPor` registra quién canceló.

**Pendientes (siguen abiertos):**

- [ ] Pagos parciales de una liquidación mensual (hoy: no).
- [ ] **Recuperación y el divisor del turno destino**: hipótesis NO cambia
      el divisor ni genera cargo (Juan ya pagó su clase original).
      Confirmar con la realidad.
- [ ] Pedido de productos desde el perfil del alumno (llega con el portal).
