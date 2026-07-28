import { useQuery } from '@tanstack/react-query';
import Modal from '../../components/Modal';
import Avatar from '../../components/Avatar';
import NotasAlumnoSection from './NotasAlumnoSection';
import { api } from '../../lib/api';
import { CAT_COLOR, CAT_LABEL, ESTADO_UI, formatoPlata, subPorEdad } from './types';
import type { Alumno, AlumnoCuenta, AlumnoHorario } from './types';
import { useProfesores } from '../profesores/useProfesores';
import s from './DetalleAlumnoModal.module.css';

interface Props {
  alumno: Alumno;
  /** Otros miembros que comparten la cuenta (Capa 2: cuenta familiar). */
  hermanos: Alumno[];
  onClose: () => void;
  /** "Crear acceso" para fichas sin usuario (genera credenciales del portal). */
  onCrearAcceso?: (alumno: Alumno) => void;
}

/** Ficha del alumno con sus horarios asignados y su cuenta corriente reales. */
export default function DetalleAlumnoModal({ alumno, hermanos, onClose, onCrearAcceso }: Props) {
  const cat = CAT_COLOR[alumno.categoria];
  const estado = ESTADO_UI[alumno.estado];
  const { nombreDe } = useProfesores();

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
    ['Es menor', alumno.esMenor ? 'Sí (tutor registrado)' : 'No'],
    ['Profe de cabecera', nombreDe(alumno.profesorUserId) ?? 'Sin asignar'],
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
          <div className={s.seccion}>Horarios asignados</div>
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
                    <span className={s.itemTipo}>{h.grupo ?? h.tipo}</span>
                  </div>
                  <div className={s.itemSub}>
                    {h.cancha}{h.sede ? ` · ${h.sede}` : ''} · {h.duracionMinutos}′
                  </div>
                </div>
              ))}
            </div>
          )}

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
    </Modal>
  );
}
