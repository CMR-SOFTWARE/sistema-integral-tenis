# ADR-0003 — EF Core con SQLite para el prototipo

- **Fecha**: 2026-06-18
- **Estado**: Aceptada

## Contexto

Lucas conoce SQLite y SQL Server. El prototipo necesita arrancar sin fricción
de infraestructura (sin servidores de DB ni cuentas cloud), y la DB definitiva
se decide recién al desplegar.

## Decisión

**EF Core + SQLite** (archivo local `sistemaintegraldeportivo.db`, ignorado por
git) para todo el prototipo. Migraciones versionadas con `dotnet ef`. Enums
persistidos como texto para legibilidad. Para producción se migrará a
**SQL Server o PostgreSQL** (EF Core abstrae la mayor parte; se revisarán las
migraciones al cambiar de provider).

## Consecuencias

- Cero setup para desarrollar; la base se regenera con `dotnet ef database update`.
- SQLite no valida todo igual que un motor de producción (concurrencia,
  precisión decimal): las operaciones críticas se re-testearán al migrar.
- **Deuda anotada**: warning NU1903 (vulnerabilidad alta conocida) en
  `SQLitePCLRaw.lib.e_sqlite3 2.1.11`, dependencia transitiva del provider.
  Riesgo bajo en contexto (DB local de prototipo, sin exposición pública).
  Se diluye al pasar a SQL Server/Postgres en prod. Revisar si el prototipo
  se expone públicamente antes de esa migración.
