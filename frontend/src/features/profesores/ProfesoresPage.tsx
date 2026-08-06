import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { obtenerSesion } from '../auth/sesion';
import Avatar from '../../components/Avatar';
import AccesoCreadoModal from '../alumnos/AccesoCreadoModal';
import { formatoPlata } from '../alumnos/types';
import NuevoEmpleadoModal from './NuevoEmpleadoModal';
import EditarEmpleadoModal from './EditarEmpleadoModal';
import DetalleEmpleadoModal from './DetalleEmpleadoModal';
import type { Staff, StaffCreado, UpdateEmpleado } from './types';
import s from '../alumnos/AlumnosPage.module.css';

/**
 * Profes empleados (Staff) del club, con la misma UI que Alumnos: tabla con
 * buscador, ver ficha y editar. Solo el DUEÑO los gestiona (alta/baja/edición).
 */
export default function ProfesoresPage() {
  const [staff, setStaff] = useState<Staff[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busqueda, setBusqueda] = useState('');
  const [modalNuevo, setModalNuevo] = useState(false);
  const [editando, setEditando] = useState<Staff | null>(null);
  const [detalle, setDetalle] = useState<Staff | null>(null);
  const [credenciales, setCredenciales] = useState<{ nombre: string; usuario: string; passwordTemporal: string } | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const confirmar = useConfirmar();
  const esOwner = obtenerSesion()?.rol === 'owner';

  const cargar = useCallback(() => {
    setCargando(true);
    api.get<Staff[]>('/staff')
      .then((data) => { setStaff(data); setError(null); })
      .catch((e) => setError(e instanceof ApiError ? e.message : 'No se pudo cargar el equipo.'))
      .finally(() => setCargando(false));
  }, []);

  useEffect(() => { cargar(); }, [cargar]);

  const avisar = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 2800);
  };

  // Buscador (nombre/apellido/DNI/celular): client-side sobre lo ya cargado.
  const termino = busqueda.trim().toLowerCase();
  const visibles = staff.filter((p) =>
    termino === ''
    || `${p.nombre} ${p.apellido}`.toLowerCase().includes(termino)
    || (p.dni ?? '').toLowerCase().includes(termino)
    || p.telefono.toLowerCase().includes(termino),
  );

  const crear = (dto: { nombre: string; apellido: string; telefono: string; email?: string; valorHora?: number; sedeId?: string }) =>
    api.post<StaffCreado>('/staff', dto);

  // El director no tiene membresía: su ficha se edita por su propia ruta.
  const editar = async (id: string, dto: UpdateEmpleado) => {
    await api.put(editando?.esDueño ? '/staff/director' : `/staff/${id}`, dto);
    cargar();
  };

  const cambiarActivo = async (p: Staff) => {
    if (p.activo && !(await confirmar({
      titulo: 'Sacar del equipo',
      mensaje: `¿Sacar a ${p.nombre} ${p.apellido} de tu equipo? Deja de ver la academia; lo podés reactivar cuando quieras.`,
      confirmar: 'Sacar',
      peligro: true,
    }))) return;
    await api.patch(`/staff/${p.id}/activo`, { activo: !p.activo });
    cargar();
    avisar(p.activo ? `${p.nombre} salió del equipo` : `${p.nombre} reactivado`);
  };

  /** Borrado REAL: saca al profe de verdad (para los que se cargaron mal). */
  const eliminar = async (p: Staff) => {
    if (!(await confirmar({
      titulo: `Eliminar a ${p.nombre} ${p.apellido}`,
      mensaje: (
        <>
          Se borra el profe de tu equipo y su acceso a la app. Los horarios y alumnos
          que lo tenían asignado quedan <b>sin profe</b> (no se borran).{' '}
          <b>Esto no se puede deshacer.</b>
        </>
      ),
      confirmar: 'Eliminar definitivamente',
      cancelar: 'No, cancelar',
      peligro: true,
    }))) return;
    try {
      await api.delete(`/staff/${p.id}/definitivo`);
      avisar(`${p.nombre} ${p.apellido} eliminado`);
      cargar();
    } catch (e) {
      avisar(e instanceof ApiError ? e.message : 'No se pudo eliminar el profe.');
    }
  };

  return (
    <div>
      <div className={s.toolbar}>
        <input
          className={s.buscador}
          type="search"
          value={busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
          placeholder="Buscar por nombre, DNI o celular…"
        />
        <div className={s.spacer} />
        <div className={s.contador}>{visibles.length} profes</div>
        {esOwner && (
          <button className={s.btnNuevo} onClick={() => setModalNuevo(true)}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.5" strokeLinecap="round">
              <path d="M12 5v14M5 12h14" />
            </svg>
            Nuevo profe
          </button>
        )}
      </div>

      <div className={s.tarjeta}>
        {error && <div className={s.error}>{error}</div>}
        {cargando && !error && <div className={s.vacio}>Cargando…</div>}
        {!cargando && !error && (
          <table className={s.tabla}>
            <thead>
              <tr>
                <th>Profe</th>
                <th>Club</th>
                <th>Valor hora</th>
                <th>Celular</th>
                <th>Estado</th>
                <th className={s.thAcciones}>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {visibles.map((p) => (
                <tr key={p.esDueño ? 'director' : p.id}>
                  <td>
                    <div className={s.celdaAlumno}>
                      <Avatar nombre={p.nombre} apellido={p.apellido} size={40} radius={12} />
                      <div>
                        <div className={s.nombre}>
                          {p.nombre} {p.apellido}
                          {p.esDueño && <span className={s.insignia}>Director</span>}
                        </div>
                        <div className={s.dni}>{p.dni ? `DNI ${p.dni}` : 'Sin DNI'}</div>
                      </div>
                    </div>
                  </td>
                  {/* El director no tiene club fijo ni valor hora: trabaja donde haga
                      falta y no se paga sueldo a sí mismo. */}
                  <td>{p.esDueño ? 'Todos' : p.sedeNombre ?? '—'}</td>
                  <td>{p.valorHora != null ? `${formatoPlata(p.valorHora)}/h` : '—'}</td>
                  <td className={s.tel}>{p.telefono || '—'}</td>
                  <td>
                    {p.esDueño ? (
                      <span
                        className={s.chip}
                        style={p.daClases
                          ? { background: '#e7f6ec', color: '#0e6b3c' }
                          : { background: '#eef2ff', color: '#4f46e5' }}
                      >
                        {p.daClases ? 'Da clases' : 'Solo gestiona'}
                      </span>
                    ) : (
                      <span
                        className={s.chip}
                        style={p.activo
                          ? { background: '#e7f6ec', color: '#0e6b3c' }
                          : { background: '#f3f4f6', color: '#6b7280' }}
                      >
                        {p.activo ? 'Activo' : 'Inactivo'}
                      </span>
                    )}
                  </td>
                  <td>
                    <div className={s.acciones}>
                      <button className={s.accion} title="Ver ficha" onClick={() => setDetalle(p)}>
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                          <path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7-10-7-10-7z" /><circle cx="12" cy="12" r="3" />
                        </svg>
                      </button>
                      {esOwner && (
                        <>
                          <button className={s.accion} title="Editar datos" onClick={() => setEditando(p)}>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                              <path d="M11 4H4v16h16v-7M18.5 2.5a2.12 2.12 0 0 1 3 3L12 15l-4 1 1-4z" />
                            </svg>
                          </button>
                          {/* Al director no se lo saca del equipo ni se lo elimina:
                              es el dueño del club. */}
                          {!p.esDueño && (
                          <>
                          <button
                            className={`${s.accion} ${s.accionBaja}`}
                            title={p.activo ? 'Sacar del equipo (se puede reactivar)' : 'Reactivar'}
                            onClick={() => void cambiarActivo(p)}
                          >
                            {p.activo ? (
                              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM23 11h-6" />
                              </svg>
                            ) : (
                              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM20 8v6M23 11h-6" />
                              </svg>
                            )}
                          </button>
                          <button className={`${s.accion} ${s.accionEliminar}`} title="Eliminar definitivamente (no se deshace)" onClick={() => void eliminar(p)}>
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                              <polygon points="7.86 2 16.14 2 22 7.86 22 16.14 16.14 22 7.86 22 2 16.14 2 7.86" />
                              <path d="M15 9l-6 6M9 9l6 6" />
                            </svg>
                          </button>
                          </>
                          )}
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        {!cargando && !error && visibles.length === 0 && (
          <div className={s.vacio}>
            {staff.length === 0 && termino === ''
              ? 'Todavía no sumaste ningún profe. Creá el primero con "Nuevo profe".'
              : 'No se encontraron profes con esa búsqueda.'}
          </div>
        )}
      </div>

      {modalNuevo && (
        <NuevoEmpleadoModal
          onClose={() => setModalNuevo(false)}
          onCrear={crear}
          onCreado={(creado) => {
            cargar();
            if (creado.passwordTemporal) {
              setCredenciales({
                nombre: `${creado.staff.nombre} ${creado.staff.apellido}`,
                usuario: creado.usuario ?? creado.staff.email,
                passwordTemporal: creado.passwordTemporal,
              });
            } else {
              avisar(`${creado.staff.nombre} volvió a tu equipo.`);
            }
          }}
        />
      )}
      {editando && (
        <EditarEmpleadoModal
          empleado={editando}
          onClose={() => setEditando(null)}
          onEditar={async (id, dto) => {
            await editar(id, dto);
            avisar(`${dto.nombre} ${dto.apellido} actualizado`);
          }}
        />
      )}
      {detalle && (
        <DetalleEmpleadoModal empleado={detalle} onClose={() => setDetalle(null)} />
      )}
      {credenciales && (
        <AccesoCreadoModal
          nombre={credenciales.nombre}
          usuario={credenciales.usuario}
          passwordTemporal={credenciales.passwordTemporal}
          vinculado={false}
          titular={null}
          onClose={() => setCredenciales(null)}
        />
      )}

      {toast && (
        <div className={s.toast}>
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#7bed9f" strokeWidth="2.5" strokeLinecap="round">
            <path d="M20 6L9 17l-5-5" />
          </svg>
          {toast}
        </div>
      )}
    </div>
  );
}
