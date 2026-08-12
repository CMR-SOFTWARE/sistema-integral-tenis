import s from './TexturaCancha.module.css';

export type TexturaCanchaVariante = 'esquina' | 'red' | 'servicio' | 'lateral';

/** Fragmento de cancha como textura de fondo. Opacidad baja, nunca tapar texto. */
export default function TexturaCancha({
  variante = 'esquina',
  className,
}: {
  variante?: TexturaCanchaVariante;
  className?: string;
}) {
  return (
    <svg
      className={`${s.textura} ${className ?? ''}`}
      viewBox="0 0 200 120"
      fill="none"
      aria-hidden
      preserveAspectRatio="xMaxYMax slice"
    >
      {variante === 'esquina' && (
        <>
          <path d="M18 18 H130 M18 18 V100" />
          <rect x="18" y="18" width="72" height="48" />
        </>
      )}
      {variante === 'red' && (
        <>
          <line x1="100" y1="8" x2="100" y2="112" />
          <line x1="70" y1="60" x2="130" y2="60" />
        </>
      )}
      {variante === 'servicio' && (
        <>
          <rect x="40" y="16" width="120" height="88" />
          <line x1="100" y1="16" x2="100" y2="104" />
          <line x1="40" y1="60" x2="160" y2="60" />
        </>
      )}
      {variante === 'lateral' && (
        <>
          <line x1="24" y1="12" x2="24" y2="108" />
          <line x1="24" y1="108" x2="170" y2="108" />
          <line x1="24" y1="60" x2="90" y2="60" />
        </>
      )}
    </svg>
  );
}
