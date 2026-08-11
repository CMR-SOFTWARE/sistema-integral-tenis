import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api, ApiError } from '../../lib/api';
// Mismo look que el panel de pedidos de clase individual: son dos bandejas de
// entrada iguales, apiladas arriba del calendario.
import s from './PanelSolicitudesHorario.module.css';

interface SolicitudCupo {
  id: string;
  alumnoNombre: string;
  claseNombre: string;
}

interface Props {
  /** El padre recarga los horarios cuando se acepta (el nuevo alumno aparece). */
  onCambio: () => void;
}

/** Pedidos de lugar en una clase con cupo: el profe acepta (lo suma) o rechaza. */
export default function PanelSolicitudesCupo({ onCambio }: Props) {
  const [error, setError] = useState<string | null>(null);
  const [resolviendo, setResolviendo] = useState<string | null>(null);
  const qc = useQueryClient();

  // Cacheada: la bandeja se re-arma cada vez que el owner entra a la Agenda y casi
  // siempre vuelve vacía. Las acciones de abajo la invalidan.
  const { data: solicitudes = [] } = useQuery({
    queryKey: ['solicitudes-cupo'],
    queryFn: () => api.get<SolicitudCupo[]>('/horarios/solicitudes-cupo'),
  });
  // Resolver acá también mueve la lista de espera y su badge: el mismo pedido se
  // ve desde las dos pantallas.
  const cargar = () => Promise.all([
    qc.invalidateQueries({ queryKey: ['solicitudes-cupo'] }),
    qc.invalidateQueries({ queryKey: ['solicitudes'] }),
    qc.invalidateQueries({ queryKey: ['solicitudes-conteo'] }),
  ]);

  const resolver = async (sol: SolicitudCupo, accion: 'aceptar' | 'rechazar') => {
    setResolviendo(sol.id);
    setError(null);
    try {
      await api.post(`/horarios/solicitudes-cupo/${sol.id}/${accion}`, {});
      await cargar();
      if (accion === 'aceptar') onCambio();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo resolver el pedido.');
    } finally {
      setResolviendo(null);
    }
  };

  if (solicitudes.length === 0) return null;

  return (
    <div className={s.panel}>
      <div className={s.titulo}>
        <span className={s.badge}>{solicitudes.length}</span>
        Pedidos de lugar en una clase
      </div>
      {error && <div className={s.error}>{error}</div>}
      {solicitudes.map((sol) => (
        <div key={sol.id} className={s.fila}>
          <span className={s.texto}>
            <b>{sol.alumnoNombre}</b> quiere sumarse a <b>{sol.claseNombre}</b>
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
