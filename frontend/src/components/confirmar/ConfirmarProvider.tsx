import { createContext, useCallback, useContext, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import Modal from '../Modal';
import s from './ConfirmarProvider.module.css';

/** Opciones de una confirmación (todo menos el título es opcional). */
export interface ConfirmarOpciones {
  titulo: string;
  mensaje?: ReactNode;
  /** Texto del botón que confirma (por defecto "Confirmar"). */
  confirmar?: string;
  /** Texto del botón que cancela (por defecto "Cancelar"). */
  cancelar?: string;
  /** Acción destructiva: el botón de confirmar se pinta en rojo. */
  peligro?: boolean;
}

type ConfirmarFn = (opciones: ConfirmarOpciones) => Promise<boolean>;

const ConfirmarContext = createContext<ConfirmarFn | null>(null);

/**
 * Provee una confirmación con el look de la app (Modal), en vez del
 * window.confirm() nativo del navegador. Se usa con `const confirmar =
 * useConfirmar()` y luego `if (!(await confirmar({ titulo, mensaje }))) return;`.
 */
export function ConfirmarProvider({ children }: { children: ReactNode }) {
  const [opciones, setOpciones] = useState<ConfirmarOpciones | null>(null);
  // Guarda el resolve de la promesa mientras el modal está abierto.
  const resolver = useRef<((ok: boolean) => void) | null>(null);

  const confirmar = useCallback<ConfirmarFn>((op) => {
    setOpciones(op);
    return new Promise<boolean>((resolve) => {
      resolver.current = resolve;
    });
  }, []);

  const cerrar = useCallback((ok: boolean) => {
    resolver.current?.(ok);
    resolver.current = null;
    setOpciones(null);
  }, []);

  return (
    <ConfirmarContext.Provider value={confirmar}>
      {children}
      {opciones && (
        <Modal titulo={opciones.titulo} onClose={() => cerrar(false)} ancho={440}>
          {opciones.mensaje && <p className={s.mensaje}>{opciones.mensaje}</p>}
          <div className={s.acciones}>
            <button className={s.cancelar} onClick={() => cerrar(false)}>
              {opciones.cancelar ?? 'Cancelar'}
            </button>
            <button
              className={opciones.peligro ? s.confirmarPeligro : s.confirmar}
              onClick={() => cerrar(true)}
              autoFocus
            >
              {opciones.confirmar ?? 'Confirmar'}
            </button>
          </div>
        </Modal>
      )}
    </ConfirmarContext.Provider>
  );
}

/** Devuelve la función `confirmar(...)`. Debe usarse dentro de <ConfirmarProvider>. */
export function useConfirmar(): ConfirmarFn {
  const ctx = useContext(ConfirmarContext);
  if (!ctx) throw new Error('useConfirmar debe usarse dentro de <ConfirmarProvider>');
  return ctx;
}
