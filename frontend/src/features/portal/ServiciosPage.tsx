import { useState } from 'react';
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

/** Servicios del club: el alumno ve el catálogo, pide, y sigue sus pedidos. */
export default function ServiciosPage() {
  const conClub = obtenerSesion()?.alumno != null;
  const qc = useQueryClient();
  const query = useServiciosYPedidos();
  const servicios: Servicio[] = query.data?.servicios ?? [];
  const pedidos: Pedido[] = query.data?.pedidos ?? [];
  const cargando = query.isLoading;
  const [error, setError] = useState<string | null>(null); // errores de "Pedir"
  const [pidiendo, setPidiendo] = useState<string | null>(null); // servicioId en curso
  const [aviso, setAviso] = useState<string | null>(null);

  const errorMostrado =
    error ?? (query.error ? (query.error.message || 'Error cargando los servicios') : null);

  if (!conClub) return <SinClub mensaje="Cuando estés en un club, acá vas a ver los servicios que ofrece (encordados, pelotas y más)." />;

  const pedir = async (servicio: Servicio) => {
    setPidiendo(servicio.id);
    setError(null);
    try {
      await api.post('/portal/pedidos', { servicioId: servicio.id });
      setAviso(`Pediste "${servicio.nombre}". Tu profe lo va a confirmar.`);
      setTimeout(() => setAviso(null), 3500);
      await qc.invalidateQueries({ queryKey: ['portal-servicios'] });
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo enviar el pedido.');
    } finally {
      setPidiendo(null);
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
            {servicios.map((sv) => (
              <div key={sv.id} className={s.cargo}>
                <span className={s.cargoConcepto}>{sv.nombre}</span>
                <span className={s.cargoMonto}>{formatoPlata(sv.precio)}</span>
                <button
                  className={s.btnAvisarCargo}
                  disabled={pidiendo === sv.id}
                  onClick={() => void pedir(sv)}
                >
                  {pidiendo === sv.id ? 'Pidiendo…' : 'Pedir'}
                </button>
              </div>
            ))}
          </div>

          <h2 className={s.seccion}>Mis pedidos</h2>
          <div className={s.tarjeta}>
            {pedidos.length === 0 && (
              <div className={s.vacio}>Todavía no pediste nada.</div>
            )}
            {pedidos.map((p) => {
              const ui = ESTADO_PEDIDO_UI[p.estado];
              return (
                <div key={p.id} className={s.cargo}>
                  <span className={s.cargoFecha}>
                    {new Date(p.pedidoEl).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' })}
                  </span>
                  <span className={s.cargoConcepto}>{p.nombreServicio}</span>
                  <span className={s.cargoMonto}>{formatoPlata(p.precio)}</span>
                  <span className={`${s.chip} ${s[ui.clase]}`}>{ui.label}</span>
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
