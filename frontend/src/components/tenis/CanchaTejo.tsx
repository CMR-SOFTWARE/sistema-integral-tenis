import PelotaOutline from './PelotaOutline';
import s from './CanchaTejo.module.css';

/**
 * Cancha contenida + pelota tipo tejo: va y vuelve sin salir del recuadro.
 * Vive en su propia franja; nunca se superpone al contenido de al lado.
 */
export default function CanchaTejo({ className }: { className?: string }) {
  return (
    <div className={`${s.wrap} ${className ?? ''}`} aria-hidden>
      <svg className={s.cancha} viewBox="0 0 360 80" fill="none" preserveAspectRatio="none">
        <rect x="10" y="10" width="340" height="60" />
        <line x1="180" y1="10" x2="180" y2="70" />
        <line x1="10" y1="40" x2="350" y2="40" />
        <rect x="78" y="10" width="204" height="60" />
      </svg>
      <span className={s.estela} />
      <PelotaOutline className={s.pelota} />
    </div>
  );
}
