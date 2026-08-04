import { useRef, useState } from 'react';
import { ApiError } from '../../lib/api';
import s from './MiPerfil.module.css';

interface Props {
  etiqueta: string;
  /** Ya recibe el File elegido; quien lo use se encarga de comprimir y subir. */
  onElegir: (file: File) => Promise<unknown>;
  onError: (mensaje: string) => void;
  disabled?: boolean;
  /** Si viene, se usa en vez del botón por defecto (para la celda "sumar foto"). */
  children?: (abrir: () => void, subiendo: boolean) => React.ReactNode;
}

/**
 * Input de archivo escondido detrás de un botón. Deja el input limpio al terminar
 * para que elegir DOS VECES la misma foto vuelva a disparar el change.
 */
export default function SubirImagen({ etiqueta, onElegir, onError, disabled, children }: Props) {
  const input = useRef<HTMLInputElement>(null);
  const [subiendo, setSubiendo] = useState(false);

  const abrir = () => input.current?.click();

  const alElegir = async (file: File | undefined) => {
    if (!file) return;
    setSubiendo(true);
    try {
      await onElegir(file);
    } catch (e) {
      onError(e instanceof ApiError ? e.message : 'No pudimos subir la imagen. Probá de nuevo.');
    } finally {
      setSubiendo(false);
      if (input.current) input.current.value = '';
    }
  };

  return (
    <>
      <input
        ref={input}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        hidden
        onChange={(e) => void alElegir(e.target.files?.[0])}
      />
      {children ? children(abrir, subiendo) : (
        <button className={s.btnSuave} onClick={abrir} disabled={disabled || subiendo}>
          {subiendo ? 'Subiendo…' : etiqueta}
        </button>
      )}
    </>
  );
}
