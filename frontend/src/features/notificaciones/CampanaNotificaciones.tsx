import { useState } from 'react';
import { api } from '../../lib/api';
import { useContadorNoLeidas, useInvalidarNotificaciones, useNotificaciones } from './useNotificaciones';
import s from './CampanaNotificaciones.module.css';

function tiempoRelativo(iso: string): string {
  const minutos = Math.round((Date.now() - new Date(iso).getTime()) / 60_000);
  if (minutos < 1) return 'ahora';
  if (minutos < 60) return `hace ${minutos} min`;
  const horas = Math.round(minutos / 60);
  if (horas < 24) return `hace ${horas} h`;
  return `hace ${Math.round(horas / 24)} d`;
}

/** Campana de avisos in-app: contador de no leídas (poll cada 1 min) + lista
 *  desplegable. Consume la infraestructura de Notificacion que el ranking
 *  viene alimentando desde la Fase 0 — hasta ahora invisible para el usuario. */
export default function CampanaNotificaciones() {
  const [abierta, setAbierta] = useState(false);
  const contador = useContadorNoLeidas();
  const notificaciones = useNotificaciones(abierta);
  const invalidar = useInvalidarNotificaciones();

  const marcarTodasLeidas = async () => {
    await api.post('/notificaciones/marcar-todas-leidas', {});
    invalidar();
  };

  return (
    <div className={s.contenedor}>
      <button className={s.boton} onClick={() => setAbierta((v) => !v)} aria-label="Notificaciones">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
          <path d="M13.73 21a2 2 0 0 1-3.46 0" />
        </svg>
        {!!contador.data && contador.data > 0 && (
          <span className={s.badge}>{contador.data > 9 ? '9+' : contador.data}</span>
        )}
      </button>

      {abierta && (
        <>
          <button className={s.backdrop} onClick={() => setAbierta(false)} aria-label="Cerrar" />
          <div className={s.panel}>
            <div className={s.panelHeader}>
              <span>Notificaciones</span>
              <button className={s.marcarLeidas} onClick={() => void marcarTodasLeidas()}>
                Marcar todas leídas
              </button>
            </div>
            {notificaciones.data?.length === 0 && <div className={s.vacio}>No tenés notificaciones.</div>}
            {notificaciones.data?.map((n) => (
              <div key={n.id} className={s.fila}>
                <span className={n.leida ? s.puntoLeida : s.puntoNoLeida} />
                <div className={s.filaTexto}>
                  <div className={s.mensaje}>{n.mensaje}</div>
                  <div className={s.fecha}>{tiempoRelativo(n.creadaEl)}</div>
                </div>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
