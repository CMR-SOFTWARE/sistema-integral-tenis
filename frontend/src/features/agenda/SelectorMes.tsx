import { useEffect, useRef, useState } from 'react';
import { BotonNavFecha, ChevronDownIcon } from '../../components/iconos';
import { MESES } from '../cuotas/types';
import s from './SelectorMes.module.css';

interface Props {
  anio: number;
  mes: number;
  onChange: (anio: number, mes: number) => void;
}

/** Selector de mes propio (no un <select> nativo). El popover entra con opacity + translateY. */
export default function SelectorMes({ anio, mes, onChange }: Props) {
  const [abierto, setAbierto] = useState(false);
  const [anioVista, setAnioVista] = useState(anio);
  const wrap = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (abierto) setAnioVista(anio);
  }, [abierto, anio]);

  useEffect(() => {
    if (!abierto) return;
    const onDoc = (e: MouseEvent) => {
      if (wrap.current && !wrap.current.contains(e.target as Node)) setAbierto(false);
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setAbierto(false);
    };
    document.addEventListener('mousedown', onDoc);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onDoc);
      document.removeEventListener('keydown', onKey);
    };
  }, [abierto]);

  return (
    <div className={s.wrap} ref={wrap}>
      <button
        type="button"
        className={s.trigger}
        aria-expanded={abierto}
        aria-haspopup="dialog"
        onClick={() => setAbierto((v) => !v)}
      >
        {MESES[mes - 1]} {anio}
        <ChevronDownIcon size={16} className={abierto ? s.chevAbierto : s.chev} />
      </button>
      {abierto && (
        <div className={s.popover} role="dialog" aria-label="Elegir mes">
          <div className={s.popoverInner}>
            <div className={s.anioNav}>
              <BotonNavFecha
                direccion="anterior"
                className={s.anioBtn}
                label="Año anterior"
                onClick={() => setAnioVista((y) => y - 1)}
              />
              <span className={s.anio}>{anioVista}</span>
              <BotonNavFecha
                direccion="siguiente"
                className={s.anioBtn}
                label="Año siguiente"
                onClick={() => setAnioVista((y) => y + 1)}
              />
            </div>
            <div className={s.meses}>
              {MESES.map((nombre, i) => {
                const activo = anioVista === anio && i + 1 === mes;
                return (
                  <button
                    key={nombre}
                    type="button"
                    className={activo ? s.mesActivo : s.mes}
                    onClick={() => {
                      onChange(anioVista, i + 1);
                      setAbierto(false);
                    }}
                  >
                    {nombre.slice(0, 3)}
                  </button>
                );
              })}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
