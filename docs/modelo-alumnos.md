# Modelo de Datos — Gestión de Alumnos

> Documento de diseño. Fase 1, módulo de alumnos.
> Última actualización: 12/06/2026

---

## 1. Decisiones que definen el diseño

| Decisión | Elección | Consecuencia en el schema |
|---|---|---|
| Multi-tenant | Desde el día 1 | `tenantId` en TODAS las tablas, índices compuestos, unicidad por tenant |
| Login de alumnos | Más adelante | `Alumno` es entidad de datos, NO usuario. Campo `userId` nullable para vincular después sin migración |
| Tipos de clase | Individuales + grupales | Modelo `Grupo` para grupos fijos. Los grupos "armados por clase" NO se modelan acá (van en el booking, Fase turnos) |
| Menores de edad | Sí, hay | Tabla `Tutor` separada + consentimiento con timestamp |
| Categorías | 7ma a 1ra | Enum `CategoriaAlumno` (+ `SIN_CATEGORIA` para nuevos) |

---

## 2. Schema Prisma

```prisma
// ─────────────────────────────────────────────
// ENUMS
// En SQL Server esto sería un CHECK constraint
// o una tabla lookup. Prisma lo genera como
// tipo nativo de Postgres (CREATE TYPE ... AS ENUM)
// ─────────────────────────────────────────────

enum TipoTenant {
  PROFESOR
  CLUB        // Fase 2, pero el enum ya lo soporta
}

enum CategoriaAlumno {
  PRIMERA     // la mejor
  SEGUNDA
  TERCERA
  CUARTA
  QUINTA
  SEXTA
  SEPTIMA     // la inicial
  SIN_CATEGORIA  // alumno nuevo, todavía no evaluado
}

enum EstadoAlumno {
  ACTIVO      // al día, puede reservar
  SUSPENDIDO  // no pagó → se bloquea reserva, NO se borra (regla del plan de acción 1.8)
  INACTIVO    // dejó de venir, se conserva el historial
}

enum RelacionTutor {
  PADRE
  MADRE
  TUTOR_LEGAL
  OTRO
}

// ─────────────────────────────────────────────
// TENANT
// El "dueño" de todos los datos. Equivale a tener
// una base de datos por cliente en SQL Server,
// pero implementado como columna + RLS.
// ─────────────────────────────────────────────

model Tenant {
  id         String     @id @default(cuid())  // cuid ≈ UNIQUEIDENTIFIER pero ordenable
  subdominio String     @unique               // juanperez.acepro.com
  nombre     String
  tipo       TipoTenant @default(PROFESOR)
  activo     Boolean    @default(true)
  creadoEl   DateTime   @default(now())

  // Relaciones (en SQL Server: las FK apuntan PARA ACÁ)
  alumnos    Alumno[]
  tutores    Tutor[]
  grupos     Grupo[]
}

// ─────────────────────────────────────────────
// ALUMNO
// Entidad de datos pura: NO tiene password ni login.
// Cuando en el futuro el alumno se registre,
// se vincula vía userId (nullable hoy).
// ─────────────────────────────────────────────

model Alumno {
  id        String   @id @default(cuid())
  tenantId  String
  tenant    Tenant   @relation(fields: [tenantId], references: [id])

  // ── Datos personales ──
  nombre          String
  apellido        String
  dni             String
  email           String?              // opcional: hay gente sin email
  telefono        String               // formato E.164 (+549...) → lo necesita Twilio
  fechaNacimiento DateTime             // NO guardamos "esMenor": se calcula
  fotoUrl         String?              // Supabase Storage

  // ── Datos deportivos ──
  categoria CategoriaAlumno @default(SIN_CATEGORIA)

  // ── Ciclo de vida ──
  estado    EstadoAlumno @default(ACTIVO)
  notas     String?      // observaciones del profe ("lesión de hombro", etc.)

  // ── Consentimientos (Ley 25.326) ──
  // No alcanza un boolean: hay que poder demostrar CUÁNDO consintió
  consentimientoWhatsapp   Boolean   @default(false)
  consentimientoWhatsappEl DateTime?
  consentimientoDatos      Boolean   @default(false)  // si es menor, lo da el tutor
  consentimientoDatosEl    DateTime?

  // ── Tutor (solo menores) ──
  tutorId   String?
  tutor     Tutor?   @relation(fields: [tutorId], references: [id])

  // ── Futuro login (no se usa todavía) ──
  userId    String?  @unique

  // ── Auditoría ──
  creadoEl      DateTime @default(now())
  actualizadoEl DateTime @updatedAt

  // ── Relaciones ──
  grupos    AlumnoGrupo[]

  // El DNI es único POR TENANT, no global:
  // la misma persona puede ser alumna de dos profes distintos.
  // En SQL Server: CREATE UNIQUE INDEX ... ON Alumno(tenantId, dni)
  @@unique([tenantId, dni])

  // El query más frecuente va a ser "alumnos activos de este profe"
  @@index([tenantId, estado])
}

// ─────────────────────────────────────────────
// TUTOR
// Tabla separada (no campos en Alumno) porque:
// 1. Un tutor puede tener varios hijos alumnos (hermanos)
// 2. Mantiene Alumno limpio para los mayores (sin 5 columnas NULL)
// ─────────────────────────────────────────────

model Tutor {
  id        String        @id @default(cuid())
  tenantId  String
  tenant    Tenant        @relation(fields: [tenantId], references: [id])

  nombre    String
  apellido  String
  dni       String
  telefono  String        // a este número van los avisos del menor
  email     String?
  relacion  RelacionTutor

  alumnos   Alumno[]      // 1 tutor → N alumnos (hermanos)

  creadoEl  DateTime @default(now())

  @@unique([tenantId, dni])
}

// ─────────────────────────────────────────────
// GRUPO (solo los FIJOS)
// "Los pibes de los martes 18hs" — grupo que se
// repite. Los grupos armados clase por clase se
// resuelven en la fase de turnos (participantes
// del booking), acá NO. No sobre-modelar.
// ─────────────────────────────────────────────

model Grupo {
  id         String  @id @default(cuid())
  tenantId   String
  tenant     Tenant  @relation(fields: [tenantId], references: [id])

  nombre     String           // "Intermedios martes"
  categoria  CategoriaAlumno? // sugerida, para armar grupos parejos
  cupoMaximo Int?             // null = sin límite
  activo     Boolean @default(true)

  alumnos    AlumnoGrupo[]

  creadoEl   DateTime @default(now())

  @@index([tenantId, activo])
}

// ─────────────────────────────────────────────
// ALUMNO ↔ GRUPO (many-to-many con historia)
// Tabla intermedia EXPLÍCITA (no implícita de Prisma)
// porque necesitamos fechaAlta/fechaBaja:
// "Pepe estuvo en este grupo de marzo a junio"
// ─────────────────────────────────────────────

model AlumnoGrupo {
  alumnoId  String
  alumno    Alumno   @relation(fields: [alumnoId], references: [id])
  grupoId   String
  grupo     Grupo    @relation(fields: [grupoId], references: [id])

  fechaAlta DateTime @default(now())
  fechaBaja DateTime?  // null = sigue en el grupo

  @@id([alumnoId, grupoId])
}
```

---

## 3. Decisiones de diseño explicadas

### 3.1 Unicidad por tenant, no global
`@@unique([tenantId, dni])` y no `dni @unique`. Si mañana el mismo jugador toma clases con dos profes, son dos registros `Alumno` distintos. Es contraintuitivo viniendo de sistemas single-tenant, pero es LA regla de oro del multi-tenant: cada tenant es un universo cerrado.

### 3.2 `esMenor` NO existe como columna
Se calcula desde `fechaNacimiento`. Un boolean guardado se vence solo (el alumno cumple 18 y el dato queda mentiroso). **Regla a nivel aplicación**: si `edad < 18` → `tutorId` y `consentimientoDatos` (del tutor) son obligatorios. Prisma no puede forzar esto (en SQL Server tampoco alcanzaría un CHECK simple porque cruza tablas) → se valida con Zod en el server action de alta.

### 3.3 Consentimientos con timestamp
`Boolean` solo no sirve legalmente. La Ley 25.326 exige poder demostrar que el consentimiento existió y cuándo. Por eso cada consentimiento tiene su `...El DateTime?`. Más adelante (cuando haya login) se puede agregar quién lo otorgó.

### 3.4 `userId` nullable desde hoy
Hoy es `null` siempre. Cuando en una fase futura los alumnos tengan login, el flujo será: alumno se registra → se busca su `Alumno` por DNI/teléfono dentro del tenant → se vincula. **Cero migración de schema.** Esto es diseñar para el futuro sin construir el futuro.

> **Generalizado (05/07/2026)**: este patrón de "reclamar ficha" quedó elevado a principio del producto para TODAS las membresías (alumnos, socios de clubes, staff) en `modelo-identidad-roles.md` (ADR-0007).

### 3.5 Grupos ad-hoc NO se modelan acá
Tentación clásica: meter todo. Los grupos que se arman clase por clase son una propiedad del **turno** (qué alumnos participan de ese booking), no del alumno. Cuando hagamos el módulo de turnos, el booking tendrá sus participantes. Si lo modeláramos acá duplicaríamos el concepto.

### 3.6 `SUSPENDIDO ≠ borrado`
Viene directo del plan de acción (1.8): si no paga, se suspende el acceso a reservas pero el registro y su historial quedan. `INACTIVO` es para el que dejó de venir. El borrado real solo existe para el derecho al olvido (Ley 25.326), y es una operación administrativa explícita, nunca un flujo normal.

---

## 4. Paralelo SQL Server ↔ Prisma (resumen)

| Concepto SQL Server | Acá |
|---|---|
| `CREATE TABLE` | `model` |
| `IDENTITY` / `NEWID()` | `@default(cuid())` |
| `UNIQUE INDEX (a, b)` | `@@unique([a, b])` |
| `CHECK (col IN (...))` | `enum` |
| FK + `REFERENCES` | `@relation(fields, references)` |
| Trigger de `updated_at` | `@updatedAt` (lo maneja Prisma) |
| Tabla intermedia N:M | model explícito con `@@id` compuesto |

---

## 5. Pendientes / preguntas para Eduardo

- [ ] **Categorías 7ma→1ra vs los 4 tiers del ranking**: `ranking-sistema.md` define Primera/Segunda/Tercera/Sin categoría. Hay que unificar criterio antes de la Fase 3. ¿El ranking usa las 7 categorías o las colapsa?
- [ ] ¿El profe quiere registrar **fecha de inicio** del alumno (antigüedad) por separado de `creadoEl`? (puede haber alumnos previos al sistema)
- [ ] ¿Hace falta un campo de **arancel/plan** por alumno ahora, o eso vive 100% en el módulo de pagos?
- [ ] Menores: ¿alcanza UN tutor por alumno o necesitamos los dos padres? (hoy: uno solo)

---

## 6. Próximo paso

1. Validar este modelo
2. `prisma migrate dev` con estos modelos en un proyecto limpio
3. Seed de datos de prueba (alumnos ficticios, un menor con tutor, un grupo)
4. Server action de alta de alumno con validación Zod (regla del menor)

---

## 7. Perfil editable por el alumno (M3, 17/07/2026)

El alumno administra parte de su propia ficha desde el portal:

- **Editable por el alumno**: teléfono, email, **categoría** y **foto**. Nombre,
  apellido, fecha de nacimiento, DNI y modalidad de pago los mantiene el profe.
- **Foto** (`Alumno.FotoUrl`, ya existía): se sube desde el portal, se
  **comprime en el navegador** (~256px, JPEG) y se guarda como **data URL
  base64** en la base — sin storage externo. Validación en el server: que sea
  imagen y ≤ ~500 KB.
- **Raquetas** (`Raqueta`, entidad nueva 1:N con el alumno): marca + tensión +
  marca del encordado. El alumno tiene 1 o más y las administra él (CRUD por
  el portal, con regla de **pertenencia**: solo toca las suyas).
- **Categoría "por ahora" editable por el alumno**: es un solo campo en la
  ficha, así que el cambio se refleja en todos lados (lista del profe, cuotas).
  **Pendiente**: al cambiar de categoría, validar contra sus clases (una clase
  tiene categoría sugerida; ver la regla de categoría↔clase).

## 8. Las tres listas del profe (12/08/2026)

La sección Alumnos tiene tres pestañas, que son **tres recortes de la misma gente**:

| Pestaña | Quiénes |
|---|---|
| **Alumnos** | Los que tienen una clase asignada |
| **Lista de espera** | Los que quieren clase y no la tienen: sin ninguna, con un pedido de cupo sin resolver, o anotados a mano por el profe |
| **Usuarios** | Todas las fichas de la academia, tengan horario o no |

**No hay ninguna columna que diga en qué lista está alguien, y es a propósito.** Se derivan
del roster (`AlumnoHorario`) y de los pedidos pendientes. `EstadoAlumno.EnEspera` existió y
se borró el 10/08/2026 justo porque se desincronizaba de la realidad: al que estaba en la
espera lo sacaba de Alumnos y no había vuelta atrás. Por eso alguien puede estar **en
Alumnos y en la espera a la vez** (ya viene los martes y pidió los jueves).

Consecuencia práctica: **no existe forma de "mover" a alguien de lista**, y no hay que
construirla. Si aparece ese pedido, la pregunta correcta es qué situación de la cancha se
está tratando de resolver — la respuesta siempre termina siendo darle o sacarle una clase.

Reglas a respetar si tocás esto:

- **Prioridad de motivos: `PidioCupo` > `SinClase` > `LoAnotoElProfe`**, y **una fila por
  persona**: gana el motivo más concreto, que es el que ofrece la acción fuerte.
- **"Sacar de la espera" significa dos cosas distintas.** Al anotado a mano le apaga la
  marca y sigue siendo alumno; al que espera sin clase le **borra la ficha**. Es el mismo
  endpoint (`POST /solicitudes/{id}/quitar`) y decide por motivo.
- **`Alumno.EnEsperaDesde` guarda fecha, no bool**, para ordenar por antigüedad en la cola.
  Marcar dos veces no pisa la fecha (si la pisara, volver a tocar el botón lo mandaría al
  final).

### "Usuarios" es un padrón de FICHAS, no de personas

Esa pestaña lee la tabla `Alumnos`. **El director y los profes empleados no aparecen ahí**
salvo que alguien los haya cargado como alumnos: viven en `Tenant.OwnerUserId` y en
`MembresiaTenant`, y se ven en **Mi academia → Profesores**. Ni el registro de profesor ni
el alta de empleado crean una ficha.

Se reporta como bug cada tanto, y no lo es. Meter personas sin ficha en una tabla de fichas
da filas donde la mitad de las acciones no aplican (no hay a quién pausar, dar de baja ni
cobrarle cuota). El padrón de PERSONAS es otro concepto y va en el panel de Plataforma —
ver [`pedidos-del-profe.md`](pedidos-del-profe.md), bloque 6.

Desde el 12/08/2026, además, **el director y los profes empleados no ensucian la lista de
espera** cuando no toman clases: están trabajando, no esperando horario.

## Changelog

- **12/06/2026**: versión inicial. Multi-tenant, sin login de alumnos, grupos fijos, soporte menores, categorías 7ma-1ra.
- **17/07/2026 (M3)**: perfil editable por el alumno (contacto + categoría + foto base64 + raquetas). Entidad `Raqueta`.
- **12/08/2026**: las tres listas (§8). `EstadoAlumno.EnEspera` se elimina: la pertenencia
  a una lista se **deriva** de tener o no clase. La lista de espera pasa a mostrar la misma
  tabla que Alumnos (`EsperaResponseDto` hereda de `AlumnoResponseDto`), con filtro por club
  y `Club · Profe` en cada fila. El director y los profes salen de la espera.
- **05/08/2026**: se elimina el concepto `Grupo`. El cupo, la categoría y el
  roster pasan al `Horario` (`AlumnoHorario` reemplaza a `AlumnoGrupo`, con la
  misma forma). Lo de `Grupo`/`AlumnoGrupo` de §3 queda como historia: ver
  `modelo-agenda.md` §2 para el modelo vigente.
