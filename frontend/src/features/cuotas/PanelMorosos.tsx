import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { formatoPlata } from '../alumnos/types';
import s from './PanelMorosos.module.css';

interface Moroso {
  id: string;
  nombre: string;
  apellido: string;
  telefono: string;
  deuda: number;
  mesesAdeudados: string;
}

/**
 * Alumnos a los que se les pasó el día 15 sin pagar. Sacarlos del calendario
 * NUNCA es automático: el profe decide (quizás sabe que le pagan mañana).
 * "Pausar" reusa el mismo mecanismo de la ficha: sale de sus turnos futuros
 * pero se le guarda el lugar; al reactivarlo vuelve solo.
 */
export default function PanelMorosos({ onCambio }: { onCambio?: () => void }) {
  const [morosos, setMorosos] = useState<Moroso[]>([]);
  const [abierto, setAbierto] = useState(false);
  const [pausando, setPausando] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const cargar = useCallback(() => {
    api.get<Moroso[]>('/alumnos/morosos').then(setMorosos).catch(() => setMorosos([]));
  }, []);

  useEffect(() => {
    cargar();
  }, [cargar]);

  const pausar = async (m: Moroso) => {
    if (!window.confirm(
      `¿Sacar a ${m.nombre} ${m.apellido} del calendario? Queda pausado y se liberan sus turnos futuros (sus cargos impagos se borran). Cuando pague, lo reactivás y vuelve solo.`,
    )) return;

    setError(null);
    setPausando(m.id);
    try {
      await api.patch(`/alumnos/${m.id}/estado`, { estado: 'Suspendido' });
      cargar();
      onCambio?.(); // la liquidación cambió: sus cargos impagos se fueron
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo pausar al alumno.');
    } finally {
      setPausando(null);
    }
  };

  const textoWhatsapp = (m: Moroso) =>
    encodeURIComponent(
      `Hola ${m.nombre}! Te recuerdo que tenés pendiente la cuota de ${m.mesesAdeudados} (${formatoPlata(m.deuda)}). ¡Gracias!`,
    );

  if (morosos.length === 0) return null;

  return (
    <div className={s.panel}>
      <button className={s.cabecera} onClick={() => setAbierto((v) => !v)}>
        <span className={s.alerta}>!</span>
        <span className={s.titulo}>
          {morosos.length} {morosos.length === 1 ? 'alumno adeuda' : 'alumnos adeudan'} la cuota hace más de 5 días
        </span>
        <span className={s.chevron}>{abierto ? '▲' : '▼'}</span>
      </button>

      {abierto && (
        <div className={s.cuerpo}>
          <p className={s.bajada}>
            Podés recordarles por WhatsApp o sacarlos del calendario hasta que
            regularicen (quedan pausados y su lugar se les guarda).
          </p>
          {error && <div className={s.error}>{error}</div>}

          {morosos.map((m) => (
            <div key={m.id} className={s.fila}>
              <div className={s.info}>
                <div className={s.nombre}>{m.nombre} {m.apellido}</div>
                <div className={s.detalle}>
                  {formatoPlata(m.deuda)} · {m.mesesAdeudados}
                </div>
              </div>
              <a
                className={s.btnWhatsapp}
                href={`https://wa.me/${m.telefono.replace(/\D/g, '')}?text=${textoWhatsapp(m)}`}
                target="_blank"
                rel="noreferrer"
              >
                Recordar
              </a>
              <button
                className={s.btnPausar}
                disabled={pausando === m.id}
                onClick={() => void pausar(m)}
              >
                {pausando === m.id ? '…' : 'Sacar del calendario'}
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
