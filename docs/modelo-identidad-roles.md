# Modelo de identidad y roles — el mapa del producto

> Documento de diseño conceptual. Responde CÓMO se registran y conviven
> profesores, clubes, alumnos y socios en la plataforma. Las próximas
> verticales no deben violar este mapa. Decisión registrada en ADR-0007.
> Última actualización: 05/07/2026

---

## 1. El principio: separar PERSONA de RELACIÓN

La trampa clásica sería que un usuario "SEA alumno O socio O profesor". En
este dominio no funciona: la misma persona toma clases con dos profesores,
es socia de dos clubes, y eso **cambia con el tiempo**. Por eso:

- **Usuario** (global, plataforma): una persona real. Se registra UNA vez,
  **gratis**, con email/teléfono. No "es" nada por sí mismo: es identidad.
- **Tenant** (el negocio que paga la suscripción): el universo de datos de
  un PROFESOR o un CLUB (`TipoTenant` ya soporta ambos).
- **Membresía** (relación persona ↔ tenant): lo que la persona ES dentro de
  cada universo. Una persona tiene N membresías a la vez.

```
                     ┌─ Alumno en el tenant del profe Juan   (tabla Alumno, HOY)
Usuario "Lucas" ─────┼─ Alumno en el tenant de la profe Ana  (otro registro, mismo UserId)
                     ├─ Socio del Club A                     (tabla Socio, Fase 2)
                     ├─ Socio del Club B
                     └─ Staff en el tenant de un head pro    (rol en tenant, futuro)
```

**Analogía**: Slack/Discord — una cuenta, N workspaces, rol distinto en cada
uno. O Mercado Libre: la misma cuenta compra en un lado y vende en otro.

La tabla `Alumno` actual **ya es** una membresía (la primera): un registro
dentro del universo de un profesor, con `UserId` nullable para vincular a la
persona cuando se registre (patrón de `modelo-alumnos.md` §3.4). Nada de lo
construido cambia; todo lo que sigue es aditivo.

## 2. Quién paga y quién no

| Actor | Paga | Qué obtiene |
|---|---|---|
| Profesor | Suscripción mensual (MP) | Su tenant: alumnos, horarios, cuotas, reportes |
| Club | Suscripción mensual (MP) | Su tenant: canchas, horarios, socios, turnos |
| Jugador (alumno/socio) | **Gratis** | Su cuenta: sus clases, sus turnos, ranking, tienda |

Los jugadores gratis son la red que hace valioso el producto (ranking,
marketplace); el ingreso viene de los negocios. Ya estaba decidido en el
plan v2 §7 ("Alumnos: registro gratuito siempre") — acá se generaliza.

## 3. Flujos de registro

### Profesor
Landing → **"Soy profesor"** → registro + checkout Mercado Pago → nace su
tenant `PENDIENTE_PAGO` → webhook MP lo pasa a `ACTIVO` (plan v2 §7).

### Club (Fase 2)
Ídem, con `TipoTenant.Club`.

### Jugador
Registro **gratis** (email/teléfono). La vinculación con negocios tiene dos
caminos, y se soportan AMBOS:

1. **Reclamo de ficha** — el negocio lo cargó primero (como el profe carga
   alumnos hoy): existe la ficha sin usuario. Al registrarse la persona, el
   sistema busca coincidencias por DNI/teléfono dentro de cada tenant y le
   propone reclamarlas → se completa `UserId`. Cero migración.
2. **Solicitud / invitación** — el jugador busca el club/profe en la app y
   solicita unirse (el negocio aprueba), o entra directo con un código de
   invitación que el negocio le comparte.

### El socio que "solo juega turnos en su club"
Se registra gratis como jugador → se vincula a su club (reclamo o
solicitud) → ve las canchas y horarios que administra SU club → saca turno.
Si es socio de dos clubes, tiene dos membresías y elige dónde reservar.
La cuota social la administra el club (su módulo de socios, Fase 2).

## 4. La página principal y la experiencia por membresías

- **Pública (sin login)**: marketing del producto + login único + registro
  segmentado: "Soy profesor" / "Soy club" / "Juego o entreno".
- **Jugador logueado**: home personal — mis clases, mis turnos, mi cuota,
  **ranking** y **tienda**.
- **Profesor/club logueado**: el dashboard de gestión (lo ya construido).
- **Múltiples membresías** (es profe Y juega, o gestiona un club Y entrena):
  selector de contexto, como cambiar de workspace en Slack.

### Ranking y Marketplace son de PLATAFORMA
No viven dentro de ningún tenant: cruzan todos los clubes y profesores, en
el espacio del usuario logueado. El ranking es global ("todo usuario de la
app participa", plan v2 §1) — por eso necesita la identidad global: los
puntos son de la PERSONA, no de su ficha en un club.

## 5. Casos borde (los que motivaron este documento)

- **Alumno de N profes + socio de M clubes, y cambia en el tiempo**: cada
  vínculo es una membresía independiente; se crea o se baja sin tocar las
  demás. No existe "tipo de usuario" que migrar.
- **Profesor que trabaja en varios clubes**: su negocio (alumnos, cuotas) es
  SU tenant y viaja con él. Una relación `ProfesorEnClub` (futura) lo vincula
  con cada club (qué canchas/horarios usa). La clase ocurre EN un club, pero
  el alumno ES del tenant del profe. ("Horarios multi-club" del plan.)
- **Head pro con profesores a cargo**: los profes a cargo son usuarios con
  rol **Staff dentro del tenant** del head pro (ven agenda, cargan
  asistencia; el negocio y la facturación son del head pro). Si uno se
  independiza, crea su propio tenant — y ambas cosas coexisten porque la
  identidad es global.
- **La misma persona en dos universos**: por eso el DNI es único POR TENANT
  y no global (`modelo-alumnos.md` §3.1) — la ficha de Lucas en lo de Juan y
  la ficha de Lucas en el Club A son registros distintos, unidos por su
  `UserId` cuando reclama ambas.

## 6. Qué NO se modela todavía

Para no sobre-diseñar: las tablas `Usuario`, `Socio`, `ProfesorEnClub` y los
roles `Staff` se construyen recién en sus fases (auth y Fase 2 clubes). Este
documento es el **contrato conceptual** que garantiza que lo que se
construya hoy no las bloquee. El schema actual ya cumple:

| Pieza actual | Por qué ya es compatible |
|---|---|
| `Alumno.UserId` (nullable) | El patrón "reclamar ficha" listo desde el día 1 |
| `@@unique(TenantId, Dni)` | La misma persona puede existir en N tenants |
| `TipoTenant.Club` | El enum ya contempla la Fase 2 |
| Repos scopeados por tenant | El aislamiento entre universos ya es regla |
