import { useLayoutEffect, useState } from 'react';
import type { RefObject } from 'react';
import { useLocation } from 'react-router-dom';
import PelotaOutline from './PelotaOutline';
import s from './PelotaNav.module.css';

/**
 * Pelota de orientación: viaja hasta el ítem activo (con un arco suave)
 * y después pica en loop. El contenedor tiene que ser position: relative.
 */
export default function PelotaNav({
  contenedorRef,
  eje = 'y',
}: {
  contenedorRef: RefObject<HTMLElement | null>;
  eje?: 'y' | 'x';
}) {
  const { pathname } = useLocation();
  const [pos, setPos] = useState(0);
  const [visible, setVisible] = useState(false);
  const [viajando, setViajando] = useState(false);

  useLayoutEffect(() => {
    const caja = contenedorRef.current;
    if (!caja) return;

    const medir = () => {
      const activo = caja.querySelector<HTMLElement>('[data-nav-activo="1"]');
      if (!activo) {
        setVisible(false);
        return;
      }
      const next = eje === 'x'
        ? activo.offsetLeft + activo.offsetWidth / 2 - 7
        : activo.offsetTop + activo.offsetHeight / 2 - 8;
      setViajando(true);
      setPos(next);
      setVisible(true);
    };

    medir();
    const t = window.setTimeout(() => setViajando(false), 560);
    caja.addEventListener('scroll', medir, { passive: true });
    window.addEventListener('resize', medir);
    return () => {
      window.clearTimeout(t);
      caja.removeEventListener('scroll', medir);
      window.removeEventListener('resize', medir);
    };
  }, [pathname, contenedorRef, eje]);

  if (!visible) return null;

  return (
    <span
      className={`${s.pelota} ${eje === 'x' ? s.ejeX : s.ejeY} ${viajando ? s.viajando : s.enSitio}`}
      style={eje === 'x' ? { transform: `translateX(${pos}px)` } : { transform: `translateY(${pos}px)` }}
      aria-hidden
    >
      <PelotaOutline />
    </span>
  );
}
