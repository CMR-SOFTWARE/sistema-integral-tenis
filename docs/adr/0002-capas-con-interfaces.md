# ADR-0002 — Capas Controller → Service → Repository, con interfaces

- **Fecha**: 2026-06-18
- **Estado**: Aceptada

## Contexto

Queremos que los cambios futuros sean baratos: reglas de negocio testeables sin
base de datos, y poder cambiar detalles de infraestructura (ORM, servicios
externos) sin reescribir el dominio.

## Decisión

Tres capas estrictas (`Controller → Service → Repository`), cada una conoce solo
a la de abajo, cableadas por el DI nativo de ASP.NET Core. Services y
repositories se consumen **por interfaz** (`IAlumnoService`, `IAlumnoRepository`):
en .NET es idiomático y barato (`AddScoped<IFoo, Foo>()`), y habilita mocks en
tests. Un solo proyecto Web API con carpetas por capa; se separa en proyectos
solo si el tamaño lo exige.

Nota: cuando el borrador del plan era NestJS/TypeScript habíamos decidido
evitar interfaces (allá exigen tokens de inyección, mucha ceremonia). Con el
cambio a .NET esa decisión se invirtió a propósito.

## Consecuencias

- Enforcement por convención + revisión en PR (sin analizadores custom por ahora).
- Reglas de qué puede/no puede cada capa: ver `docs/arquitectura.md`.
