import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api, ApiError } from '../../lib/api';
import { CAT_COLOR, CAT_LABEL, avatarColor, iniciales } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import type { SolicitudPendiente } from './types';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import s from './SolicitudesPage.module.css';

/**
 * Lista de espera: quién está esperando una clase. Son dos casos distintos, y cada
 * uno tiene su acción:
 *
 *  - **Sin clase**: se anotó (o lo cargó el profe) y todavía no le asignaron nada.
 *    Se le asigna un horario, o se lo saca de la academia (borra la ficha).
 *  - **Pidió cupo**: pidió sumarse a una clase desde su portal. Puede ser un alumno
 *    que ya viene —por eso está acá Y en Alumnos—. Se acepta desde la Agenda; acá
 *    se puede rechazar, que NO le toca la ficha.
 */
export default function SolicitudesPage() {
  const [toast, setToast] = useState<string | null>(null);
  const [procesando, setProcesando] = useState<string | null>(null); // id en curso
  const confirmar = useConfirmar();
  const qc = useQueryClient();

  const query = useQuery({
    queryKey: ['solicitudes'],
    queryFn: () => api.get<SolicitudPendiente[]>('/solicitudes'),
  });
  const espera = query.data;
  const error = query.error ? (query.error.message || 'Error cargando la lista de espera') : null;

  // Resolver un pedido mueve tres cosas a la vez: esta lista, el badge de la pestaña
  // y —si era un pedido de cupo— la bandeja gemela que el profe ve en la Agenda.
  // Sacar a alguien de la academia le borra la ficha, así que también el padrón.
  const cargar = () => Promise.all([
    qc.invalidateQueries({ queryKey: ['solicitudes'] }),
    qc.invalidateQueries({ queryKey: ['solicitudes-conteo'] }),
    qc.invalidateQueries({ queryKey: ['solicitudes-cupo'] }),
    qc.invalidateQueries({ queryKey: ['alumnos'] }),
  ]);

  const avisar = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 3500);
  };

  /** Corre una acción sobre una fila mostrando el "…" y recargando al final. */
  const ejecutar = async (id: string, accion: () => Promise<unknown>, ok: string) => {
    setProcesando(id);
    try {
      await accion();
      avisar(ok);
      await cargar();
    } catch (e) {
      avisar(e instanceof ApiError ? e.message : 'No se pudo completar la acción.');
    } finally {
      setProcesando(null);
    }
  };

  /** Sin clase: sacarlo de la academia BORRA su ficha. Confirmación fuerte. */
  const eliminar = async (sol: SolicitudPendiente) => {
    const ok = await confirmar({
      titulo: `Eliminar a ${sol.nombre} ${sol.apellido}`,
      mensaje: (
        <>
          Se borra su ficha y todo lo que tenga cargado. <b>Esto no se puede deshacer.</b>{' '}
          Su cuenta se conserva: puede volver a unirse al club.
        </>
      ),
      confirmar: 'Eliminar definitivamente',
      cancelar: 'No, cancelar',
      peligro: true,
    });
    if (!ok) return;
    await ejecutar(
      sol.id,
      () => api.post(`/solicitudes/${sol.id}/quitar`, {}),
      `${sol.nombre} ${sol.apellido} salió de la academia.`,
    );
  };

  /** Pidió cupo: se rechaza EL PEDIDO; la ficha del alumno no se toca. */
  const rechazar = async (sol: SolicitudPendiente) => {
    const ok = await confirmar({
      titulo: `Rechazar el pedido de ${sol.nombre}`,
      mensaje: `No se suma a ${sol.clase ?? 'esa clase'}. Su ficha y sus otras clases quedan como están.`,
      confirmar: 'Rechazar',
      peligro: true,
    });
    if (!ok) return;
    await ejecutar(
      sol.id,
      () => api.post(`/horarios/solicitudes-cupo/${sol.solicitudId}/rechazar`, {}),
      `Pedido de ${sol.nombre} rechazado.`,
    );
  };

  if (error) return <div className={s.error}>{error}</div>;
  if (!espera) return <div className={s.vacio}>Cargando…</div>;

  return (
    <div>
      <div className={s.intro}>
        Los que están esperando una clase: los que se unieron (o cargaste) y todavía no
        tienen ninguna, y los que <b>pidieron sumarse</b> a una desde su portal. Estos
        últimos pueden ser alumnos que ya vienen, así que también los vas a ver en
        "Alumnos".
      </div>

      {espera.length === 0 && (
        <div className={s.vacioCard}>
          No hay nadie esperando. Cuando alguien se una desde su portal —o lo cargues
          sin asignarle clase— aparece acá.
        </div>
      )}

      <div className={s.lista}>
        {espera.map((sol) => {
          const av = avatarColor(sol.nombre + sol.apellido);
          const cat = sol.categoria ? CAT_COLOR[sol.categoria as Categoria] : null;
          const pidio = sol.motivo === 'PidioCupo';
          const enCurso = procesando === sol.id;
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
                  {sol.esMenor && <span className={s.chipMenor}>Con responsable</span>}
                </div>
                {/* Por qué está esperando: cambia las acciones de la derecha. */}
                <div className={s.motivo}>
                  {pidio
                    ? <>Pidió sumarse a <b>{sol.clase ?? 'una clase'}</b></>
                    : 'Todavía sin clase asignada'}
                </div>
                <div className={s.detalle}>
                  {sol.email}
                  {sol.dni && ` · DNI ${sol.dni}`}
                  {sol.telefono && ` · ${sol.telefono}`}
                </div>
                {sol.mensaje && <div className={s.mensaje}>"{sol.mensaje}"</div>}
              </div>
              <div className={s.acciones}>
                {pidio ? (
                  <>
                    {/* Aceptar vive en el panel de la Agenda, que ya existe: no se duplica. */}
                    <Link to="/agenda" className={s.btnAprobar}>Verlo en la Agenda</Link>
                    <button className={s.btnRechazar} disabled={enCurso} onClick={() => void rechazar(sol)}>
                      {enCurso ? '…' : 'Rechazar'}
                    </button>
                  </>
                ) : (
                  <>
                    <Link to="/agenda" className={s.btnAprobar}>Asignarle un horario</Link>
                    <button className={s.btnRechazar} disabled={enCurso} onClick={() => void eliminar(sol)}>
                      {enCurso ? '…' : 'Eliminar'}
                    </button>
                  </>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {toast && <div className={s.toast}>{toast}</div>}
    </div>
  );
}
