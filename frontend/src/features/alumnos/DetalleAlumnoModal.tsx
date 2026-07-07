import Modal from '../../components/Modal';
import { CAT_COLOR, CAT_LABEL, ESTADO_UI, avatarColor, formatoPlata, iniciales } from './types';
import type { Alumno } from './types';
import s from './DetalleAlumnoModal.module.css';

/** Ficha del alumno. Horarios y pagos: placeholders hasta sus verticales. */
export default function DetalleAlumnoModal({ alumno, onClose }: { alumno: Alumno; onClose: () => void }) {
  const av = avatarColor(alumno.nombre + alumno.apellido);
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
        <div className={s.avatar} style={{ background: `${av}1a`, color: av }}>
          {iniciales(alumno.nombre, alumno.apellido)}
        </div>
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
        </div>
        <div>
          <div className={s.seccion}>Horarios asignados</div>
          <div className={s.placeholder}>Llega con la vertical de Horarios.</div>
          <div className={s.seccion} style={{ marginTop: 18 }}>Pagos realizados</div>
          <div className={s.placeholder}>Llega con la vertical de Cuotas.</div>
        </div>
      </div>
    </Modal>
  );
}
