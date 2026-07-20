import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { DIAS, horaCorta } from './types';
import type { DiaSemana } from './types';
import s from './PanelSolicitudesHorario.module.css';

const diaLabel = (dia: DiaSemana) => DIAS.find((d) => d.valor === dia)?.label ?? dia;

interface SolicitudHorario {
  id: string;
  alumnoNombre: string;
  dia: DiaSemana;
  horaInicio: string;
  duracionMinutos: number;
  sede: string;
}
interface CanchaLibre {
  canchaId: string;
  cancha: string;
  sede: string;
}

interface Props {
  /** El padre recarga los horarios cuando se acepta (aparece el nuevo). */
  onCambio: () => void;
}

/** Solicitudes de clase individual fija (M5b): el profe elige cancha y acepta, o rechaza. */
export default function PanelSolicitudesHorario({ onCambio }: Props) {
  const [solicitudes, setSolicitudes] = useState<SolicitudHorario[]>([]);
  const [canchas, setCanchas] = useState<Record<string, CanchaLibre[]>>({});
  const [elegida, setElegida] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [ocupado, setOcupado] = useState<string | null>(null);

  const cargar = useCallback(() => {
    api.get<SolicitudHorario[]>('/horarios/solicitudes')
      .then(setSolicitudes)
      .catch(() => setSolicitudes([]));
  }, []);

  useEffect(() => { cargar(); }, [cargar]);

  // Al mostrar cada solicitud, cargamos sus canchas libres
  useEffect(() => {
    solicitudes.forEach((sol) => {
      if (canchas[sol.id]) return;
      api.get<CanchaLibre[]>(`/horarios/solicitudes/${sol.id}/canchas-libres`)
        .then((libres) => {
          setCanchas((c) => ({ ...c, [sol.id]: libres }));
          if (libres.length > 0) setElegida((e) => ({ ...e, [sol.id]: libres[0].canchaId }));
        })
        .catch(() => setCanchas((c) => ({ ...c, [sol.id]: [] })));
    });
  }, [solicitudes, canchas]);

  const aceptar = async (sol: SolicitudHorario) => {
    const canchaId = elegida[sol.id];
    if (!canchaId) return;
    setOcupado(sol.id); setError(null);
    try {
      await api.post(`/horarios/solicitudes/${sol.id}/aceptar`, { canchaId });
      cargar(); onCambio();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo aceptar.');
    } finally { setOcupado(null); }
  };

  const rechazar = async (sol: SolicitudHorario) => {
    setOcupado(sol.id); setError(null);
    try {
      await api.post(`/horarios/solicitudes/${sol.id}/rechazar`, {});
      cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo rechazar.');
    } finally { setOcupado(null); }
  };

  if (solicitudes.length === 0) return null;

  return (
    <div className={s.panel}>
      <div className={s.titulo}>
        <span className={s.badge}>{solicitudes.length}</span>
        Pedidos de clase individual
      </div>
      {error && <div className={s.error}>{error}</div>}
      {solicitudes.map((sol) => {
        const libres = canchas[sol.id];
        return (
          <div key={sol.id} className={s.fila}>
            <span className={s.texto}>
              <b>{sol.alumnoNombre}</b> quiere clase el <b>{diaLabel(sol.dia)} {horaCorta(sol.horaInicio)}</b> ({sol.duracionMinutos}') en <b>{sol.sede}</b>
            </span>
            <div className={s.acciones}>
              {libres && libres.length === 0 ? (
                <span className={s.sinCancha}>Sin canchas libres a esa hora</span>
              ) : (
                <select
                  className={s.select}
                  value={elegida[sol.id] ?? ''}
                  onChange={(e) => setElegida((el) => ({ ...el, [sol.id]: e.target.value }))}
                  disabled={!libres}
                >
                  {!libres && <option>Cargando…</option>}
                  {libres?.map((c) => (
                    <option key={c.canchaId} value={c.canchaId}>{c.cancha} · {c.sede}</option>
                  ))}
                </select>
              )}
              <button className={s.btnRechazar} disabled={ocupado === sol.id} onClick={() => void rechazar(sol)}>
                Rechazar
              </button>
              <button
                className={s.btnAceptar}
                disabled={ocupado === sol.id || !libres || libres.length === 0}
                onClick={() => void aceptar(sol)}
              >
                {ocupado === sol.id ? '…' : 'Aceptar'}
              </button>
            </div>
          </div>
        );
      })}
    </div>
  );
}
