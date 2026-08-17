import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api, ApiError } from '../../lib/api';
import { formatoPlata } from '../alumnos/types';
import type { Pedido } from '../cuotas/types';
import s from './PedidosPage.module.css';

/**
 * La bandeja del Shop: lo que los alumnos pidieron y el profe todavía no resolvió.
 * Vivía apretada arriba de Finanzas → Cuotas, mezclada con la cobranza; acá tiene
 * lugar para leer las aclaraciones antes de aceptar.
 *
 * ACEPTAR le hace nacer el cargo al alumno (por eso queda pegado a las cuotas);
 * RECHAZAR lo descarta sin deuda.
 */
export default function PedidosPage() {
  const qc = useQueryClient();
  const [resolviendo, setResolviendo] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ['pedidos-pendientes'],
    queryFn: () => api.get<Pedido[]>('/pedidos/pendientes'),
  });
  const pedidos = query.data ?? [];

  const resolver = async (p: Pedido, accion: 'aceptar' | 'rechazar') => {
    setError(null);
    setResolviendo(p.id);
    try {
      await api.post(`/pedidos/${p.id}/${accion}`, {});
      await qc.invalidateQueries({ queryKey: ['pedidos-pendientes'] });
      // El contador del badge y el del inicio salen del mismo endpoint barato.
      await qc.invalidateQueries({ queryKey: ['pedidos-pendientes-cuenta'] });
      // Aceptar hace nacer un cargo: la liquidación del mes quedó vieja. No dispara
      // un request ahora (Finanzas no está montada), solo la marca para recargar.
      if (accion === 'aceptar') await qc.invalidateQueries({ queryKey: ['cuotas'] });
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo resolver el pedido.');
    } finally {
      setResolviendo(null);
    }
  };

  return (
    <div className={s.contenedor}>
      <div className={s.tarjeta}>
        <h3 className={s.titulo}>Pedidos sin resolver</h3>
        <p className={s.bajada}>
          Lo que te pidieron desde el <b>Shop</b>. Aceptá los que vas a hacer (le nace el cargo
          en su cuenta y lo cobrás desde <b>Finanzas</b>) o rechazá los que no.
        </p>
        {error && <div className={s.error}>{error}</div>}
        {query.isLoading && <div className={s.vacio}>Cargando…</div>}
        {!query.isLoading && pedidos.length === 0 && (
          <div className={s.vacio}>No tenés pedidos sin resolver.</div>
        )}
      </div>

      <div className={s.lista}>
        {pedidos.map((p) => (
          <div key={p.id} className={s.pedido}>
            <div className={s.cabecera}>
              <span className={s.alumno}>{p.alumnoNombre}</span>
              <span className={s.fecha}>
                {new Date(p.pedidoEl).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' })}
              </span>
            </div>

            {p.lineas.map((l) => (
              <div key={l.servicioId} className={s.linea}>
                <div className={s.lineaFila}>
                  <span className={s.lineaNombre}>
                    {l.nombreServicio}{l.cantidad > 1 && ` x${l.cantidad}`}
                  </span>
                  <span className={s.lineaMonto}>{formatoPlata(l.subtotal)}</span>
                </div>
                {/* La aclaración es la mitad que importa del pedido (qué cuerda, qué
                    tensión): en ámbar, porque es lo que hay que leer antes de aceptar. */}
                {l.nota && <div className={s.nota}>{l.nota}</div>}
              </div>
            ))}

            <div className={s.total}>
              <span>Total</span>
              <span className={s.totalMonto}>{formatoPlata(p.total)}</span>
            </div>

            <div className={s.acciones}>
              <button
                className={s.btnRechazar}
                disabled={resolviendo === p.id}
                onClick={() => void resolver(p, 'rechazar')}
              >
                Rechazar
              </button>
              <button
                className={s.btnAceptar}
                disabled={resolviendo === p.id}
                onClick={() => void resolver(p, 'aceptar')}
              >
                {resolviendo === p.id ? '…' : 'Aceptar'}
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
