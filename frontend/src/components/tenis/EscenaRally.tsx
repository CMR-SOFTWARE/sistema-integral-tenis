import PelotaOutline from './PelotaOutline';
import s from './EscenaRally.module.css';

/**
 * Escena final: dos raquetas line art + pelota en rally sutil.
 * Solo decoración; va al pie de página con aire alrededor.
 */
export default function EscenaRally({ className }: { className?: string }) {
  return (
    <div className={`${s.escena} ${className ?? ''}`} aria-hidden>
      <svg className={s.svg} viewBox="0 0 360 96" fill="none" preserveAspectRatio="xMidYMid meet">
        {/* Trayectoria suave del intercambio */}
        <path className={s.trazo} d="M78 52 C 130 18, 230 18, 282 52" />

        {/* Raqueta izquierda */}
        <g className={s.raquetaIzq}>
          <ellipse cx="42" cy="40" rx="16" ry="20" />
          <line x1="32" y1="24" x2="32" y2="56" />
          <line x1="42" y1="20" x2="42" y2="60" />
          <line x1="52" y1="24" x2="52" y2="56" />
          <line x1="28" y1="34" x2="56" y2="34" />
          <line x1="28" y1="44" x2="56" y2="44" />
          <line x1="42" y1="60" x2="42" y2="86" />
          <line x1="36" y1="86" x2="48" y2="86" />
        </g>

        {/* Raqueta derecha (espejo) */}
        <g className={s.raquetaDer}>
          <ellipse cx="318" cy="40" rx="16" ry="20" />
          <line x1="308" y1="24" x2="308" y2="56" />
          <line x1="318" y1="20" x2="318" y2="60" />
          <line x1="328" y1="24" x2="328" y2="56" />
          <line x1="304" y1="34" x2="332" y2="34" />
          <line x1="304" y1="44" x2="332" y2="44" />
          <line x1="318" y1="60" x2="318" y2="86" />
          <line x1="312" y1="86" x2="324" y2="86" />
        </g>
      </svg>

      <PelotaOutline className={s.pelota} />
    </div>
  );
}
