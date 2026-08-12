import s from './RaquetaLineArt.module.css';

/** Raqueta mínima: marco, unas cuerdas y mango. Oscila solo si se pide. */
export default function RaquetaLineArt({
  className,
  oscilar = false,
}: {
  className?: string;
  oscilar?: boolean;
}) {
  return (
    <svg
      className={`${s.raqueta} ${oscilar ? s.oscilar : ''} ${className ?? ''}`}
      viewBox="0 0 40 72"
      fill="none"
      aria-hidden
    >
      <ellipse cx="20" cy="22" rx="14" ry="18" />
      <line x1="12" y1="8" x2="12" y2="36" />
      <line x1="20" y1="4" x2="20" y2="40" />
      <line x1="28" y1="8" x2="28" y2="36" />
      <line x1="8" y1="16" x2="32" y2="16" />
      <line x1="8" y1="24" x2="32" y2="24" />
      <line x1="10" y1="32" x2="30" y2="32" />
      <line x1="20" y1="40" x2="20" y2="66" />
      <line x1="16" y1="66" x2="24" y2="66" />
    </svg>
  );
}
