import { useState } from 'react';
import { ApiError } from '../../lib/api';
import { hoyIso } from './types';
import s from './FormEncordado.module.css';

/** El cuerpo que espera la API (espejo de GuardarEncordadoDto). */
export interface CuerpoEncordado {
  cuerdaVertical: string;
  tensionVertical: string | null;
  cuerdaHorizontal: string | null;
  tensionHorizontal: string | null;
  fecha: string; // "2026-08-01"
}

/**
 * Un encordado ya guardado, listo para precargar el formulario al corregirlo. Vive acá
 * y no en cada pantalla porque el que sabe qué campos necesita el form es el form.
 */
export function aCuerpo(e: CuerpoEncordado): CuerpoEncordado {
  return {
    cuerdaVertical: e.cuerdaVertical,
    tensionVertical: e.tensionVertical,
    cuerdaHorizontal: e.cuerdaHorizontal,
    tensionHorizontal: e.tensionHorizontal,
    fecha: e.fecha,
  };
}

interface Props {
  onGuardar: (cuerpo: CuerpoEncordado) => Promise<void>;
  onCancelar: () => void;
  onError: (mensaje: string | null) => void;
  /** Al CORREGIR uno ya cargado: los valores con los que arranca el formulario. */
  inicial?: CuerpoEncordado;
}

/**
 * Cargar un encordado, o corregir uno ya cargado. Lo usan el alumno desde su perfil y
 * el profe desde la ficha, así que no depende de los estilos de ninguno de los dos.
 *
 * Es el MISMO formulario para el alta y la edición a propósito: la regla del híbrido
 * (las horizontales solo viajan si el check está puesto) tiene que valer para los dos,
 * y duplicada se desincroniza.
 *
 * El híbrido (dos cuerdas distintas) va escondido detrás de un check: es el caso
 * raro, y mostrar cuatro campos de entrada asusta al que solo quiere anotar que
 * la encordó.
 */
export default function FormEncordado({ onGuardar, onCancelar, onError, inicial }: Props) {
  const [cuerda, setCuerda] = useState(inicial?.cuerdaVertical ?? '');
  const [tension, setTension] = useState(inicial?.tensionVertical ?? '');
  // Que ya tenga cuerda horizontal cargada ES la definición de híbrido: el check
  // arranca tildado solo, sin guardar un booleano aparte que se pueda contradecir.
  const [hibrido, setHibrido] = useState(inicial?.cuerdaHorizontal != null);
  const [cuerdaH, setCuerdaH] = useState(inicial?.cuerdaHorizontal ?? '');
  const [tensionH, setTensionH] = useState(inicial?.tensionHorizontal ?? '');
  const [fecha, setFecha] = useState(inicial?.fecha ?? hoyIso());
  const [guardando, setGuardando] = useState(false);
  const editando = inicial != null;

  const valido = cuerda.trim() !== '' && fecha !== '';

  const guardar = async () => {
    onError(null);
    setGuardando(true);
    try {
      await onGuardar({
        cuerdaVertical: cuerda.trim(),
        tensionVertical: tension.trim() || null,
        // Si se destilda el híbrido, las horizontales no viajan: el encordado
        // vuelve a ser simple aunque hayan quedado escritas.
        cuerdaHorizontal: hibrido ? cuerdaH.trim() || null : null,
        tensionHorizontal: hibrido ? tensionH.trim() || null : null,
        fecha,
      });
    } catch (e) {
      onError(e instanceof ApiError ? e.message : 'No se pudo guardar el encordado.');
    } finally {
      setGuardando(false);
    }
  };

  return (
    <div className={s.form}>
      <div className={s.titulo}>{hibrido ? 'Verticales' : 'Cuerda y tensión'}</div>
      <div className={s.fila}>
        <input
          autoFocus
          placeholder="Cuerda (ej: Luxilon ALU Power)"
          value={cuerda}
          onChange={(e) => setCuerda(e.target.value)}
          maxLength={80}
        />
        <input
          placeholder="Tensión (ej: 24 kg)"
          value={tension}
          onChange={(e) => setTension(e.target.value)}
          maxLength={40}
        />
      </div>

      <label className={s.check}>
        <input type="checkbox" checked={hibrido} onChange={(e) => setHibrido(e.target.checked)} />
        Encordado híbrido (otra cuerda en las horizontales)
      </label>

      {hibrido && (
        <>
          <div className={s.titulo}>Horizontales</div>
          <div className={s.fila}>
            <input
              placeholder="Cuerda de las horizontales"
              value={cuerdaH}
              onChange={(e) => setCuerdaH(e.target.value)}
              maxLength={80}
            />
            <input
              placeholder="Tensión"
              value={tensionH}
              onChange={(e) => setTensionH(e.target.value)}
              maxLength={40}
            />
          </div>
        </>
      )}

      <div className={s.titulo}>Fecha del encordado</div>
      <div className={s.fila}>
        {/* Por defecto hoy, pero se puede cargar uno viejo: el que anota después. */}
        <input type="date" value={fecha} max={hoyIso()} onChange={(e) => setFecha(e.target.value)} />
      </div>

      <div className={s.acciones}>
        <button className={s.btnCancelar} onClick={onCancelar}>Cancelar</button>
        <button className={s.btnGuardar} disabled={!valido || guardando} onClick={() => void guardar()}>
          {guardando ? 'Guardando…' : editando ? 'Guardar cambios' : 'Guardar encordado'}
        </button>
      </div>
    </div>
  );
}
