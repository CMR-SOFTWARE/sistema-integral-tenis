import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api, ApiError } from '../../lib/api';
import { haceCuanto } from '../alumnos/types';
import FiltrosAlumnos from '../alumnos/FiltrosAlumnos';
import TablaAlumnos from '../alumnos/TablaAlumnos';
import DetalleAlumnoModal from '../alumnos/DetalleAlumnoModal';
import EditarAlumnoModal from '../alumnos/EditarAlumnoModal';
import { useFiltrosAlumnos } from '../alumnos/useFiltrosAlumnos';
import { useEditarAlumno } from '../alumnos/useAlumnos';
import { obtenerSesion } from '../auth/sesion';
import type { SolicitudPendiente } from './types';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import s from './SolicitudesPage.module.css';

/**
 * Lista de espera: quién está esperando una clase. Es la MISMA tabla que Alumnos
 * —mismos datos, mismos filtros, misma ficha— con una columna que dice por qué está
 * y desde cuándo, porque el motivo es lo que decide qué se puede hacer con la fila:
 *
 *  - **Sin clase**: se anotó (o lo cargó el profe) y todavía no le asignaron nada.
 *    Se le asigna un horario, o se lo saca de la academia (borra la ficha).
 *  - **Pidió cupo**: pidió sumarse a una clase desde su portal. Puede ser un alumno
 *    que ya viene —por eso está acá Y en Alumnos—. Se acepta desde la Agenda; acá
 *    se puede rechazar, que NO le toca la ficha.
 *  - **Lo anotó el profe**: ya es alumno y le pidió otra clase hablando. Sacarlo de
 *    la espera solo apaga esa marca.
 */
export default function SolicitudesPage() {
  const [toast, setToast] = useState<string | null>(null);
  const [procesando, setProcesando] = useState<string | null>(null); // id en curso
  const [detalle, setDetalle] = useState<SolicitudPendiente | null>(null);
  const [editando, setEditando] = useState<SolicitudPendiente | null>(null);
  const confirmar = useConfirmar();
  const qc = useQueryClient();
  const filtros = useFiltrosAlumnos();
  // La ficha se edita desde acá igual que desde Alumnos: estar sin horario asignado
  // no es motivo para no poder corregirle el teléfono ni cargarle la cuota. Es del
  // dueño, como en el listado; el empleado ve la ficha pero no la toca.
  const esOwner = obtenerSesion()?.rol === 'owner';
  const editarAlumno = useEditarAlumno();

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

  /**
   * Lo anotó el profe: sacarlo de la espera solo apaga esa marca. Sigue siendo alumno
   * con sus clases, así que va sin confirmación (no se pierde nada).
   */
  const sacarDeLaEspera = (sol: SolicitudPendiente) =>
    ejecutar(
      sol.id,
      () => api.post(`/solicitudes/${sol.id}/quitar`, {}),
      `${sol.nombre} salió de la lista de espera.`,
    );

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

  const visibles = filtros.aplicar(espera);

  return (
    <div>
      {/* Sin el select de estado: acá son todos activos, sería un control muerto. */}
      <FiltrosAlumnos
        filtros={filtros}
        conEstado={false}
        contador={`${visibles.length} esperando`}
      />

      <div className={s.tarjeta}>
        <TablaAlumnos
          alumnos={visibles}
          columna={{
            titulo: 'Espera',
            // Desde cuándo espera, que es lo que ordena la cola. El motivo NO se escribe:
            // "Sin clase asignada" y "Lo anotaste vos" ocupaban una línea para decir algo
            // que la fila ya muestra —los botones cambian según el motivo, y al anotado a
            // mano lo marca el chip "En espera"—. Solo se nombra la clase del que pidió
            // cupo, que es el único dato que no está en ninguna otra parte de la fila.
            render: (sol) => (
              <span className={s.motivo}>
                {sol.motivo === 'PidioCupo' && <>Pidió <b>{sol.clase ?? 'una clase'}</b> · </>}
                <span className={s.desde}>{haceCuanto(sol.esperaDesde.slice(0, 10))}</span>
              </span>
            ),
          }}
          acciones={(sol) => {
            const enCurso = procesando === sol.id;
            return (
              <>
                {/* La ficha: es donde viven el mail, el teléfono y la nota con la que se
                    anotó (la tabla no los muestra, igual que en Alumnos). */}
                <button className={s.accionIcono} title="Ver ficha" onClick={() => setDetalle(sol)}>
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7-10-7-10-7z" /><circle cx="12" cy="12" r="3" />
                  </svg>
                </button>
                {/* Aceptar (o asignar) vive en la Agenda, que ya lo resuelve: no se duplica. */}
                <Link to="/agenda" className={s.btnFila}>
                  {sol.motivo === 'PidioCupo' ? 'Ver en Agenda' : 'Asignar horario'}
                </Link>
                {sol.motivo === 'PidioCupo' ? (
                  <button className={s.btnFilaRojo} disabled={enCurso} onClick={() => void rechazar(sol)}>
                    {enCurso ? '…' : 'Rechazar'}
                  </button>
                ) : sol.motivo === 'LoAnotoElProfe' ? (
                  // Ya es alumno: sacarlo de la espera NO le toca la ficha, así que
                  // acá no va ni confirmación ni el botón rojo.
                  <button className={s.btnFila} disabled={enCurso} onClick={() => void sacarDeLaEspera(sol)}>
                    {enCurso ? '…' : 'Sacar de la espera'}
                  </button>
                ) : (
                  <button className={s.btnFilaRojo} disabled={enCurso} onClick={() => void eliminar(sol)}>
                    {enCurso ? '…' : 'Eliminar'}
                  </button>
                )}
              </>
            );
          }}
          vacio={espera.length === 0 && !filtros.hayFiltros
            ? 'No hay nadie esperando. Cuando alguien se una desde su portal —o lo cargues sin asignarle clase— aparece acá.'
            : 'No se encontraron resultados con ese filtro o búsqueda.'}
        />
      </div>

      {detalle && (
        <DetalleAlumnoModal
          alumno={detalle}
          hermanos={espera.filter((o) => o.familiaId && o.familiaId === detalle.familiaId && o.id !== detalle.id)}
          onClose={() => setDetalle(null)}
          onEditar={esOwner ? (a) => { setDetalle(null); setEditando(a as SolicitudPendiente); } : undefined}
        />
      )}

      {editando && (
        <EditarAlumnoModal
          alumno={editando}
          onClose={() => setEditando(null)}
          onEditar={async (id, dto) => {
            await editarAlumno(id, dto);
            avisar(`${dto.nombre} ${dto.apellido} actualizado`);
          }}
        />
      )}

      {toast && <div className={s.toast}>{toast}</div>}
    </div>
  );
}
