import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAlumnos } from './useAlumnos';
import NuevoAlumnoModal from './NuevoAlumnoModal';
import EditarAlumnoModal from './EditarAlumnoModal';
import DetalleAlumnoModal from './DetalleAlumnoModal';
import AccesoCreadoModal from './AccesoCreadoModal';
import FiltrosAlumnos from './FiltrosAlumnos';
import TablaAlumnos from './TablaAlumnos';
import { useFiltrosAlumnos } from './useFiltrosAlumnos';
import { ApiError } from '../../lib/api';
import { obtenerSesion } from '../auth/sesion';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import type { Alumno, Estado, Lista } from './types';
import FranjaTenis from '../../components/tenis/FranjaTenis';
import s from './AlumnosPage.module.css';

interface Credenciales {
  nombre: string;
  usuario: string | null;
  passwordTemporal: string | null;
  vinculado: boolean;
  titular: string | null;
}

interface Props {
  /**
   * Qué lista se muestra. `ConClase` es la pestaña Alumnos (los que tienen clase);
   * `Todos` es Usuarios (el padrón entero, incluidos los que esperan y las bajas).
   */
  lista?: Lista;
}

export default function AlumnosPage({ lista = 'Todos' }: Props) {
  const esPadron = lista === 'Todos'; // la pestaña Usuarios
  const filtros = useFiltrosAlumnos();
  const { alumnos, cargando, error, crear, crearAcceso, editar, cambiarEstado, cambiarProfe, cambiarEspera, darDeBaja, eliminarDefinitivo } =
    useAlumnos(filtros.categoria, filtros.estado, lista);
  const [modalNuevo, setModalNuevo] = useState(false);
  const [editando, setEditando] = useState<Alumno | null>(null);
  const [detalle, setDetalle] = useState<Alumno | null>(null);
  const [credenciales, setCredenciales] = useState<Credenciales | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const confirmar = useConfirmar();
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

  const enFamilia = (a: Alumno) =>
    !!a.familiaId && alumnos.filter((o) => o.familiaId === a.familiaId).length > 1;
  const hermanosDe = (a: Alumno) =>
    a.familiaId ? alumnos.filter((o) => o.familiaId === a.familiaId && o.id !== a.id) : [];

  const visibles = filtros.aplicar(alumnos);

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

  /** Devuelve si se aplicó (false = el profe canceló la confirmación). */
  const pausarOReactivar = async (a: Alumno) => {
    const pausar = a.estado === 'Activo';
    if (pausar && !(await confirmar({
      titulo: `Pausar a ${a.nombre} ${a.apellido}`,
      mensaje: 'Sale de sus turnos futuros y deja de pagarlos, pero le guardamos su lugar: al reactivarlo vuelve solo.',
      confirmar: 'Pausar',
    }))) return false;

    await cambiarEstado(a.id, pausar ? 'Suspendido' : 'Activo');
    avisar(pausar
      ? `${a.nombre} pausado y fuera del calendario`
      : `${a.nombre} reactivado: vuelve a sus turnos`);
    return true;
  };

  /**
   * El select de estado rutea a DOS caminos distintos, y la diferencia importa:
   * pausar le GUARDA el lugar en sus clases y la baja se lo LIBERA. Mandar "Inactivo"
   * por el mismo endpoint que la pausa lo dejaría de baja ocupando cupo igual.
   *
   * Devuelve si se aplicó, para que el select vuelva a donde estaba si se cancela.
   */
  const cambiarEstadoDesdeLaFila = async (a: Alumno, nuevo: Estado) => {
    if (nuevo === a.estado) return true;
    if (nuevo === 'Inactivo') return baja(a);
    return pausarOReactivar(a);
  };

  /** Lo anotás en la espera porque te pidió otra clase hablando (no desde el portal). */
  const alternarEspera = async (a: Alumno) => {
    await cambiarEspera(a.id, !a.enEspera);
    avisar(a.enEspera
      ? `${a.nombre} salió de la lista de espera`
      : `${a.nombre} quedó anotado en la lista de espera`);
  };

  /** Devuelve si se aplicó (false = el profe canceló la confirmación). */
  const baja = async (a: Alumno) => {
    if (!(await confirmar({
      titulo: `Dar de baja a ${a.nombre} ${a.apellido}`,
      mensaje: 'Sale del calendario y de todas sus clases (se libera el cupo en cada una). El historial se conserva.',
      confirmar: 'Dar de baja',
      peligro: true,
    }))) return false;
    await darDeBaja(a.id);
    avisar(`${a.nombre} dado de baja y fuera del calendario`);
    return true;
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
      <FranjaTenis modo="rally" />
      {/* Estado: en Alumnos no se ofrecen las Bajas, que al perder sus clases dejan de
          estar en esta lista (viven en Usuarios). */}
      <FiltrosAlumnos
        filtros={filtros}
        conBajas={esPadron}
        contador={`${visibles.length} ${esPadron ? 'usuarios' : 'alumnos'}`}
      >
        {/* El dueño Y el profe empleado pueden cargar alumnos (el staff queda auto-asignado). */}
        <button className={s.btnNuevo} onClick={() => setModalNuevo(true)}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.5" strokeLinecap="round">
            <path d="M12 5v14M5 12h14" />
          </svg>
          Nuevo alumno
        </button>
      </FiltrosAlumnos>

      <div className={s.tarjeta}>
        {error && <div className={s.error}>{error}</div>}
        {cargando && !error && <div className={s.vacio}>Cargando…</div>}
        {!cargando && !error && (
          <TablaAlumnos
            alumnos={visibles}
            marcarSinClase={esPadron}
            acciones={(a) => (
              <>
                <button className={s.accion} title="Ver ficha" onClick={() => setDetalle(a)}>
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7-10-7-10-7z" /><circle cx="12" cy="12" r="3" />
                  </svg>
                </button>
                {/* Editar y eliminar viven adentro de la ficha: la fila se
                    queda con lo que se usa a diario. */}
                {esOwner && (
                  <>
                    {/* Anotar en la espera solo tiene sentido en el alumno ACTIVO
                        que YA tiene clase: al que no tiene ninguna ya lo muestra
                        la espera por sí solo, y el pausado no espera nada. */}
                    {a.estado === 'Activo' && a.tieneClase && (
                      <button
                        className={`${s.accion} ${a.enEspera ? s.accionEsperaActiva : ''}`}
                        title={a.enEspera
                          ? 'Está anotado en la lista de espera: sacarlo'
                          : 'Anotarlo en la lista de espera (te pidió otra clase)'}
                        onClick={() => void alternarEspera(a)}
                      >
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                          <circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" />
                        </svg>
                      </button>
                    )}
                    <select
                      className={s.selectFila}
                      value={a.estado}
                      onChange={(e) => {
                        // Si cancela la confirmación no cambia nada en la data, así
                        // que el select se queda mostrando lo que eligió: lo volvemos
                        // a la mano en vez de esperar que React lo redibuje solo.
                        const select = e.currentTarget;
                        const elegido = select.value as Estado;
                        void cambiarEstadoDesdeLaFila(a, elegido)
                          .then((aplicado) => { if (!aplicado) select.value = a.estado; });
                      }}
                      title="Estado del alumno"
                    >
                      <option value="Activo">Activo</option>
                      <option value="Suspendido">Pausado</option>
                      <option value="Inactivo">Baja</option>
                    </select>
                  </>
                )}
              </>
            )}
            vacio={alumnos.length === 0 && !filtros.hayFiltros
              ? esPadron
                ? 'Todavía no hay nadie en la academia. Cargá al primero con "Nuevo alumno".'
                : 'Todavía no hay alumnos con clase. Los que se anotaron y esperan están en "Lista de espera"; las bajas, en "Usuarios".'
              : 'No se encontraron resultados con ese filtro o búsqueda.'}
          />
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
        <div className={`${s.toast} motion-toast-center`}>
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#7bed9f" strokeWidth="2.5" strokeLinecap="round">
            <path d="M20 6L9 17l-5-5" />
          </svg>
          {toast}
        </div>
      )}
    </div>
  );
}
