import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { CAT_COLOR, CAT_LABEL, avatarColor, iniciales } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import type { SolicitudPendiente } from './types';
import s from './SolicitudesPage.module.css';

/**
 * Solicitudes de alumnos que quieren tomar clases con vos (plan v2).
 * Aprobar crea su ficha en tu club con los datos de su registro.
 */
export default function SolicitudesPage() {
  const [solicitudes, setSolicitudes] = useState<SolicitudPendiente[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const [procesando, setProcesando] = useState<string | null>(null); // id en curso

  const cargar = useCallback(() => {
    api.get<SolicitudPendiente[]>('/solicitudes')
      .then(setSolicitudes)
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando solicitudes'));
  }, []);

  useEffect(() => {
    cargar();
  }, [cargar]);

  const avisar = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 3500);
  };

  const resolver = async (sol: SolicitudPendiente, accion: 'aprobar' | 'rechazar') => {
    setProcesando(sol.id);
    try {
      await api.post(`/solicitudes/${sol.id}/${accion}`, {});
      avisar(accion === 'aprobar'
        ? `${sol.nombre} ${sol.apellido} ya es alumno de tu club 🎾`
        : `Solicitud de ${sol.nombre} rechazada.`);
      cargar();
    } catch (e) {
      avisar(e instanceof ApiError ? e.message : 'No se pudo resolver la solicitud.');
    } finally {
      setProcesando(null);
    }
  };

  if (error) return <div className={s.error}>{error} — ¿está corriendo la API?</div>;
  if (!solicitudes) return <div className={s.vacio}>Cargando…</div>;

  return (
    <div>
      <div className={s.intro}>
        Jugadores que quieren tomar clases con vos. Al aprobar, su ficha se
        crea sola con los datos de su registro (o se vincula si ya lo tenías cargado).
      </div>

      {solicitudes.length === 0 && (
        <div className={s.vacioCard}>
          No hay solicitudes pendientes. Cuando un alumno te busque desde su
          portal, la vas a ver acá.
        </div>
      )}

      <div className={s.lista}>
        {solicitudes.map((sol) => {
          const av = avatarColor(sol.nombre + sol.apellido);
          const cat = sol.categoria ? CAT_COLOR[sol.categoria as Categoria] : null;
          return (
            <div key={sol.id} className={s.tarjetaSol}>
              <div className={s.avatar} style={{ background: `${av}1a`, color: av }}>
                {iniciales(sol.nombre, sol.apellido)}
              </div>
              <div className={s.cuerpo}>
                <div className={s.nombreFila}>
                  <span className={s.nombre}>{sol.nombre} {sol.apellido}</span>
                  {cat && (
                    <span className={s.chip} style={{ background: `${cat}1a`, color: cat }}>
                      {CAT_LABEL[sol.categoria as Categoria]}
                    </span>
                  )}
                  {sol.esMenor && <span className={s.chipMenor}>Menor</span>}
                </div>
                <div className={s.detalle}>
                  {sol.email}
                  {sol.dni && ` · DNI ${sol.dni}`}
                  {sol.telefono && ` · ${sol.telefono}`}
                </div>
                {sol.mensaje && <div className={s.mensaje}>"{sol.mensaje}"</div>}
              </div>
              <div className={s.acciones}>
                <button
                  className={s.btnRechazar}
                  disabled={procesando === sol.id}
                  onClick={() => void resolver(sol, 'rechazar')}
                >
                  Rechazar
                </button>
                <button
                  className={s.btnAprobar}
                  disabled={procesando === sol.id}
                  onClick={() => void resolver(sol, 'aprobar')}
                >
                  {procesando === sol.id ? '…' : 'Aprobar'}
                </button>
              </div>
            </div>
          );
        })}
      </div>

      {toast && <div className={s.toast}>{toast}</div>}
    </div>
  );
}
