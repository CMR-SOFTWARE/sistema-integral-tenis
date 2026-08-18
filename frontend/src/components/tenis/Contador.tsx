import { useEffect, useState } from 'react';
import { useRevelar } from '../../hooks/useRevelar';
import { usePrefersReducedMotion } from '../../hooks/usePrefersReducedMotion';

/**
 * Cuenta al valor cuando el número entra en pantalla.
 * Valores grandes (plata) no arrancan en 0: se ve artificial.
 */
export default function Contador({
  valor,
  prefijo = '',
  sufijo = '',
}: {
  valor: number;
  prefijo?: string;
  sufijo?: string;
}) {
  const { ref, visible } = useRevelar<HTMLSpanElement>();
  const reduced = usePrefersReducedMotion();
  const grande = Math.abs(valor) >= 1000;
  const [n, setN] = useState(grande ? valor : 0);

  useEffect(() => {
    if (!visible) return;
    if (reduced) {
      setN(valor);
      return;
    }
    const desde = grande ? Math.round(valor * 0.88) : 0;
    const inicio = performance.now();
    const duracion = 560;
    let raf = 0;
    const tick = (ahora: number) => {
      const t = Math.min(1, (ahora - inicio) / duracion);
      const eased = 1 - (1 - t) ** 3;
      setN(Math.round(desde + (valor - desde) * eased));
      if (t < 1) raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [visible, valor, reduced, grande]);

  return (
    <span ref={ref}>
      {prefijo}{n.toLocaleString('es-AR')}{sufijo}
    </span>
  );
}
