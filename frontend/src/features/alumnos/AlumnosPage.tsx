import { useState } from 'react';
import { useAlumnos } from './useAlumnos';
import NuevoAlumnoModal from './NuevoAlumnoModal';
import EditarAlumnoModal from './EditarAlumnoModal';
import DetalleAlumnoModal from './DetalleAlumnoModal';
import AccesoCreadoModal from './AccesoCreadoModal';
import { ApiError } from '../../lib/api';
import Avatar from '../../components/Avatar';
import { CATEGORIAS, CAT_COLOR, CAT_LABEL, ESTADO_UI } from './types';
import type { Alumno, Categoria, Estado } from './types';
import s from './AlumnosPage.module.css';

interface Credenciales {
  nombre: string;
  email: string;
  passwordTemporal: string;
}

export default function AlumnosPage() {
  const [filtro, setFiltro] = useState<Categoria | 'todas'>('todas');
  const [filtroEstado, setFiltroEstado] = useState<Estado | 'todos'>('todos');
  const { alumnos, cargando, error, crear, crearAcceso, editar, cambiarEstado, darDeBaja } =
    useAlumnos(filtro, filtroEstado);
  const [modalNuevo, setModalNuevo] = useState(false);
  const [editando, setEditando] = useState<Alumno | null>(null);
  const [detalle, setDetalle] = useState<Alumno | null>(null);
  const [credenciales, setCredenciales] = useState<Credenciales | null>(null);
  const [toast, setToast] = useState<string | null>(null);

  const avisar = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 2600);
  };

  /** "Crear acceso" para fichas viejas sin usuario (desde la ficha). */
  const accesoParaFicha = async (a: Alumno) => {
    let email = a.email ?? undefined;
    if (!email) {
      email = window.prompt('La ficha no tiene email. Ingresá el email del alumno:')?.trim();
      if (!email) return;
    }
    try {
      const acceso = await crearAcceso(a.id, a.email ? undefined : email);
      setDetalle(null);
      setCredenciales({ nombre: `${a.nombre} ${a.apellido}`, ...acceso });
    } catch (e) {
      avisar(e instanceof ApiError ? e.message : 'No se pudo crear el acceso.');
    }
  };

  const pausarOReactivar = async (a: Alumno) => {
    const pausar = a.estado === 'Activo';
    if (pausar && !window.confirm(
      `¿Pausar a ${a.nombre} ${a.apellido}? Sale de sus turnos futuros y deja de pagarlos, pero le guardamos su lugar: al reactivarlo vuelve solo.`,
    )) return;

    await cambiarEstado(a.id, pausar ? 'Suspendido' : 'Activo');
    avisar(pausar
      ? `${a.nombre} pausado y fuera del calendario`
      : `${a.nombre} reactivado: vuelve a sus turnos`);
  };

  const baja = async (a: Alumno) => {
    if (!window.confirm(
      `¿Dar de baja a ${a.nombre} ${a.apellido}? Sale del calendario, de sus grupos (se libera el cupo) y se desactivan sus horarios individuales. El historial se conserva.`,
    )) return;
    await darDeBaja(a.id);
    avisar(`${a.nombre} dado de baja y fuera del calendario`);
  };

  return (
    <div>
      <div className={s.toolbar}>
        <div className={s.filtros}>
          <button
            className={filtro === 'todas' ? s.filtroActivo : s.filtro}
            onClick={() => setFiltro('todas')}
          >
            Todas
          </button>
          {CATEGORIAS.map((c) => (
            <button
              key={c}
              className={filtro === c ? s.filtroActivo : s.filtro}
              onClick={() => setFiltro(c)}
            >
              {CAT_LABEL[c]}
            </button>
          ))}
        </div>
        {/* Estado: por defecto se ven todos (incluidas bajas) */}
        <select
          className={s.selectEstado}
          value={filtroEstado}
          onChange={(e) => setFiltroEstado(e.target.value as Estado | 'todos')}
        >
          <option value="todos">Todos los estados</option>
          <option value="Activo">Activos</option>
          <option value="Suspendido">Pausados</option>
          <option value="Inactivo">Bajas</option>
        </select>

        <div className={s.spacer} />
        <div className={s.contador}>{alumnos.length} alumnos</div>
        <button className={s.btnNuevo} onClick={() => setModalNuevo(true)}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.5" strokeLinecap="round">
            <path d="M12 5v14M5 12h14" />
          </svg>
          Nuevo alumno
        </button>
      </div>

      <div className={s.tarjeta}>
        {error && <div className={s.error}>{error} — ¿está corriendo la API? (dotnet run)</div>}
        {cargando && !error && <div className={s.vacio}>Cargando…</div>}
        {!cargando && !error && (
          <table className={s.tabla}>
            <thead>
              <tr>
                <th>Alumno</th>
                <th>Categoría</th>
                <th>Teléfono</th>
                <th>Cuota</th>
                <th>Estado</th>
                <th className={s.thAcciones}>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {alumnos.map((a) => {
                const cat = CAT_COLOR[a.categoria];
                const estado = ESTADO_UI[a.estado];
                return (
                  <tr key={a.id}>
                    <td>
                      <div className={s.celdaAlumno}>
                        <Avatar nombre={a.nombre} apellido={a.apellido} fotoUrl={a.fotoUrl} size={40} radius={12} />
                        <div>
                          <div className={s.nombre}>{a.nombre} {a.apellido}</div>
                          <div className={s.dni}>DNI {a.dni}{a.esMenor ? ' · menor' : ''}</div>
                        </div>
                      </div>
                    </td>
                    <td>
                      <span className={s.chip} style={{ background: `${cat}1a`, color: cat }}>
                        {CAT_LABEL[a.categoria]}
                      </span>
                    </td>
                    <td className={s.tel}>{a.telefono}</td>
                    <td>
                      {a.deudaVencida ? (
                        <span className={s.chip} style={{ background: '#fdeaea', color: '#b91c1c' }}>
                          Vencida
                        </span>
                      ) : (
                        <span className={s.chip} style={{ background: '#e7f6ec', color: '#0e6b3c' }}>
                          Al día
                        </span>
                      )}
                    </td>
                    <td>
                      <span className={s.chip} style={{ background: estado.bg, color: estado.fg }}>
                        {estado.label}
                      </span>
                    </td>
                    <td>
                      <div className={s.acciones}>
                        <button className={s.accion} title="Ver ficha" onClick={() => setDetalle(a)}>
                          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                            <path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7-10-7-10-7z" /><circle cx="12" cy="12" r="3" />
                          </svg>
                        </button>
                        <button className={s.accion} title="Editar datos" onClick={() => setEditando(a)}>
                          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <path d="M11 4H4v16h16v-7M18.5 2.5a2.12 2.12 0 0 1 3 3L12 15l-4 1 1-4z" />
                          </svg>
                        </button>
                        <button
                          className={`${s.accion} ${s.accionPausa}`}
                          title={a.estado === 'Activo' ? 'Pausar' : 'Reactivar'}
                          onClick={() => void pausarOReactivar(a)}
                        >
                          {a.estado === 'Activo' ? (
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                              <path d="M10 4H6v16h4zM18 4h-4v16h4z" />
                            </svg>
                          ) : (
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinejoin="round">
                              <path d="M6 4l14 8-14 8z" />
                            </svg>
                          )}
                        </button>
                        <button className={`${s.accion} ${s.accionBaja}`} title="Dar de baja" onClick={() => void baja(a)}>
                          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                            <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
                          </svg>
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
        {!cargando && !error && alumnos.length === 0 && (
          <div className={s.vacio}>
            {filtro === 'todas'
              ? 'Todavía no hay alumnos. Creá el primero con "Nuevo alumno".'
              : 'No se encontraron alumnos con ese filtro.'}
          </div>
        )}
      </div>

      {modalNuevo && (
        <NuevoAlumnoModal
          onClose={() => setModalNuevo(false)}
          onCrear={crear}
          onCreado={(creado) => {
            setModalNuevo(false);
            setCredenciales({
              nombre: `${creado.alumno.nombre} ${creado.alumno.apellido}`,
              email: creado.email,
              passwordTemporal: creado.passwordTemporal,
            });
          }}
        />
      )}
      {editando && (
        <EditarAlumnoModal
          alumno={editando}
          onClose={() => setEditando(null)}
          onEditar={async (id, dto) => {
            await editar(id, dto);
            avisar(`${dto.nombre} ${dto.apellido} actualizado`);
          }}
        />
      )}
      {detalle && (
        <DetalleAlumnoModal
          alumno={detalle}
          onClose={() => setDetalle(null)}
          onCrearAcceso={accesoParaFicha}
        />
      )}
      {credenciales && (
        <AccesoCreadoModal
          nombre={credenciales.nombre}
          email={credenciales.email}
          passwordTemporal={credenciales.passwordTemporal}
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
