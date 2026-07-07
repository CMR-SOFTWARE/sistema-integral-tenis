import type { ReactNode } from 'react';
import s from './Modal.module.css';

interface Props {
  titulo: string;
  subtitulo?: string;
  onClose: () => void;
  children: ReactNode;
  footer?: ReactNode;
  ancho?: number;
}

/** Modal genérico con el look del mockup (overlay blur + tarjeta redondeada). */
export default function Modal({ titulo, subtitulo, onClose, children, footer, ancho = 560 }: Props) {
  return (
    <div className={s.overlay} onClick={onClose}>
      <div className={s.card} style={{ maxWidth: ancho }} onClick={(e) => e.stopPropagation()}>
        <div className={s.header}>
          <div>
            <h3 className={s.titulo}>{titulo}</h3>
            {subtitulo && <div className={s.subtitulo}>{subtitulo}</div>}
          </div>
          <button className={s.cerrar} onClick={onClose} aria-label="Cerrar">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
              <path d="M18 6L6 18M6 6l12 12" />
            </svg>
          </button>
        </div>
        <div className={s.body}>{children}</div>
        {footer && <div className={s.footer}>{footer}</div>}
      </div>
    </div>
  );
}
