import { useEffect, useRef, useState } from 'react';
import { usePrefersReducedMotion } from '../../hooks/usePrefersReducedMotion';
import s from './ScoreBox.module.css';

type Nivel = 'punto' | 'game' | 'set' | 'match';

/**
 * Marcador que reacciona SOLO si el valor cambió después del primer paint.
 * El mount no dispara impacto: si no, cada visita al ranking “golpea”.
 */
export default function ScoreBox({
  valor,
  nivel = 'punto',
  className,
}: {
  valor: number | string;
  nivel?: Nivel;
  className?: string;
}) {
  const reduced = usePrefersReducedMotion();
  const primer = useRef(true);
  const prev = useRef(valor);
  const [bump, setBump] = useState(false);

  useEffect(() => {
    if (primer.current) {
      primer.current = false;
      prev.current = valor;
      return;
    }
    if (prev.current === valor) return;
    prev.current = valor;
    if (reduced) return;
    setBump(true);
    const t = window.setTimeout(() => setBump(false), 420);
    return () => window.clearTimeout(t);
  }, [valor, reduced]);

  return (
    <span
      className={`${s.box} ${className ?? ''}`.trim()}
      data-nivel={nivel}
      data-bump={bump ? '1' : undefined}
    >
      {valor}
    </span>
  );
}
