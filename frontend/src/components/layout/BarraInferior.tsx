import { useRef } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import PelotaNav from '../tenis/PelotaNav';
import { coincideRuta } from './nav';
import type { NavItem } from './nav';
import s from './AppLayout.module.css';

/**
 * Barra inferior mobile: los 3 destinos del día a día + “Más” (abre el drawer).
 * En escritorio no se ve (CSS). Es el equivalente a un TabBar de una app nativa.
 */
export default function BarraInferior({
  items,
  onMas,
}: {
  items: NavItem[];
  onMas: () => void;
}) {
  const { pathname } = useLocation();
  const barraRef = useRef<HTMLElement>(null);
  const enPrincipal = items.some((item) => coincideRuta(pathname, item.to));

  return (
    <nav ref={barraRef} className={s.barraInferior} aria-label="Navegación principal">
      <PelotaNav contenedorRef={barraRef} eje="x" />
      {items.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          end={item.to === '/portal'}
          data-nav-activo={coincideRuta(pathname, item.to) ? '1' : undefined}
          className={() =>
            coincideRuta(pathname, item.to) ? `${s.barraItem} ${s.barraItemActivo}` : s.barraItem
          }
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d={item.icon} />
          </svg>
          <span>{item.label}</span>
        </NavLink>
      ))}
      <button
        type="button"
        data-nav-activo={!enPrincipal ? '1' : undefined}
        className={enPrincipal ? s.barraItem : `${s.barraItem} ${s.barraItemActivo}`}
        onClick={onMas}
      >
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
          <path d="M3 6h18M3 12h18M3 18h18" />
        </svg>
        <span>Más</span>
      </button>
    </nav>
  );
}
