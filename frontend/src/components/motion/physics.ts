/**
 * Física CMR: un solo lugar para tensión, freno, arco y squash.
 * El rAF de la pelota de nav lee esto; no copiar números sueltos en componentes.
 *
 * Modelo: F = -k(x - target) - c·v  (resorte + amortiguación).
 * Un toque subamortiguado: aterriza con un overshoot chico, como una pelota.
 */
export const PHYSICS = {
  stiffness: 170,
  damping: 22,
  mass: 1,
  /** Stretch máximo a lo largo del viaje. Nunca cartoon. */
  maxStretch: 1.08,
  /** px/s a partir de los cuales el stretch llega al tope. */
  stretchRef: 900,
  /** Overshoot visual tope (fracción del recorrido). */
  maxOvershoot: 0.08,
  /** Altura de arco (px). Viajes largos se acercan a esto. */
  arcMax: 10,
  arcMaxMobile: 7,
  /** Compresión al picar la línea. */
  impactScale: 0.96,
  restPos: 0.45,
  restVel: 12,
  /** Clicks más lentos que esto se leen como saque calmo. */
  idleMs: 1400,
} as const;

export function clamp(n: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, n));
}

/** Amortiguación a partir de k y un damping ratio (1 = crítico). */
export function dampingFor(stiffness: number, zeta: number, mass: number = PHYSICS.mass): number {
  return 2 * zeta * Math.sqrt(stiffness * mass);
}

/**
 * Un paso de Euler semi-implícito. dt se clampa para que un tab
 * en background no dispare la pelota al otro lado de la pantalla.
 */
export function stepSpring(
  x: number,
  v: number,
  target: number,
  dt: number,
  k: number = PHYSICS.stiffness,
  c: number = PHYSICS.damping,
  mass: number = PHYSICS.mass,
): { x: number; v: number } {
  const step = Math.min(0.032, Math.max(0.001, dt));
  const a = (-k * (x - target) - c * v) / mass;
  const nv = v + a * step;
  const nx = x + nv * step;
  return { x: nx, v: nv };
}

/** Arco parabólico 4p(1-p): 0 en los extremos, 1 a mitad de camino. */
export function arcAt(progress: number, height: number): number {
  const p = clamp(progress, 0, 1);
  return 4 * p * (1 - p) * height;
}

export function stretchFromSpeed(speed: number): number {
  const t = clamp(speed / PHYSICS.stretchRef, 0, 1);
  return 1 + (PHYSICS.maxStretch - 1) * t;
}

/** Línea de cancha horizontal con un hundimiento suave bajo cx (no es un menisco líquido). */
export function courtPathX(width: number, cx: number, dip: number): string {
  const w = Math.max(1, width);
  const r = 18;
  const x = clamp(cx, r, w - r);
  return `M 0 2 H ${x - r} Q ${x} ${2 + dip} ${x + r} 2 H ${w}`;
}

/** Línea vertical (sidebar) con un pliegue hacia adentro a la altura cy. */
export function courtPathY(height: number, cy: number, dip: number): string {
  const h = Math.max(1, height);
  const r = 16;
  const y = clamp(cy, r, h - r);
  return `M 8 0 V ${y - r} Q ${8 - dip} ${y} 8 ${y + r} V ${h}`;
}
