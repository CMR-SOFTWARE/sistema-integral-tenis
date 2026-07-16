import s from './AppLayout.module.css';

/** Hamburguesa del header: abre el drawer. Solo visible en móvil (CSS). */
export default function BotonMenu({ onClick }: { onClick: () => void }) {
  return (
    <button className={s.hamburguesa} onClick={onClick} title="Menú" aria-label="Abrir menú">
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
        <path d="M3 6h18M3 12h18M3 18h18" />
      </svg>
    </button>
  );
}
