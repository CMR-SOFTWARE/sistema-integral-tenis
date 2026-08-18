import { useLayoutEffect, useRef, type RefObject } from 'react';
import { usePrefersReducedMotion } from '../../hooks/usePrefersReducedMotion';
import {
  PHYSICS,
  arcAt,
  clamp,
  courtPathX,
  courtPathY,
  dampingFor,
  stepSpring,
  stretchFromSpeed,
} from './physics';

export type BallAxis = 'x' | 'y';

interface Opts {
  contenedorRef: RefObject<HTMLElement | null>;
  eje: BallAxis;
  ballRef: RefObject<HTMLElement | null>;
  pathRef: RefObject<SVGPathElement | null>;
  svgRef: RefObject<SVGSVGElement | null>;
  /** Extra (pathname, día abierto…): vuelve a medir el ítem activo. */
  watch?: unknown;
  onVisible: (v: boolean) => void;
}

function medirTarget(caja: HTMLElement, eje: BallAxis): number | null {
  const activo =
    caja.querySelector<HTMLElement>('[data-nav-activo="1"]')
    ?? caja.querySelector<HTMLElement>('a[aria-current="page"]');
  if (!activo) return null;
  return eje === 'x'
    ? activo.offsetLeft + activo.offsetWidth / 2
    : activo.offsetTop + activo.offsetHeight / 2;
}

/**
 * Pelota con masa: viaja al ítem activo por rAF (no teletransporta).
 * En reposo queda visible y pica: es la marca de la sección actual.
 */
export function useBallTravel({
  contenedorRef, eje, ballRef, pathRef, svgRef, watch, onVisible,
}: Opts) {
  const reduced = usePrefersReducedMotion();
  const irA = useRef<(animate: boolean) => void>(() => {});
  const primerWatch = useRef(true);

  useLayoutEffect(() => {
    let cancelled = false;
    let raf = 0;
    let wait = 0;
    let lastTs = 0;
    let pos = 0;
    let vel = 0;
    let target = 0;
    let start = 0;
    let arcH = 0;
    let k: number = PHYSICS.stiffness;
    let c: number = PHYSICS.damping;
    let squash = 0;
    let squashVel = 0;
    let hasPos = false;
    let traveling = false;
    let lastChange = performance.now();
    let mobile = window.matchMedia('(max-width: 900px)').matches;
    const half = eje === 'x' ? 7 : 8;
    let ro: ResizeObserver | undefined;
    let onScroll: (() => void) | undefined;
    let caja: HTMLElement | null = null;

    const soltar = () => {
      if (raf) cancelAnimationFrame(raf);
      if (wait) cancelAnimationFrame(wait);
      ro?.disconnect();
      if (caja && onScroll) {
        caja.removeEventListener('scroll', onScroll);
        window.removeEventListener('resize', onScroll);
      }
    };

    const arrancar = () => {
      caja = contenedorRef.current;
      if (cancelled) return;
      if (!caja) {
        wait = requestAnimationFrame(arrancar);
        return;
      }

      const paint = () => {
        const ball = ballRef.current;
        const path = pathRef.current;
        const svg = svgRef.current;
        if (!ball) return;

        const span = target - start;
        const p = Math.abs(span) < 0.5 ? 1 : clamp((pos - start) / span, 0, 1);
        const arc = traveling ? arcAt(p, arcH) : 0;
        const stretch = traveling ? stretchFromSpeed(Math.abs(vel)) : 1;
        const across = 1 - (stretch - 1) * 0.55;
        const impact = 1 - squash;

        let sx: number;
        let sy: number;
        let x: number;
        let y: number;
        if (eje === 'x') {
          x = pos - half;
          y = -arc;
          sx = stretch;
          sy = across * impact;
        } else {
          x = -arc;
          y = pos - half;
          sx = across * impact;
          sy = stretch;
        }

        ball.style.transform = `translate3d(${x}px, ${y}px, 0) scale(${sx}, ${sy})`;
        ball.style.visibility = 'visible';
        ball.style.willChange = traveling ? 'transform' : 'auto';
        if (traveling) ball.removeAttribute('data-en-sitio');
        else ball.setAttribute('data-en-sitio', '1');

        if (path && svg) {
          const dip = 2.4 + (traveling ? 3.2 * (4 * p * (1 - p)) : 0);
          if (eje === 'x') {
            const w = caja!.clientWidth;
            svg.setAttribute('viewBox', `0 0 ${Math.max(1, w)} 10`);
            path.setAttribute('d', courtPathX(w, pos, dip));
          } else {
            const h = caja!.scrollHeight || caja!.clientHeight;
            svg.setAttribute('viewBox', `0 0 10 ${Math.max(1, h)}`);
            path.setAttribute('d', courtPathY(h, pos, dip));
          }
        }
      };

      const tick = (ts: number) => {
        const dt = lastTs ? (ts - lastTs) / 1000 : 0.016;
        lastTs = ts;
        const next = stepSpring(pos, vel, target, dt, k, c);
        pos = next.x;
        vel = next.v;

        const over = Math.abs(target - start) * PHYSICS.maxOvershoot;
        const lo = Math.min(start, target) - over;
        const hi = Math.max(start, target) + over;
        if (pos < lo) { pos = lo; vel *= 0.35; }
        if (pos > hi) { pos = hi; vel *= 0.35; }

        if (squash > 0.001 || Math.abs(squashVel) > 0.001) {
          const sq = stepSpring(squash, squashVel, 0, dt, 280, dampingFor(280, 0.72));
          squash = sq.x;
          squashVel = sq.v;
        } else {
          squash = 0;
          squashVel = 0;
        }

        const settled = Math.abs(pos - target) < PHYSICS.restPos && Math.abs(vel) < PHYSICS.restVel;
        if (settled && traveling) {
          traveling = false;
          vel = 0;
          pos = target;
          squash = 1 - PHYSICS.impactScale;
          squashVel = 0;
        }

        paint();

        if (!settled || squash > 0.002 || Math.abs(squashVel) > 0.002) {
          raf = requestAnimationFrame(tick);
        } else {
          raf = 0;
          lastTs = 0;
          paint();
        }
      };

      const mover = (next: number, animate: boolean) => {
        if (!hasPos || reduced || !animate) {
          pos = next;
          vel = 0;
          target = next;
          start = next;
          traveling = false;
          squash = 0;
          hasPos = true;
          paint();
          return;
        }
        if (Math.abs(next - target) < 0.5) return;

        const now = performance.now();
        const dist = Math.abs(next - pos);
        const elapsed = now - lastChange;
        lastChange = now;
        const vUser = elapsed < PHYSICS.idleMs && elapsed > 0 ? dist / elapsed : 0.04;
        const cap = mobile ? PHYSICS.arcMaxMobile : PHYSICS.arcMax;
        arcH = clamp(dist * 0.07, 3, cap) * clamp(vUser / 0.55, 0.55, 1.25);
        const fast = vUser > 0.45;
        k = fast ? 145 : 190;
        c = dampingFor(k, fast ? 0.78 : 0.92);

        start = pos;
        target = next;
        traveling = true;
        if (!raf) {
          lastTs = 0;
          raf = requestAnimationFrame(tick);
        }
      };

      const medir = (animate: boolean) => {
        if (!caja) return;
        mobile = window.matchMedia('(max-width: 900px)').matches;
        const next = medirTarget(caja, eje);
        if (next == null) {
          const ball = ballRef.current;
          if (ball) {
            ball.style.visibility = 'hidden';
            ball.removeAttribute('data-en-sitio');
          }
          onVisible(false);
          return;
        }
        onVisible(true);
        mover(next, animate);
      };

      irA.current = medir;
      medir(false);

      ro = new ResizeObserver(() => medir(false));
      ro.observe(caja);
      onScroll = () => medir(false);
      caja.addEventListener('scroll', onScroll, { passive: true });
      window.addEventListener('resize', onScroll);
    };

    arrancar();
    return () => {
      cancelled = true;
      soltar();
    };
  }, [contenedorRef, eje, ballRef, pathRef, svgRef, reduced, onVisible]);

  useLayoutEffect(() => {
    if (primerWatch.current) {
      primerWatch.current = false;
      return;
    }
    irA.current(true);
  }, [watch]);
}
