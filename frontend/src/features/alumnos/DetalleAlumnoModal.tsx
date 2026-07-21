import Modal from '../../components/Modal';
import Avatar from '../../components/Avatar';
import NotasAlumnoSection from './NotasAlumnoSection';
import { CAT_COLOR, CAT_LABEL, ESTADO_UI, formatoPlata } from './types';
import type { Alumno } from './types';
import s from './DetalleAlumnoModal.module.css';

interface Props {
  alumno: Alumno;
  onClose: () => void;
  /** "Crear acceso" para fichas sin usuario (genera credenciales del portal). */
  onCrearAcceso?: (alumno: Alumno) => void;
}

/** Ficha del alumno. Horarios y pagos: placeholders hasta sus verticales. */
export default function DetalleAlumnoModal({ alumno, onClose, onCrearAcceso }: Props) {
  const cat = CAT_COLOR[alumno.categoria];
  const estado = ESTADO_UI[alumno.estado];

  const datos: [string, string][] = [
    ['DNI', alumno.dni],
    ['Teléfono', alumno.telefono],
    ['Email', alumno.email ?? '—'],
    ['Nacimiento', new Date(alumno.fechaNacimiento).toLocaleDateString('es-AR')],
    ['Es menor', alumno.esMenor ? 'Sí (tutor registrado)' : 'No'],
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
        </div>
        <div>
          <div className={s.seccion}>Horarios asignados</div>
          <div className={s.placeholder}>Llega con la vertical de Horarios.</div>
          <div className={s.seccion} style={{ marginTop: 18 }}>Pagos realizados</div>
          <div className={s.placeholder}>Llega con la vertical de Cuotas.</div>
        </div>
      </div>

      <div className={s.seguimiento}>
        <NotasAlumnoSection alumnoId={alumno.id} />
      </div>
    </Modal>
  );
}
