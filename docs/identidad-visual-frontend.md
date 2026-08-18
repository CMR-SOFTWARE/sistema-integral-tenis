# Identidad visual frontend — CMR Tennis (handoff)

Estado al 12/08/2026. Trabajo **solo visual**; no tocar lógica/backend.
Retomar desde acá en la próxima sesión.

## Fuente de verdad

- Tokens: `frontend/src/styles/tokens.css`
- Tipografías cargadas: `frontend/index.html`
- Base tipográfica global: `frontend/src/index.css`
- Shell (sidebar + header + barra inferior): `frontend/src/components/layout/AppLayout.module.css`
- Animaciones tenis: `frontend/src/components/tenis/*`

## Paleta — jerarquía

### CMR institucional (protagonista)

| Token | Claro | Uso |
|-------|-------|-----|
| `--color-primary` | `#178a4c` | Links, activos, acciones, focus |
| `--color-primary-dark` | `#0e6b3c` | Botones, peso visual |
| `--color-primary-darker` | `#0b5730` | Hover / contraste |
| `--color-primary-deep` | `#137a43` | Login, profundidad, gradientes |
| `--color-lime` | `#d8fb5b` | **Solo acento** (indicadores, underlines, motion) |
| `--color-on-lime` | `#132a13` | Texto sobre lima |
| `--color-on-primary` | crema / ink | Texto sobre botones (legible en light y dark) |
| `--color-nav` | `#178a4c` (dark: `#0e6b3c`) | Sidebar **y** header |

### Calidez (fondos / superficies)

`#F7F1E3` `#F4EBDD` `#F2E8CF` `#E7F6EC` `#AFC8B4` `#6F8F7B` `#D8C7A6` `#C9B28A`

Nunca fondo predominante blanco puro.

### Gama intensa (acento / animación, no dominante)

`#CCFF33` `#9EF01A` `#70E000` `#38B000` `#008000`  
Tokens: `--color-spark`, `--color-accent`, `--court-motion` / `--court-line`.

### Otros CMR legacy que siguen vivos

`#386641` `#3E5F4D` `#556B2F` `#6A994E` `#8A9B63` `#A7C957` `#132A13` `#BC4749`

## Navegación unificada

Sidebar + barra superior = mismo `--color-nav`.
Texto nav: crema / muted; activo: lima `#d8fb5b`.
Barra inferior mobile: mismo verde nav + activo lima.

## Tipografía

| Token | Familia | Uso |
|-------|---------|-----|
| `--font-display` | Outfit (+ Rajdhani fallback) | Títulos / encabezados |
| `--font-ui` | Poppins | Nav, botones, labels, UI |
| `--font-friendly` | Nunito | Vacíos, mensajes (`.vacio`, `.msgAmable`) |
| `--font-body` | Inter | Cuerpo |
| `--font-score` | Rajdhani | Marcadores / avatares numéricos |

**No abusar de 700/800/900.** Preferir 400/500/600; bold solo lo importante.

## Contraste — reglas ya aprendidas

- Lima **no** como color de texto grande sobre crema (tabs usan `--color-primary` + underline lima).
- Botones: `color: var(--color-on-primary)`, no crema hardcodeado en dark.
- Chips sobre lima: `var(--color-on-lime)`.
- `sectorDeep` / paneles profundos: texto crema sobre verde oscuro.

## Pendiente / próximos refinamientos posibles

- Revisar pesos `font-weight: 700|800` residuales en módulos de features.
- Spot-check visual light/dark en: login, dashboard, agenda, alumnos, portal, cuotas.
- Afinar saturación si algún bloque se siente agresivo (volver a sage/crema).
- No commitear hasta que Lucas lo pida (él corre git).

## Motion

Tokens en `frontend/src/styles/tokens.css` (`--motion-fast` … `--motion-slow`, easings).
Keyframes y utilidades (`.motion-page`, `.motion-card`, `.motion-press` via `button`, `.motion-toast`, `.motion-net`, LIVE, skeleton) en `frontend/src/styles/motion.css`.
Primitivos: `components/motion/` (`PageTransition`, `LiveIndicator`, `ScoreBox`, `CargaTenis`).
La pelota no se agrega como mascota: `PelotaNav` / `pelotaRuta` siguen siendo cues de navegación.

## Verificación

Último check: `npm run build` (frontend) en verde tras el refinamiento CMR + tipografías + header unificado.
