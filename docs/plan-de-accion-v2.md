# Plan de Acción v2 — AcePro (reinicio desde cero)

> Documento fundacional del proyecto nuevo. Reemplaza a `plan-de-accion.md` (v1).
> Última actualización: 03/07/2026
>
> ⚠️ **Stack actualizado (18/06/2026)**: el backend cambió de NestJS a
> **ASP.NET Core Web API (.NET 10) + EF Core + SQLite** — ver `docs/adr/0001`
> y `docs/arquitectura.md`. La sección 2 refleja la decisión original y se
> conserva como historia; ante conflicto, mandan los ADRs.

---

## 1. Visión del producto (sin cambios)

Plataforma SaaS multi-tenant para el ecosistema del tenis en Argentina:

1. **Profesores** se registran pagando mensualidad → gestionan alumnos, horarios (en uno o más clubes), cuotas y estadísticas de sus números.
2. **Clubes** se registran como institución → cargan canchas con horarios, dan de alta socios, y los socios reservan turnos recurrentes.
3. **Ranking amateur** (futuro): todo usuario de la app participa; al terminar un turno jugado se carga el resultado y suma al ranking.
4. **Marketplace** (futuro): compra-venta de productos de tenis entre usuarios.

**Principio rector**: construir por partes, bien estructurado, listo para crecer. Cada módulo se termina (con tests) antes de pasar al siguiente.

---

## 2. Stack técnico (decisión cerrada)

| Capa | Tecnología | Por qué |
|---|---|---|
| Frontend | **React 18 + TypeScript + Vite** | Componentes con estado complejo (agendas, dashboards). TS ≈ C#: tipado familiar |
| Backend | **NestJS** (Node + TypeScript) | Arquitectura impuesta (DI, módulos, capas). Espejo de ASP.NET Core |
| ORM | **Prisma** | Migraciones versionadas, types automáticos hacia TS |
| Base de datos | **PostgreSQL** (Supabase) | RLS para multi-tenancy, gratis al arrancar |
| Auth | **Passport + JWT** (integrado en Nest) | Estándar del ecosistema |
| Validación | **class-validator** en DTOs | Nada entra sin validarse en el borde |
| Testing | **Jest** (unit + integration) + **Playwright** (e2e) | Jest viene con Nest |
| Pagos | **Mercado Pago** (preapproval + checkout) | Estándar Argentina |
| Mensajería | **Twilio WhatsApp Business API** | Canal principal de los usuarios |
| Hosting | A definir (Vercel/Railway/Render para API) | Decisión pendiente, no bloquea |
| CI/CD | **GitHub Actions** | Lint + tests en cada push, desde el día 1 |

**Gestor de paquetes**: `pnpm` con workspaces (monorepo).

---

## 3. Estructura del repositorio (monorepo)

```
acepro/
├── apps/
│   ├── api/                  # Backend NestJS
│   │   ├── src/
│   │   │   ├── modules/      # Un módulo por dominio
│   │   │   │   ├── tenants/
│   │   │   │   ├── alumnos/
│   │   │   │   │   ├── alumnos.controller.ts   # HTTP: recibe y responde, NO piensa
│   │   │   │   │   ├── alumnos.service.ts      # Lógica de negocio, NO sabe de HTTP
│   │   │   │   │   ├── alumnos.repository.ts   # Acceso a datos vía Prisma
│   │   │   │   │   ├── dto/                    # Validación de entrada/salida
│   │   │   │   │   └── alumnos.module.ts
│   │   │   │   └── ...
│   │   │   ├── common/       # Guards, pipes, filters, decorators transversales
│   │   │   │   └── tenant/   # Resolución de tenantId (guard + decorator)
│   │   │   ├── prisma/       # PrismaService + schema
│   │   │   └── main.ts
│   │   └── test/             # Tests e2e de la API
│   └── web/                  # Frontend React + Vite
│       └── src/
│           ├── features/     # Por dominio (alumnos, turnos...), no por tipo de archivo
│           ├── components/   # UI compartida
│           ├── lib/          # Cliente HTTP, helpers
│           └── routes/
├── docs/                     # Documentación viva (este archivo, modelos, LEARNING.md)
├── .github/workflows/        # CI
├── pnpm-workspace.yaml
└── package.json
```

---

## 4. Arquitectura y patrones (reglas no negociables)

### 4.1 Capas estrictas (igual que en .NET)
`Controller → Service → Repository`. Cada capa solo conoce a la de abajo. Un controller NUNCA toca Prisma directo. Un service NUNCA lee headers HTTP.

### 4.2 Multi-tenancy transversal
El `tenantId` se resuelve UNA vez (guard global a partir del JWT/subdominio) y se inyecta. Ningún repository acepta queries sin tenant. Refuerzo en DB con Row Level Security. Doble candado: aplicación + base.

### 4.3 DTOs en todos los bordes
Entrada: `class-validator` rechaza cualquier payload inválido antes de llegar al service. Salida: nunca devolver entidades de Prisma crudas (riesgo de filtrar campos sensibles).

### 4.4 Transacciones donde hay concurrencia
Toda operación "leer-decidir-escribir" (reservar turno, tomar turno liberado) va dentro de `prisma.$transaction` con el nivel de aislamiento correcto. Lección aprendida del proyecto anterior (race condition en claim de slots).

### 4.5 Definición de "terminado"
Una feature está terminada cuando: compila sin warnings de lint + tiene tests de la lógica de negocio + pasó CI + está documentada si introduce conceptos nuevos. No antes.

### 4.6 Seguridad desde el día 1
- Passwords con argon2, JWT con expiración corta + refresh
- Rate limiting en login/registro (`@nestjs/throttler`)
- Variables de entorno fuera del repo, validadas al arrancar (la app no levanta si falta una)
- Nunca loguear datos sensibles (DNI, tokens, passwords)
- Checklist Ley 25.326 heredada de v1: consentimientos con timestamp, derecho al olvido, tutores para menores

---

## 5. Metodología de trabajo

- **Git**: rama `main` protegida. Todo entra por Pull Request con CI verde. Ramas `feat/...`, `fix/...`, `docs/...`
- **Commits**: convención `tipo: descripción` (`feat: alta de alumno`, `fix: validación de DNI`)
- **CI (GitHub Actions)**: en cada push corre lint + tests de api y web. Si falla, no se mergea
- **Documentación**: cada decisión de diseño va a `docs/`. `LEARNING.md` como changelog semanal de aprendizaje
- **Issues de GitHub** como backlog: cada tarea de las fases es un issue

---

## 6. Fases (mismas que v1, reordenadas al nuevo contexto)

> **Diseño de identidad y registro (05/07/2026)**: cómo se registran y
> conviven profesores, clubes, alumnos y socios (usuario global gratis +
> membresías por tenant, ranking/marketplace a nivel plataforma) está
> definido en `docs/modelo-identidad-roles.md` (ADR-0007). Las fases de
> abajo lo implementan gradualmente.

| Fase | Alcance | Módulos |
|---|---|---|
| **0 — Fundación** | Setup repo, CI, auth básica, multi-tenancy funcionando | tenants, auth, common |
| **1 — Profesores** | Gestión de alumnos (modelo ya diseñado en `modelo-alumnos.md`), horarios multi-club, cuotas, estadísticas básicas | alumnos, horarios, pagos |
| **2 — Clubes** | Registro institucional, canchas + horarios, socios, turnos recurrentes | clubes, canchas, reservas |
| **3 — Ranking** | Carga de resultados post-turno, puntos, categorías 7ma→1ra | ranking, partidos |
| **4 — Marketplace** | Publicaciones, compra-venta, comisiones | marketplace |

> El diseño del ranking (`ranking-sistema.md`) y el contador de partidos (`contador-partidos.md`) siguen vigentes como referencia para Fase 3.

---

## 7. Decisiones tomadas (12/06/2026)

| Decisión | Elección | Notas |
|---|---|---|
| **Hosting** | Frontend en **Vercel** (gratis) · API en **Railway** (~USD 5/mes, no se duerme) · DB en **Supabase** (gratis) | Render free descartado: el servicio se duerme y el primer request tarda 30-60 s. Deploy guiado paso a paso cuando llegue el momento |
| **Categorías** | **7ma a 1ra, definitivo** | `ranking-sistema.md` debe actualizarse a este esquema (reemplaza los 4 tiers) |
| **Tenants en URL** | **Paths**: `acepro.com/juanperez` | Subdominios postergados (DNS wildcard, SSL, cookies, dev local). La resolución del tenant se encapsula en UN guard (`TenantResolver`): si algún día migramos a subdominios, se cambia solo ese componente |
| **Flujo de alta de profes** | Registro → checkout Mercado Pago → acceso | El tenant nace `PENDIENTE_PAGO` y pasa a `ACTIVO` con el webhook de MP. Alumnos: registro gratuito siempre |

## 7b. Decisiones pendientes

- [ ] Monto del plan de profes (referencia previa: ARS 28.000–35.000/mes) — lo define Lucas más adelante; no bloquea el desarrollo porque el flujo ya está definido

---

## Changelog

- **12/06/2026 (tarde)**: cerradas 4 decisiones: hosting (Vercel + Railway + Supabase), categorías 7ma→1ra definitivas, tenants por path con resolución encapsulada, flujo registro-pago-acceso para profes.
- **12/06/2026**: v2 inicial. Reinicio desde cero. Stack: NestJS + React/Vite/TS, monorepo pnpm, metodología con PRs + CI obligatorio.
