# ADR-0001 — Backend en ASP.NET Core Web API (reemplaza NestJS)

- **Fecha**: 2026-06-18
- **Estado**: Aceptada

## Contexto

El plan v2 había elegido NestJS argumentando que "es un espejo de ASP.NET Core",
para aprovechar lo que Lucas ya sabía de .NET. Lucas domina C#/.NET, va a hacer
un curso formal de .NET, y es su producto: prioriza poder mantenerlo y darle
seguimiento él mismo a largo plazo.

## Decisión

El backend es **ASP.NET Core Web API** (.NET 10 LTS). Si el argumento para NestJS
era parecerse a ASP.NET Core, teniendo dominado el original se usa el original:
se elimina la capa de traducción y se pelea en un solo frente (el front React,
que sí es terreno nuevo). Web API (JSON) y no MVC/Razor porque el frontend es
una SPA React que consume endpoints.

## Consecuencias

- El monorepo pnpm se desarma: `backend/` (solución .NET) + `frontend/` (React).
- El scaffold NestJS se descartó (commit b563d03); el modelo Prisma se portó a EF Core.
- El hosting del backend deberá ser .NET-friendly (se decide al desplegar).
- El curso de .NET alimenta directamente el proyecto.
