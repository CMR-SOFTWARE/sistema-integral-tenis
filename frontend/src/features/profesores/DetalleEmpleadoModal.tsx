import Modal from '../../components/Modal';
import Avatar from '../../components/Avatar';
import { edad, formatoPlata } from '../alumnos/types';
import type { Staff } from './types';
import s from '../alumnos/DetalleAlumnoModal.module.css';

interface Props {
  empleado: Staff;
  onClose: () => void;
}

/** Ficha del profe empleado: datos personales + valor hora + estado. */
export default function DetalleEmpleadoModal({ empleado, onClose }: Props) {
  const nac = empleado.fechaNacimiento
    ? `${new Date(empleado.fechaNacimiento).toLocaleDateString('es-AR')} (${edad(empleado.fechaNacimiento)} años)`
    : '—';

  const datos: [string, string][] = [
    ['Celular (login)', empleado.telefono || '—'],
    ['Email', empleado.email || '—'],
    ['DNI', empleado.dni ?? '—'],
    ['Nacimiento', nac],
    ['Valor hora', empleado.valorHora != null ? `${formatoPlata(empleado.valorHora)} / hora` : 'Sin definir'],
    ['Alta en el club', new Date(empleado.creadoEl).toLocaleDateString('es-AR')],
  ];

  return (
    <Modal titulo="" onClose={onClose} ancho={520}>
      <div className={s.cabecera}>
        <Avatar nombre={empleado.nombre} apellido={empleado.apellido} size={56} radius={16} />
        <div>
          <div className={s.nombre}>{empleado.nombre} {empleado.apellido}</div>
          <div className={s.chips}>
            <span className={s.chip} style={{ background: '#e8f0fe', color: '#1a56db' }}>Profesor</span>
            <span
              className={s.chip}
              style={empleado.activo
                ? { background: '#e7f6ec', color: '#0e6b3c' }
                : { background: '#f3f4f6', color: '#6b7280' }}
            >
              {empleado.activo ? 'Activo' : 'Inactivo'}
            </span>
          </div>
        </div>
      </div>

      <div className={s.seccion}>Datos personales</div>
      {datos.map(([k, v]) => (
        <div key={k} className={s.fila}>
          <span className={s.filaK}>{k}</span>
          <span className={s.filaV}>{v}</span>
        </div>
      ))}

      <div className={s.seccion} style={{ marginTop: 18 }}>Acceso a la app</div>
      <div className={s.obs}>
        Entra con su celular como usuario. Ve su agenda y sus alumnos (vista reducida del panel). ✅
      </div>
    </Modal>
  );
}
