import type { ReactNode } from 'react';
import { useRevelar } from '../../hooks/useRevelar';

/** Bloque que aparece al scrollear. En mobile reemplaza el hover. */
export default function Seccion({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  const { ref, visible } = useRevelar<HTMLDivElement>();
  return (
    <div ref={ref} className={`revelar ${visible ? 'esVisible' : ''} ${className ?? ''}`.trim()}>
      {children}
    </div>
  );
}
