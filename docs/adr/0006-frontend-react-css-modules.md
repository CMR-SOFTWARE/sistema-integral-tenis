# ADR-0006 — Frontend React reconstruyendo el mockup CourtSet, con CSS Modules

- **Fecha**: 2026-06-18
- **Estado**: Aceptada

## Contexto

Existe un diseño completo hecho con Claude Design (`docs/diseno/Turnos
Tenis.dc.html`, marca "CourtSet"): 10 vistas de profesor + portal de alumno +
9 modales. No es código React (es el DSL interno de Claude Design), así que se
usa como **referencia visual**, no como código.

## Decisión

- Cada pantalla se **reconstruye como componentes React** (Vite + TS), fieles
  al mockup.
- Estilos con **CSS Modules** + **tokens de diseño** (`src/styles/tokens.css`
  extrae la paleta y tipografías del mockup). Sin Tailwind (una cosa nueva
  menos que aprender) y sin estilos inline (mantenibilidad).
- Organización **por features** (`src/features/...`), layout compartido en
  `src/components/layout/`.
- El **portal del alumno se posterga**: el modelo definió que el alumno no
  tiene login todavía (`UserId` nullable). El mockup de ese portal queda como
  referencia para esa fase futura. Ídem "generar acceso al portal" del modal
  de alta: se omite.
- Naming: la solución .NET se llama `SistemaIntegralDeportivo`; "CourtSet" se
  mantiene como marca visual en la UI hasta decidir el nombre comercial.

## Consecuencias

- Fidelidad visual verificable contra `docs/diseno/`.
- Al modal de alta del mockup se le **agrega** el bloque tutor/consentimiento
  para menores (obligatorio por modelo y por Ley 25.326), que el diseño no tenía.
