import { Link } from 'react-router-dom';
import s from './AccesosRapidos.module.css';

export interface Acceso {
  to: string;
  label: string;
  /** Path del ícono SVG (stroke, 24x24). */
  icon: string;
  color: string;
}

/** Fila de accesos directos del inicio: llevan a la acción con un toque. */
export default function AccesosRapidos({ accesos }: { accesos: Acceso[] }) {
  return (
    <div className={s.grid}>
      {accesos.map((a) => (
        <Link key={a.to + a.label} to={a.to} className={s.item}>
          <span className={s.icono} style={{ background: `${a.color}1a`, color: a.color }}>
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d={a.icon} />
            </svg>
          </span>
          <span className={s.label}>{a.label}</span>
          <span className={s.chev}>→</span>
        </Link>
      ))}
    </div>
  );
}
