import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import s from './Tema.module.css';

export type Tema = 'light' | 'dark';

const STORAGE = 'cmr-tema';

function leerTema(): Tema {
  try {
    const guardado = localStorage.getItem(STORAGE);
    if (guardado === 'dark' || guardado === 'light') return guardado;
  } catch { /* private mode */ }
  return 'light';
}

function aplicar(tema: Tema) {
  document.documentElement.dataset.theme = tema;
}

const TemaCtx = createContext<{ tema: Tema; toggle: () => void }>({
  tema: 'light',
  toggle: () => {},
});

export function useTema() {
  return useContext(TemaCtx);
}

/** Tema persistido. La transición se activa solo al cambiar, no al cargar. */
export function TemaProvider({ children }: { children: ReactNode }) {
  const [tema, setTema] = useState<Tema>(() => {
    const inicial = leerTema();
    aplicar(inicial);
    return inicial;
  });

  useEffect(() => {
    aplicar(tema);
  }, [tema]);

  const toggle = useCallback(() => {
    document.documentElement.classList.add('theme-transicion');
    window.setTimeout(() => document.documentElement.classList.remove('theme-transicion'), 500);
    setTema((t) => {
      const next = t === 'light' ? 'dark' : 'light';
      try { localStorage.setItem(STORAGE, next); } catch { /* ignore */ }
      return next;
    });
  }, []);

  const value = useMemo(() => ({ tema, toggle }), [tema, toggle]);
  return <TemaCtx.Provider value={value}>{children}</TemaCtx.Provider>;
}

/** Botón de tema: se lee como control de cancha, no como icono genérico. */
export function BotonTema() {
  const { tema, toggle } = useTema();
  const aOscuro = tema === 'light';
  return (
    <button
      type="button"
      className={`${s.boton} motion-static`}
      onClick={toggle}
      aria-label={aOscuro ? 'Pasar a modo oscuro' : 'Pasar a modo claro'}
      title={aOscuro ? 'Modo oscuro' : 'Modo claro'}
    >
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden>
        {aOscuro ? (
          <>
            <circle cx="12" cy="12" r="4" />
            <path d="M12 3v2M12 19v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M3 12h2M19 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4" />
          </>
        ) : (
          <path d="M18 14.5A6.5 6.5 0 0 1 9.5 6 7 7 0 1 0 18 14.5z" />
        )}
      </svg>
    </button>
  );
}
