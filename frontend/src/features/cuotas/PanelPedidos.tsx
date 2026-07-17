import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { formatoPlata } from '../alumnos/types';
import type { Pedido } from './types';
import s from './PanelMorosos.module.css';

/**
 * Pedidos de servicios pendientes (M4). El profe ACEPTA (nace el cargo en la
 * cuenta del alumno, entra en la liquidación de abajo) o RECHAZA (sin deuda).
 * Reusa el estilo del panel de morosos para que Cuotas se sienta un solo lugar.
 */
export default function PanelPedidos({ onCambio }: { onCambio?: () => void }) {
  const [pedidos, setPedidos] = useState<Pedido[]>([]);
  const [abierto, setAbierto] = useState(true);
  const [resolviendo, setResolviendo] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const cargar = useCallback(() => {
    api.get<Pedido[]>('/pedidos/pendientes').then(setPedidos).catch(() => setPedidos([]));
  }, []);

  useEffect(() => { cargar(); }, [cargar]);

  const resolver = async (p: Pedido, accion: 'aceptar' | 'rechazar') => {
    setError(null);
    setResolviendo(p.id);
    try {
      await api.post(`/pedidos/${p.id}/${accion}`, {});
      cargar();
      if (accion === 'aceptar') onCambio?.(); // nació un cargo: la liquidación cambió
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo resolver el pedido.');
    } finally {
      setResolviendo(null);
    }
  };

  if (pedidos.length === 0) return null;

  return (
    <div className={s.panel}>
      <button className={s.cabecera} onClick={() => setAbierto((v) => !v)}>
        <span className={s.alerta}>!</span>
        <span className={s.titulo}>
          {pedidos.length} {pedidos.length === 1 ? 'pedido de servicio' : 'pedidos de servicios'} sin resolver
        </span>
        <span className={s.chevron}>{abierto ? '▲' : '▼'}</span>
      </button>

      {abierto && (
        <div className={s.cuerpo}>
          <p className={s.bajada}>
            Aceptá los que vas a hacer (les nace el cargo en su cuenta) o rechazá los que no.
          </p>
          {error && <div className={s.error}>{error}</div>}

          {pedidos.map((p) => (
            <div key={p.id} className={s.fila}>
              <div className={s.info}>
                <div className={s.nombre}>{p.alumnoNombre}</div>
                <div className={s.detalle}>
                  {p.nombreServicio} · {formatoPlata(p.precio)}
                </div>
              </div>
              <button
                className={s.btnWhatsapp}
                disabled={resolviendo === p.id}
                onClick={() => void resolver(p, 'aceptar')}
              >
                {resolviendo === p.id ? '…' : 'Aceptar'}
              </button>
              <button
                className={s.btnPausar}
                disabled={resolviendo === p.id}
                onClick={() => void resolver(p, 'rechazar')}
              >
                Rechazar
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
