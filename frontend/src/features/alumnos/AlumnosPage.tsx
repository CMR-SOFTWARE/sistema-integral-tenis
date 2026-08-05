import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAlumnos } from './useAlumnos';
import NuevoAlumnoModal from './NuevoAlumnoModal';
import EditarAlumnoModal from './EditarAlumnoModal';
import DetalleAlumnoModal from './DetalleAlumnoModal';
import AccesoCreadoModal from './AccesoCreadoModal';
import { ApiError } from '../../lib/api';
import Avatar from '../../components/Avatar';
import { obtenerSesion } from '../auth/sesion';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { useProfesores } from '../profesores/useProfesores';
import { CAT_COLOR, CAT_LABEL, ESTADO_UI, subPorEdad } from './types';
import CategoriaOptions from './CategoriaOptions';
import type { Alumno, Categoria, Estado } from './types';
import s from './AlumnosPage.module.css';

interface Credenciales {
  nombre: string;
  usuario: string | null;
  passwordTemporal: string | null;
  vinculado: boolean;
  titular: string | null;
}

export default function AlumnosPage() {
  const [filtro, setFiltro] = useState<Categoria | 'todas'>('todas');
  const [filtroEstado, setFiltroEstado] = useState<Estado | 'todos'>('todos');
  const { alumnos, cargando, error, crear, crearAcceso, editar, cambiarEstado, cambiarProfe, darDeBaja, eliminarDefinitivo } =
    useAlumnos(filtro, filtroEstado);
  const [busqueda, setBusqueda] = useState('');
  const [filtroProfe, setFiltroProfe] = useState<string>('todos');
  const [modalNuevo, setModalNuevo] = useState(false);
  const [editando, setEditando] = useState<Alumno | null>(null);
  const [detalle, setDetalle] = useState<Alumno | null>(null);
  const [credenciales, setCredenciales] = useState<Credenciales | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const confirmar = useConfirmar();
  const { profes } = useProfesores();
  // El profe empleado ve la lista y la ficha, pero no gestiona alumnos (eso es del dueño).
  const esOwner = obtenerSesion()?.rol === 'owner';

  // Deep-link desde el inicio (Accesos directos): /alumnos?nuevo=1 abre el alta.
  const [params, setParams] = useSearchParams();
  useEffect(() => {
    if (params.get('nuevo') !== '1') return;
    setModalNuevo(true);
    const next = new URLSearchParams(params);
    next.delete('nuevo');
    setParams(next, { replace: true });
  }, [params, setParams]);

  // Cuenta familiar: fichas que comparten familiaId (mismo login) son una familia.
  // Se calcula sobre la lista COMPLETA (el vínculo no depende de los filtros).
  const conteoFamilia = new Map<string, number>();
  for (const a of alumnos) if (a.familiaId) conteoFamilia.set(a.familiaId, (conteoFamilia.get(a.familiaId) ?? 0) + 1);
  const enFamilia = (a: Alumno) => !!a.familiaId && (conteoFamilia.get(a.familiaId) ?? 0) > 1;
  const hermanosDe = (a: Alumno) =>
    a.familiaId ? alumnos.filter((o) => o.familiaId === a.familiaId && o.id !== a.id) : [];

  // Buscador (nombre/apellido/DNI) + filtro por profe de cabecera: client-side
  // sobre la lista ya cargada (categoría/estado siguen filtrándose en el back).
  const termino = busqueda.trim().toLowerCase();
  const visibles = alumnos
    .filter((a) => {
      const coincideTexto = termino === ''
        || `${a.nombre} ${a.apellido}`.toLowerCase().includes(termino)
        || (a.dni ?? '').toLowerCase().includes(termino);
      const coincideProfe = filtroProfe === 'todos' || a.profesorUserId === filtroProfe;
      return coincideTexto && coincideProfe;
    })
    // Orden alfabético por apellido y después nombre (es-AR, sin distinguir acentos/mayús).
    .sort((a, b) =>
      a.apellido.localeCompare(b.apellido, 'es', { sensitivity: 'base' })
      || a.nombre.localeCompare(b.nombre, 'es', { sensitivity: 'base' }));

  const avisar = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 2600);
  };

  /** "Crear acceso" para fichas sin usuario (usa su celular; o uno alternativo). */
  const accesoParaFicha = async (a: Alumno, telefonoAlternativo?: string) => {
    try {
      const acceso = await crearAcceso(a.id, telefonoAlternativo);
      setDetalle(null);
      setCredenciales({ nombre: `${a.nombre} ${a.apellido}`, ...acceso });
    } catch (e) {
      const msg = e instanceof ApiError ? e.message : 'No se pudo crear el acceso.';
      // Celular ya usado (ej. hermano): pedimos uno alternativo y reintentamos
      if (!telefonoAlternativo && msg.includes('ya tiene una cuenta')) {
        const alt = window.prompt('Ese celular ya tiene una cuenta. Ingresá otro número para el acceso:')?.trim();
        if (alt) await accesoParaFicha(a, alt);
        return;
      }
      avisar(msg);
    }
  };

  const pausarOReactivar = async (a: Alumno) => {
    const pausar = a.estado === 'Activo';
    if (pausar && !(await confirmar({
      titulo: `Pausar a ${a.nombre} ${a.apellido}`,
      mensaje: 'Sale de sus turnos futuros y deja de pagarlos, pero le guardamos su lugar: al reactivarlo vuelve solo.',
      confirmar: 'Pausar',
    }))) return;

    await cambiarEstado(a.id, pausar ? 'Suspendido' : 'Activo');
    avisar(pausar
      ? `${a.nombre} pausado y fuera del calendario`
      : `${a.nombre} reactivado: vuelve a sus turnos`);
  };

  const baja = async (a: Alumno) => {
    if (!(await confirmar({
      titulo: `Dar de baja a ${a.nombre} ${a.apellido}`,
      mensaje: 'Sale del calendario y de todas sus clases (se libera el cupo en cada una). El historial se conserva.',
      confirmar: 'Dar de baja',
      peligro: true,
    }))) return;
    await darDeBaja(a.id);
    avisar(`${a.nombre} dado de baja y fuera del calendario`);
  };

  /** Borrado REAL: la ficha y TODO su historial, sin vuelta atrás. */
  const eliminar = async (a: Alumno) => {
    const enFam = enFamilia(a);
    if (!(await confirmar({
      titulo: `Eliminar a ${a.nombre} ${a.apellido}`,
      mensaje: (
        <>
          Se borra la ficha y <b>todo su historial</b> (cuotas, pagos, asistencia y
          horarios). <b>Esto no se puede deshacer.</b>
          {enFam
            ? ' Su cuenta familiar sigue activa: los demás miembros conservan el login.'
            : a.tieneUsuario ? ' También se elimina su acceso al portal.' : ''}
        </>
      ),
      confirmar: 'Eliminar definitivamente',
      cancelar: 'No, cancelar',
      peligro: true,
    }))) return;
    await eliminarDefinitivo(a.id);
    avisar(`${a.nombre} ${a.apellido} eliminado definitivamente`);
  };

  return (
    <div>
      <div className={s.toolbar}>
        {/* Buscador por nombre/apellido/DNI (client-side sobre lo ya cargado) */}
        <input
          className={s.buscador}
          type="search"
          value={busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
          placeholder="Buscar por nombre o DNI…"
        />
        {/* Categoría: select agrupado Varones/Damas (12 categorías no entran como chips) */}
        <select
          className={s.selectEstado}
          value={filtro}
          onChange={(e) => setFiltro(e.target.value as Categoria | 'todas')}
        >
          <option value="todas">Todas las categorías</option>
          <option value="SinCategoria">Sin categoría</option>
          <CategoriaOptions />
        </select>
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
        {/* Filtro por profe de cabecera (el club puede tener varios profes) */}
        {profes.length > 1 && (
          <select
            className={s.selectEstado}
            value={filtroProfe}
            onChange={(e) => setFiltroProfe(e.target.value)}
          >
            <option value="todos">Todos los profes</option>
            {profes.map((p) => (
              <option key={p.userId} value={p.userId}>{p.nombre}{p.esDueño ? ' (vos)' : ''}</option>
            ))}
          </select>
        )}

        <div className={s.spacer} />
        <div className={s.contador}>{visibles.length} alumnos</div>
        {/* El dueño Y el profe empleado pueden cargar alumnos (el staff queda auto-asignado). */}
        <button className={s.btnNuevo} onClick={() => setModalNuevo(true)}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.5" strokeLinecap="round">
            <path d="M12 5v14M5 12h14" />
          </svg>
          Nuevo alumno
        </button>
      </div>

      <div className={s.tarjeta}>
        {error && <div className={s.error}>{error}</div>}
        {cargando && !error && <div className={s.vacio}>Cargando…</div>}
        {!cargando && !error && (
          <table className={s.tabla}>
            <thead>
              <tr>
                <th>Alumno</th>
                <th>Categoría</th>
                <th>Cuota</th>
                <th>Estado</th>
                <th className={s.thAcciones}>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {visibles.map((a) => {
                const cat = CAT_COLOR[a.categoria];
                const estado = ESTADO_UI[a.estado];
                const sub = subPorEdad(a.fechaNacimiento);
                return (
                  <tr key={a.id}>
                    <td>
                      <div className={s.celdaAlumno}>
                        <Avatar nombre={a.nombre} apellido={a.apellido} fotoUrl={a.fotoUrl} size={40} radius={12} />
                        <div>
                          <div className={s.nombre}>{a.apellido}, {a.nombre}</div>
                          <div className={s.dni}>
                            {a.dni ? `DNI ${a.dni}` : 'Sin DNI'}{a.esMenor ? (a.tutorId ? ' · menor' : ' · menor (falta tutor)') : ''}
                            {enFamilia(a) ? ` · 👪 Familia (${conteoFamilia.get(a.familiaId!)})` : ''}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td>
                      <span className={s.chip} style={{ background: `${cat}1a`, color: cat }}>
                        {CAT_LABEL[a.categoria]}
                      </span>
                      {sub && (
                        <span className={s.chip} style={{ background: '#eef2ff', color: '#4f46e5', marginLeft: 6 }}>
                          {sub}
                        </span>
                      )}
                    </td>
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
                        {/* Editar y eliminar viven adentro de la ficha: la fila se
                            queda con lo que se usa a diario. */}
                        {esOwner && (
                        <>
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
                        <button className={`${s.accion} ${s.accionBaja}`} title="Dar de baja (se puede reactivar)" onClick={() => void baja(a)}>
                          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                            <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
                          </svg>
                        </button>
                        </>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
        {!cargando && !error && visibles.length === 0 && (
          <div className={s.vacio}>
            {alumnos.length === 0 && filtro === 'todas' && termino === '' && filtroProfe === 'todos'
              ? 'Todavía no hay alumnos. Creá el primero con "Nuevo alumno".'
              : 'No se encontraron alumnos con ese filtro o búsqueda.'}
          </div>
        )}
      </div>

      {modalNuevo && (
        <NuevoAlumnoModal
          onClose={() => setModalNuevo(false)}
          onCrear={crear}
          onCreado={(creado) => {
            setModalNuevo(false);
            if (creado.sumadoAFamilia) {
              // Mismo celular: se sumó a la cuenta del titular (cuenta familiar)
              avisar(`${creado.alumno.nombre} se sumó a la cuenta de ${creado.familiaTitular ?? 'la familia'}. Entra con ese mismo celular y aparece en su selector.`);
            } else if (creado.usuario && creado.passwordTemporal) {
              setCredenciales({
                nombre: `${creado.alumno.nombre} ${creado.alumno.apellido}`,
                usuario: creado.usuario,
                passwordTemporal: creado.passwordTemporal,
                vinculado: false,
                titular: null,
              });
            }
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
          hermanos={hermanosDe(detalle)}
          onClose={() => setDetalle(null)}
          onCrearAcceso={esOwner ? accesoParaFicha : undefined}
          onCambiarProfe={esOwner ? cambiarProfe : undefined}
          onEditar={esOwner ? (a) => { setDetalle(null); setEditando(a); } : undefined}
          onEliminar={esOwner ? (a) => { setDetalle(null); void eliminar(a); } : undefined}
        />
      )}
      {credenciales && (
        <AccesoCreadoModal
          nombre={credenciales.nombre}
          usuario={credenciales.usuario}
          passwordTemporal={credenciales.passwordTemporal}
          vinculado={credenciales.vinculado}
          titular={credenciales.titular}
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
