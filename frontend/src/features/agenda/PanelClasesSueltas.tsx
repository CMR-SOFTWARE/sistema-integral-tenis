import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { horaCorta } from './types';
import { formatoPlata } from '../alumnos/types';
import s from './PanelSolicitudesHorario.module.css';

interface ClaseSuelta {
  id: string;
  alumnoNombre: string;
  sede: string;
  fecha: string; // "2026-07-25"
  horaInicio: string;
  duracionMinutos: number;
  monto: number;
  pagoInformado: boolean;
}
interface CanchaLibre {
  canchaId: string;
  cancha: string;
  sede: string;
}

interface Props {
  /** El padre recarga la semana cuando se confirma (aparece el turno suelto). */
  onCambio: () => void;
}

const fechaCorta = (iso: string) =>
  new Date(`${iso}T00:00:00`).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' });

/** Clases sueltas pendientes (M5c): el profe elige cancha y confirma (o rechaza). */
export default function PanelClasesSueltas({ onCambio }: Props) {
  const [clases, setClases] = useState<ClaseSuelta[]>([]);
  const [canchas, setCanchas] = useState<Record<string, CanchaLibre[]>>({});
  const [elegida, setElegida] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [ocupado, setOcupado] = useState<string | null>(null);

  const cargar = useCallback(() => {
    api.get<ClaseSuelta[]>('/clases-sueltas').then(setClases).catch(() => setClases([]));
  }, []);

  useEffect(() => { cargar(); }, [cargar]);

  useEffect(() => {
    clases.forEach((c) => {
      if (canchas[c.id]) return;
      api.get<CanchaLibre[]>(`/clases-sueltas/${c.id}/canchas-libres`)
        .then((libres) => {
          setCanchas((m) => ({ ...m, [c.id]: libres }));
          if (libres.length > 0) setElegida((e) => ({ ...e, [c.id]: libres[0].canchaId }));
        })
        .catch(() => setCanchas((m) => ({ ...m, [c.id]: [] })));
    });
  }, [clases, canchas]);

  const confirmar = async (c: ClaseSuelta) => {
    const canchaId = elegida[c.id];
    if (!canchaId) return;
    setOcupado(c.id); setError(null);
    try {
      await api.post(`/clases-sueltas/${c.id}/confirmar`, { canchaId });
      cargar(); onCambio();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo confirmar.');
    } finally { setOcupado(null); }
  };

  const rechazar = async (c: ClaseSuelta) => {
    setOcupado(c.id); setError(null);
    try {
      await api.post(`/clases-sueltas/${c.id}/rechazar`, {});
      cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo rechazar.');
    } finally { setOcupado(null); }
  };

  if (clases.length === 0) return null;

  return (
    <div className={s.panel}>
      <div className={s.titulo}>
        <span className={s.badge}>{clases.length}</span>
        Clases sueltas para confirmar
      </div>
      {error && <div className={s.error}>{error}</div>}
      {clases.map((c) => {
        const libres = canchas[c.id];
        return (
          <div key={c.id} className={s.fila}>
            <span className={s.texto}>
              <b>{c.alumnoNombre}</b> — {fechaCorta(c.fecha)} {horaCorta(c.horaInicio)} ({c.duracionMinutos}') en <b>{c.sede}</b> · {formatoPlata(c.monto)}
              {c.pagoInformado
                ? <span style={{ color: '#0e6b3c', fontWeight: 700 }}> · informó pago ✓</span>
                : <span style={{ color: '#b7791f', fontWeight: 700 }}> · sin avisar pago</span>}
            </span>
            <div className={s.acciones}>
              {libres && libres.length === 0 ? (
                <span className={s.sinCancha}>Sin canchas libres ese día</span>
              ) : (
                <select
                  className={s.select}
                  value={elegida[c.id] ?? ''}
                  onChange={(e) => setElegida((el) => ({ ...el, [c.id]: e.target.value }))}
                  disabled={!libres}
                >
                  {!libres && <option>Cargando…</option>}
                  {libres?.map((x) => (
                    <option key={x.canchaId} value={x.canchaId}>{x.cancha} · {x.sede}</option>
                  ))}
                </select>
              )}
              <button className={s.btnRechazar} disabled={ocupado === c.id} onClick={() => void rechazar(c)}>
                Rechazar
              </button>
              <button
                className={s.btnAceptar}
                disabled={ocupado === c.id || !libres || libres.length === 0}
                onClick={() => void confirmar(c)}
              >
                {ocupado === c.id ? '…' : 'Confirmar'}
              </button>
            </div>
          </div>
        );
      })}
    </div>
  );
}
