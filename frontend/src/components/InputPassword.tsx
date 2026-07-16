import { useState } from 'react';
import s from './InputPassword.module.css';

interface Props {
  value: string;
  onChange: (valor: string) => void;
  placeholder?: string;
  autoFocus?: boolean;
}

/** Campo de contraseña con ojito para ver/ocultar lo tipeado. */
export default function InputPassword({ value, onChange, placeholder, autoFocus }: Props) {
  const [visible, setVisible] = useState(false);

  return (
    <div className={s.campo}>
      <input
        type={visible ? 'text' : 'password'}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder ?? '••••••••'}
        autoFocus={autoFocus}
      />
      <button
        type="button"
        className={s.ojito}
        onClick={() => setVisible((v) => !v)}
        title={visible ? 'Ocultar contraseña' : 'Ver contraseña'}
        aria-label={visible ? 'Ocultar contraseña' : 'Ver contraseña'}
      >
        {visible ? (
          // Ojo tachado
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
            <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24M1 1l22 22" />
          </svg>
        ) : (
          // Ojo abierto
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
            <circle cx="12" cy="12" r="3" />
          </svg>
        )}
      </button>
    </div>
  );
}
