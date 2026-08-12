import { useEffect, useState } from 'react';
import { useRevelar } from '../../hooks/useRevelar';

/**
 * Cuenta de 0 al valor cuando el número entra en pantalla.
 * Es el “marcador que se enciende”, no un spinner.
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
  const [n, setN] = useState(0);

  useEffect(() => {
    if (!visible) return;
    const inicio = performance.now();
    const duracion = 900;
    let raf = 0;
    const tick = (ahora: number) => {
      const t = Math.min(1, (ahora - inicio) / duracion);
      const eased = 1 - (1 - t) ** 3;
      setN(Math.round(valor * eased));
      if (t < 1) raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [visible, valor]);

  return (
    <span ref={ref}>
      {prefijo}{n.toLocaleString('es-AR')}{sufijo}
    </span>
  );
}
