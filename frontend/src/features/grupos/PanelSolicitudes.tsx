import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import s from './PanelSolicitudes.module.css';

interface SolicitudGrupo {
  id: string;
  alumnoNombre: string;
  grupoNombre: string;
}

interface Props {
  /** El padre recarga los grupos cuando se acepta (el nuevo integrante aparece). */
  onCambio: () => void;
}

/** Solicitudes de alumnos para sumarse a un grupo (M5a): el profe acepta o rechaza. */
export default function PanelSolicitudes({ onCambio }: Props) {
  const [solicitudes, setSolicitudes] = useState<SolicitudGrupo[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [resolviendo, setResolviendo] = useState<string | null>(null);

  const cargar = useCallback(() => {
    api.get<SolicitudGrupo[]>('/grupos/solicitudes')
      .then(setSolicitudes)
      .catch(() => setSolicitudes([]));
  }, []);

  useEffect(() => { cargar(); }, [cargar]);

  const resolver = async (sol: SolicitudGrupo, accion: 'aceptar' | 'rechazar') => {
    setResolviendo(sol.id);
    setError(null);
    try {
      await api.post(`/grupos/solicitudes/${sol.id}/${accion}`, {});
      cargar();
      if (accion === 'aceptar') onCambio();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo resolver la solicitud.');
    } finally {
      setResolviendo(null);
    }
  };

  if (solicitudes.length === 0) return null;

  return (
    <div className={s.panel}>
      <div className={s.titulo}>
        <span className={s.badge}>{solicitudes.length}</span>
        Pedidos de sumarse a un grupo
      </div>
      {error && <div className={s.error}>{error}</div>}
      {solicitudes.map((sol) => (
        <div key={sol.id} className={s.fila}>
          <span className={s.texto}>
            <b>{sol.alumnoNombre}</b> quiere sumarse a <b>{sol.grupoNombre}</b>
          </span>
          <div className={s.acciones}>
            <button className={s.btnRechazar} disabled={resolviendo === sol.id} onClick={() => void resolver(sol, 'rechazar')}>
              Rechazar
            </button>
            <button className={s.btnAceptar} disabled={resolviendo === sol.id} onClick={() => void resolver(sol, 'aceptar')}>
              {resolviendo === sol.id ? '…' : 'Aceptar'}
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}
