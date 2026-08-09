import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import Modal from '../../components/Modal';
import Avatar from '../../components/Avatar';
import NotasAlumnoSection from './NotasAlumnoSection';
import RaquetasAlumnoSection from './RaquetasAlumnoSection';
import { api, ApiError } from '../../lib/api';
import { CAT_COLOR, CAT_LABEL, ESTADO_UI, formatoPlata, subPorEdad } from './types';
import type { Alumno, AlumnoCuenta, AlumnoHorario } from './types';
import { useProfesores } from '../profesores/useProfesores';
import { useSedes } from '../agenda/hooks';
import NuevoHorarioModal from '../agenda/NuevoHorarioModal';
import { obtenerSesion } from '../auth/sesion';
import s from './DetalleAlumnoModal.module.css';

interface Props {
  alumno: Alumno;
  /** Otros miembros que comparten la cuenta (Capa 2: cuenta familiar). */
  hermanos: Alumno[];
  onClose: () => void;
  /** "Crear acceso" para fichas sin usuario (genera credenciales del portal). */
  onCrearAcceso?: (alumno: Alumno) => void;
  /** Cambia el profe titular desde la ficha; devuelve la ficha actualizada. */
  onCambiarProfe?: (id: string, profesorUserId: string | null) => Promise<Alumno>;
  /** Abre la edición de datos (el listado ya no tiene su propio botón). */
  onEditar?: (alumno: Alumno) => void;
  /** Borrado REAL. Vive acá adentro y no en la fila: es el que no se deshace. */
  onEliminar?: (alumno: Alumno) => void;
}

/** Ficha del alumno: datos, roles (Director + Profe titular), horarios y cuenta. */
export default function DetalleAlumnoModal({
  alumno, hermanos, onClose, onCrearAcceso, onCambiarProfe, onEditar, onEliminar,
}: Props) {
  const cat = CAT_COLOR[alumno.categoria];
  const estado = ESTADO_UI[alumno.estado];
  // Solo los profes que dan clases en el club del alumno (el director, siempre).
  const { profes } = useProfesores(alumno.sedeId);
  const qc = useQueryClient();

  // "Asignar horario" desde la ficha: reusa el modal de horario con el alumno fijo.
  const [asignando, setAsignando] = useState(false);
  const sesion = obtenerSesion();
  const miSedeId = sesion?.rol === 'staff' ? sesion.sedeId : null; // el staff solo su club
  const { sedes } = useSedes();
  const disponibles = sedes.filter((x) => x.activo);
  const sedesParaHorario = miSedeId ? disponibles.filter((x) => x.id === miSedeId) : disponibles;

  // El Director es el dueño de la academia (fijo); el Profe titular se asigna acá.
  const director = profes.find((p) => p.esDueño);
  const [profeId, setProfeId] = useState(alumno.profesorUserId ?? '');
  const [guardandoProfe, setGuardandoProfe] = useState(false);
  const [errorProfe, setErrorProfe] = useState<string | null>(null);

  // El club NO se mueve al cambiar de profe: es donde entrena el alumno. Si el profe
  // elegido no da clases ahí, el back lo rechaza y se muestra el motivo.
  const cambiarProfe = async (nuevo: string) => {
    if (!onCambiarProfe) return;
    setGuardandoProfe(true);
    setErrorProfe(null);
    try {
      const act = await onCambiarProfe(alumno.id, nuevo || null);
      setProfeId(act.profesorUserId ?? '');
    } catch (e) {
      setErrorProfe(e instanceof ApiError ? e.message : 'No se pudo cambiar el profe.');
    } finally {
      setGuardandoProfe(false);
    }
  };

  const horarios = useQuery({
    queryKey: ['alumno-horarios', alumno.id],
    queryFn: () => api.get<AlumnoHorario[]>(`/alumnos/${alumno.id}/horarios`),
  });
  const cuenta = useQuery({
    queryKey: ['alumno-cuenta', alumno.id],
    queryFn: () => api.get<AlumnoCuenta>(`/alumnos/${alumno.id}/cuenta`),
  });

  const datos: [string, string][] = [
    ['DNI', alumno.dni ?? '—'],
    ['Teléfono', alumno.telefono],
    ['Email', alumno.email ?? '—'],
    ['Nacimiento', alumno.fechaNacimiento
      ? new Date(alumno.fechaNacimiento).toLocaleDateString('es-AR')
      : '—'],
    ['Categoría por edad', subPorEdad(alumno.fechaNacimiento) ?? 'Adulto'],
    ['Responsable', alumno.esMenor ? (alumno.tutorId ? 'Sí (con tutor)' : 'Sí — falta cargarlo') : 'No necesita'],
    ['Director', director ? director.nombre : '—'],
    ['Club', alumno.sedeNombre ?? 'Sin asignar'],
    ['Cuota mensual', formatoPlata(alumno.arancel)],
    ['Alta en el sistema', new Date(alumno.creadoEl).toLocaleDateString('es-AR')],
  ];

  return (
    <Modal titulo="" onClose={onClose} ancho={620}>
      <div className={s.cabecera}>
        <Avatar nombre={alumno.nombre} apellido={alumno.apellido} fotoUrl={alumno.fotoUrl} size={56} radius={16} />
        <div>
          <div className={s.nombre}>{alumno.nombre} {alumno.apellido}</div>
          <div className={s.chips}>
            <span className={s.chip} style={{ background: `${cat}1a`, color: cat }}>
              Categoría {CAT_LABEL[alumno.categoria]}
            </span>
            <span className={s.chip} style={{ background: estado.bg, color: estado.fg }}>
              {estado.label}
            </span>
          </div>
        </div>
        {/* La edición se hace desde acá: el listado dejó de tener su propio lápiz */}
        {onEditar && (
          <button className={s.btnEditar} onClick={() => onEditar(alumno)}>
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M11 4H4v16h16v-7M18.5 2.5a2.12 2.12 0 0 1 3 3L12 15l-4 1 1-4z" />
            </svg>
            Editar datos
          </button>
        )}
      </div>

      <div className={s.columnas}>
        <div>
          <div className={s.seccion}>Datos personales</div>
          {datos.map(([k, v]) => (
            <div key={k} className={s.fila}>
              <span className={s.filaK}>{k}</span>
              <span className={s.filaV}>{v}</span>
            </div>
          ))}
          {/* Profe titular: se cambia desde acá, entre los que dan clases en su club.
              Para mudarlo de club hay que ir a "Editar datos". */}
          <div className={s.fila}>
            <span className={s.filaK}>Profe titular</span>
            <span className={s.filaV}>
              {onCambiarProfe ? (
                <select
                  value={profeId}
                  disabled={guardandoProfe}
                  onChange={(e) => void cambiarProfe(e.target.value)}
                  style={{ padding: '4px 8px', borderRadius: 8, border: '1px solid #dde5da', fontWeight: 600, maxWidth: 190 }}
                >
                  <option value="">Sin asignar</option>
                  {profes.map((p) => <option key={p.userId} value={p.userId}>{p.nombre}</option>)}
                </select>
              ) : (
                profes.find((p) => p.userId === profeId)?.nombre ?? 'Sin asignar'
              )}
            </span>
          </div>
          {errorProfe && <div className={s.errorProfe}>{errorProfe}</div>}

          <div className={s.seccion} style={{ marginTop: 18 }}>Observaciones del profesor</div>
          <div className={s.obs}>{alumno.notas ?? 'Sin observaciones.'}</div>

          <div className={s.seccion} style={{ marginTop: 18 }}>Acceso al portal</div>
          {alumno.tieneUsuario ? (
            <div className={s.obs}>Tiene su cuenta activa. ✅</div>
          ) : (
            <>
              <div className={s.obs}>Todavía no tiene cuenta para entrar al portal.</div>
              {onCrearAcceso && (
                <button className={s.btnAcceso} onClick={() => onCrearAcceso(alumno)}>
                  Crear acceso al portal
                </button>
              )}
            </>
          )}

          {hermanos.length > 0 && (
            <>
              <div className={s.seccion} style={{ marginTop: 18 }}>👪 Cuenta familiar</div>
              <div className={s.obs}>
                Comparte el mismo login con: {hermanos.map((h) => `${h.nombre} ${h.apellido}`).join(', ')}.
              </div>
            </>
          )}
        </div>
        <div>
          <div className={s.seccion} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span>Horarios asignados</span>
            <button
              onClick={() => setAsignando(true)}
              style={{
                marginLeft: 'auto', padding: '4px 10px', borderRadius: 8,
                border: '1px solid var(--color-primary)', background: 'transparent',
                color: 'var(--color-primary-dark)', fontWeight: 700, fontSize: 12, cursor: 'pointer',
              }}
            >
              + Asignar horario
            </button>
          </div>
          {horarios.isLoading ? (
            <div className={s.placeholder}>Cargando…</div>
          ) : (horarios.data?.length ?? 0) === 0 ? (
            <div className={s.placeholder}>Sin horarios asignados todavía.</div>
          ) : (
            <div className={s.lista}>
              {horarios.data!.map((h, i) => (
                <div key={i} className={s.itemHorario}>
                  <div className={s.itemPrincipal}>
                    <span className={s.itemDia}>{h.dia} {h.horaInicio}</span>
                    <span className={s.itemTipo}>{h.titulo}</span>
                  </div>
                  <div className={s.itemSub}>
                    {h.cancha}{h.sede ? ` · ${h.sede}` : ''} · {h.duracionMinutos}′
                    {/* Con cuántos comparte la clase: 0 = la tiene para él solo. */}
                    {h.companeros > 0
                      ? ` · con ${h.companeros} más`
                      : ' · particular'}
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Las raquetas: el profe mira hace cuánto que están encordadas. */}
          <div style={{ marginTop: 18 }}>
            <RaquetasAlumnoSection alumnoId={alumno.id} nombre={alumno.nombre} />
          </div>

          <div className={s.seccion} style={{ marginTop: 18 }}>
            Cuenta corriente
            {cuenta.data && cuenta.data.totalAdeudado > 0 && (
              <span
                className={s.chip}
                style={{
                  marginLeft: 8,
                  background: cuenta.data.deudaVencida ? '#fdeaea' : '#fef6e7',
                  color: cuenta.data.deudaVencida ? '#b91c1c' : '#b7791f',
                }}
              >
                debe {formatoPlata(cuenta.data.totalAdeudado)}
              </span>
            )}
          </div>
          {cuenta.isLoading ? (
            <div className={s.placeholder}>Cargando…</div>
          ) : (cuenta.data?.cargos.length ?? 0) === 0 ? (
            <div className={s.placeholder}>Sin movimientos.</div>
          ) : (
            <div className={s.lista}>
              {cuenta.data!.cargos.map((c) => (
                <div key={c.id} className={s.itemCargo}>
                  <span className={s.cargoFecha}>
                    {new Date(`${c.fecha}T00:00:00`).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' })}
                  </span>
                  <span className={s.cargoConcepto}>{c.concepto}</span>
                  <span className={s.cargoMonto}>{formatoPlata(c.monto)}</span>
                  {c.pagado ? (
                    <span className={s.cargoPagado}>✓ {c.medioPago}</span>
                  ) : c.pagoInformado ? (
                    <span className={s.cargoInformado}>informó</span>
                  ) : (
                    <span className={s.cargoImpago}>impago</span>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      <div className={s.seguimiento}>
        <NotasAlumnoSection alumnoId={alumno.id} />
      </div>

      {/* Zona de peligro: bien al fondo y separada de todo lo demás, porque esto
          borra la ficha con su historial y no se puede deshacer (CLAUDE.md §6). */}
      {onEliminar && (
        <div className={s.zonaPeligro}>
          <div>
            <div className={s.zonaPeligroTitulo}>Eliminar definitivamente</div>
            <div className={s.zonaPeligroTexto}>
              Borra la ficha y todo su historial. Si solo dejó de venir, usá "dar de
              baja" en el listado: eso se puede revertir.
            </div>
          </div>
          <button className={s.btnEliminar} onClick={() => onEliminar(alumno)}>
            Eliminar
          </button>
        </div>
      )}

      {asignando && (
        <NuevoHorarioModal
          sedes={sedesParaHorario}
          alumnoFijo={{
            id: alumno.id,
            nombre: alumno.nombre,
            apellido: alumno.apellido,
            profesorUserId: alumno.profesorUserId,
          }}
          onClose={() => setAsignando(false)}
          onCrear={async (dto) => {
            await api.post('/horarios', dto);
            // Refrescamos los horarios de la ficha + la lista (puede pasar de espera a alumno).
            await Promise.all([
              qc.invalidateQueries({ queryKey: ['alumno-horarios', alumno.id] }),
              qc.invalidateQueries({ queryKey: ['alumnos'] }),
              qc.invalidateQueries({ queryKey: ['dashboard'] }),
            ]);
          }}
        />
      )}
    </Modal>
  );
}
