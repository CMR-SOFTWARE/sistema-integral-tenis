import { useMemo, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { api, ApiError } from '../../lib/api';
import { obtenerSesion } from '../auth/sesion';
import SinClub from './SinClub';
import { formatoPlata } from '../alumnos/types';
import type { Servicio, Pedido } from '../cuotas/types';
import { useServiciosYPedidos } from './hooks';
import s from './PortalPages.module.css';

const ESTADO_PEDIDO_UI: Record<Pedido['estado'], { label: string; clase: string }> = {
  Pendiente: { label: 'Pendiente', clase: 'chipAmbar' },
  Aceptado: { label: 'Aceptado ✓', clase: 'chipVerde' },
  Rechazado: { label: 'Rechazado', clase: 'chipRojo' },
};

/** Servicios del club: el alumno arma un carrito, lo manda como un solo pedido y sigue sus pedidos. */
export default function ServiciosPage() {
  const conClub = obtenerSesion()?.alumno != null;
  const qc = useQueryClient();
  const query = useServiciosYPedidos();
  const servicios: Servicio[] = query.data?.servicios ?? [];
  const pedidos: Pedido[] = query.data?.pedidos ?? [];
  const cargando = query.isLoading;
  const [error, setError] = useState<string | null>(null);
  const [carrito, setCarrito] = useState<Record<string, number>>({}); // servicioId → cantidad
  const [notas, setNotas] = useState<Record<string, string>>({}); // servicioId → aclaración
  const [enviando, setEnviando] = useState(false);
  const [aviso, setAviso] = useState<string | null>(null);
  const [cancelando, setCancelando] = useState<string | null>(null); // pedidoId en curso

  const errorMostrado =
    error ?? (query.error ? (query.error.message || 'Error cargando los servicios') : null);

  const totalCarrito = useMemo(
    () => servicios.reduce((acc, sv) => acc + sv.precio * (carrito[sv.id] ?? 0), 0),
    [servicios, carrito],
  );

  if (!conClub) return <SinClub mensaje="Cuando estés en un club, acá vas a ver los servicios que ofrece (encordados, pelotas y más)." />;

  // Updaters FUNCIONALES (leen `prev`, no una `cantidad` capturada en el render):
  // dos clicks seguidos en el mismo tick (doble click, o clicks programáticos en
  // tests) tienen que sumar de a uno cada uno, no pisarse con el mismo valor.
  const incrementar = (servicioId: string) =>
    setCarrito((prev) => ({ ...prev, [servicioId]: (prev[servicioId] ?? 0) + 1 }));

  const decrementar = (servicioId: string) =>
    setCarrito((prev) => {
      const actual = (prev[servicioId] ?? 0) - 1;
      if (actual <= 0) {
        const { [servicioId]: _quitado, ...resto } = prev;
        return resto;
      }
      return { ...prev, [servicioId]: actual };
    });

  const enviarPedido = async () => {
    setError(null);
    setEnviando(true);
    try {
      const lineas = Object.entries(carrito)
        .filter(([, cantidad]) => cantidad > 0)
        .map(([servicioId, cantidad]) => ({
          servicioId,
          cantidad,
          // El back guarda null si viene vacía; se manda undefined para no ensuciar el JSON.
          nota: notas[servicioId]?.trim() || undefined,
        }));
      await api.post('/portal/pedidos', { lineas });
      setAviso('Pediste tu carrito. Tu profe lo va a confirmar.');
      setTimeout(() => setAviso(null), 3500);
      setCarrito({});
      setNotas({});
      await qc.invalidateQueries({ queryKey: ['portal-servicios'] });
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo enviar el pedido.');
    } finally {
      setEnviando(false);
    }
  };

  const cancelarPedido = async (pedidoId: string) => {
    setError(null);
    setCancelando(pedidoId);
    try {
      await api.delete(`/portal/pedidos/${pedidoId}`);
      await qc.invalidateQueries({ queryKey: ['portal-servicios'] });
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo cancelar el pedido.');
    } finally {
      setCancelando(null);
    }
  };

  return (
    <div>
      {errorMostrado && <div className={s.error}>{errorMostrado}</div>}
      {aviso && <div className={s.avisoOk}>{aviso}</div>}
      {cargando && <div className={s.vacio}>Cargando…</div>}

      {!cargando && (
        <>
          <h2 className={s.seccion}>Lo que ofrece tu club</h2>
          <div className={s.tarjeta}>
            {servicios.length === 0 && (
              <div className={s.vacio}>Tu club todavía no cargó servicios.</div>
            )}
            {servicios.map((sv) => {
              const cantidad = carrito[sv.id] ?? 0;
              return (
                <div key={sv.id}>
                  <div className={s.cargo}>
                    <span className={s.cargoConcepto}>{sv.nombre}</span>
                    <span className={s.cargoMonto}>{formatoPlata(sv.precio)}</span>
                    <div className={s.stepper}>
                      <button
                        className={s.stepperBtn}
                        disabled={cantidad === 0}
                        onClick={() => decrementar(sv.id)}
                      >
                        −
                      </button>
                      <span className={s.stepperCantidad}>{cantidad}</span>
                      <button className={s.stepperBtn} onClick={() => incrementar(sv.id)}>
                        +
                      </button>
                    </div>
                  </div>
                  {/* La aclaración aparece recién cuando el producto está en el carrito:
                      mostrarla siempre llenaría la lista de campos vacíos. */}
                  {cantidad > 0 && (
                    <input
                      className={s.notaLinea}
                      type="text"
                      maxLength={200}
                      value={notas[sv.id] ?? ''}
                      onChange={(e) => setNotas((prev) => ({ ...prev, [sv.id]: e.target.value }))}
                      placeholder="Aclaración (opcional): marca, tensión, color…"
                    />
                  )}
                </div>
              );
            })}
          </div>

          {totalCarrito > 0 && (
            <div className={s.tarjeta}>
              <div className={s.carritoResumen}>
                <span>Total del pedido</span>
                <span className={s.carritoTotal}>{formatoPlata(totalCarrito)}</span>
              </div>
              <button className={s.btnPrimario} disabled={enviando} onClick={() => void enviarPedido()}>
                {enviando ? 'Enviando…' : 'Enviar pedido'}
              </button>
            </div>
          )}

          <h2 className={s.seccion}>Mis pedidos</h2>
          <div className={s.tarjeta}>
            {pedidos.length === 0 && (
              <div className={s.vacio}>Todavía no pediste nada.</div>
            )}
            {pedidos.map((p) => {
              const ui = ESTADO_PEDIDO_UI[p.estado];
              return (
                <div key={p.id} className={s.pedidoCard}>
                  <div className={s.pedidoHeader}>
                    <span className={s.cargoFecha}>
                      {new Date(p.pedidoEl).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' })}
                    </span>
                    <div className={s.pedidoHeaderAcciones}>
                      {p.estado === 'Pendiente' && (
                        <button
                          className={s.btnCancelarPedido}
                          disabled={cancelando === p.id}
                          onClick={() => void cancelarPedido(p.id)}
                        >
                          {cancelando === p.id ? 'Cancelando…' : 'Cancelar'}
                        </button>
                      )}
                      <span className={`${s.chip} ${s[ui.clase]}`}>{ui.label}</span>
                    </div>
                  </div>
                  {p.lineas.map((l) => (
                    <div key={l.servicioId}>
                      <div className={s.cargo}>
                        <span className={s.cargoConcepto}>
                          {l.nombreServicio}{l.cantidad > 1 && ` x${l.cantidad}`}
                        </span>
                        <span className={s.cargoMonto}>{formatoPlata(l.subtotal)}</span>
                      </div>
                      {/* Su propia aclaración, para que sepa qué pidió exactamente. */}
                      {l.nota && <div className={s.notaPedida}>{l.nota}</div>}
                    </div>
                  ))}
                  <div className={s.pedidoTotal}>Total: {formatoPlata(p.total)}</div>
                </div>
              );
            })}
          </div>
          <p className={s.nota}>
            Cuando pedís algo, tu profe lo confirma y recién ahí entra en tu cuenta. Lo pagás
            desde <b>Mi cuota</b>.
          </p>
        </>
      )}
    </div>
  );
}
