import { useCallback, useRef, useState } from 'react';
import type { RefObject } from 'react';
import { useLocation } from 'react-router-dom';
import PelotaOutline from './PelotaOutline';
import { useBallTravel } from '../motion/useBallTravel';
import s from './PelotaNav.module.css';

/**
 * Marca el ítem activo con la pelota (pica en sitio). Al cambiar de sección
 * viaja con física; no desaparece: sin ella no se ve dónde estás parado.
 */
export default function PelotaNav({
  contenedorRef,
  eje = 'y',
}: {
  contenedorRef: RefObject<HTMLElement | null>;
  eje?: 'x' | 'y';
}) {
  const { pathname } = useLocation();
  const ballRef = useRef<HTMLSpanElement>(null);
  const pathRef = useRef<SVGPathElement>(null);
  const svgRef = useRef<SVGSVGElement>(null);
  const [visible, setVisible] = useState(false);
  const onVisible = useCallback((v: boolean) => setVisible(v), []);

  useBallTravel({
    contenedorRef,
    eje,
    ballRef,
    pathRef,
    svgRef,
    watch: pathname,
    onVisible,
  });

  return (
    <div
      className={`${s.capa} ${eje === 'x' ? s.ejeX : s.ejeY}`}
      data-visible={visible ? '1' : undefined}
      aria-hidden
    >
      <svg ref={svgRef} className={s.linea} preserveAspectRatio="none">
        <path ref={pathRef} />
      </svg>
      <span ref={ballRef} className={s.pelota}>
        <PelotaOutline />
      </span>
    </div>
  );
}
