import { useRevelar } from '../../hooks/useRevelar';
import s from './LineArt.module.css';

export type LineArtVariante =
  | 'saque'
  | 'rally'
  | 'bote'
  | 'golpe'
  | 'slice'
  | 'globo'
  | 'lateral'
  | 'red'
  | 'cancha';

const TRAZOS: Record<Exclude<LineArtVariante, 'cancha'>, string> = {
  saque: 'M48 88 L48 30 C48 16, 62 10, 86 14 C 150 24, 220 78, 320 40',
  rally: 'M20 50 C 80 14, 140 86, 200 30 S 280 78, 340 44',
  bote: 'M16 74 C 70 74, 88 22, 140 22 S 188 74, 248 74 S 300 26, 344 26',
  golpe: 'M12 68 C 90 68, 130 18, 210 32 S 300 78, 348 24',
  slice: 'M16 58 C 90 78, 170 22, 250 48 S 320 70, 348 52',
  globo: 'M20 78 C 70 78, 110 8, 180 8 S 270 78, 340 78',
  lateral: 'M16 48 H344',
  red: 'M180 14 V82',
};

/**
 * Line art de tenis en loop lento: la línea se dibuja, el outline recorre,
 * todo se apaga y vuelve a empezar. Sin física ni pelota 3D.
 */
export default function LineArt({
  variante = 'golpe',
  className,
  alScroll = true,
  compacto = false,
}: {
  variante?: LineArtVariante;
  className?: string;
  alScroll?: boolean;
  compacto?: boolean;
}) {
  const { ref, visible } = useRevelar<HTMLDivElement>();
  const activo = !alScroll || visible;
  const path = variante === 'cancha' ? 'M24 48 H336' : TRAZOS[variante];

  return (
    <div
      ref={ref}
      className={`${s.escena} ${s[variante]} ${compacto ? s.compacto : ''} ${activo ? s.activo : ''} ${className ?? ''}`}
      aria-hidden
    >
      <svg className={s.svg} viewBox="0 0 360 96" fill="none" preserveAspectRatio="xMidYMid meet">
        {variante === 'rally' && (
          <>
            <circle className={s.ancla} cx="20" cy="50" r="3.5" />
            <circle className={s.ancla} cx="340" cy="44" r="3.5" />
          </>
        )}
        {variante === 'bote' && <path className={s.suelo} d="M12 74 H348" />}
        {variante === 'red' && <path className={s.suelo} d="M168 48 H192" />}
        {variante === 'cancha' ? (
          <>
            <rect className={s.trazoCancha} x="24" y="12" width="312" height="72" />
            <line className={s.trazoCancha} x1="180" y1="12" x2="180" y2="84" />
            <line className={s.trazoCancha} x1="24" y1="48" x2="336" y2="48" />
            <rect className={s.trazoCancha} x="90" y="12" width="180" height="72" />
          </>
        ) : (
          <path className={s.trazo} d={path} />
        )}
        {activo && variante !== 'cancha' && (
          <>
            <circle className={s.estela} r="3.2" cx="0" cy="0">
              <animateMotion
                dur="12s"
                begin="-0.55s"
                repeatCount="indefinite"
                path={path}
                keyPoints="0;0;1;1"
                keyTimes="0;0.18;0.72;1"
                calcMode="linear"
              />
              <animate
                attributeName="opacity"
                values="0;0.35;0.35;0;0"
                keyTimes="0;0.18;0.72;0.88;1"
                dur="12s"
                begin="-0.55s"
                repeatCount="indefinite"
              />
            </circle>
            <circle className={s.outline} r="4" cx="0" cy="0">
              <animateMotion
                dur="12s"
                repeatCount="indefinite"
                path={path}
                keyPoints="0;0;1;1"
                keyTimes="0;0.18;0.72;1"
                calcMode="linear"
              />
              <animate
                attributeName="opacity"
                values="0;1;1;0;0"
                keyTimes="0;0.18;0.72;0.88;1"
                dur="12s"
                repeatCount="indefinite"
              />
            </circle>
          </>
        )}
      </svg>
    </div>
  );
}

/** Cancha mínima detrás del contenido. Solo decoración, nunca tapar texto. */
export function FondoCancha() {
  return (
    <svg className={s.fondoCancha} viewBox="0 0 200 120" fill="none" aria-hidden>
      <rect className={s.fondoTrazo} x="10" y="10" width="180" height="100" />
      <line className={s.fondoTrazo} x1="100" y1="10" x2="100" y2="110" />
      <line className={s.fondoTrazo} x1="10" y1="60" x2="190" y2="60" />
      <rect className={s.fondoTrazo} x="48" y="10" width="104" height="100" />
    </svg>
  );
}
