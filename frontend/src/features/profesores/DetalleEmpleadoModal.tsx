import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import Modal from '../../components/Modal';
import Avatar from '../../components/Avatar';
import { api } from '../../lib/api';
import { edad, formatoPlata } from '../alumnos/types';
import type { Alumno } from '../alumnos/types';
import type { Staff } from './types';
import s from '../alumnos/DetalleAlumnoModal.module.css';

interface Props {
  empleado: Staff;
  onClose: () => void;
}

/** Ficha del profe empleado: datos + su club + sus alumnos + acceso a sus horarios. */
export default function DetalleEmpleadoModal({ empleado, onClose }: Props) {
  // Sus alumnos = los que lo tienen de profe de cabecera (filtro client-side).
  const [alumnos, setAlumnos] = useState<Alumno[] | null>(null);
  useEffect(() => {
    void api.get<Alumno[]>('/alumnos')
      .then((xs) => setAlumnos(xs.filter((a) => a.profesorUserId === empleado.userId)))
      .catch(() => setAlumnos([]));
  }, [empleado.userId]);

  const nac = empleado.fechaNacimiento
    ? `${new Date(empleado.fechaNacimiento).toLocaleDateString('es-AR')} (${edad(empleado.fechaNacimiento)} años)`
    : '—';

  const datos: [string, string][] = [
    ['Club', empleado.sedeNombre ?? 'Sin asignar'],
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
            {empleado.sedeNombre && (
              <span className={s.chip} style={{ background: '#eef7f0', color: '#0e6b3c' }}>{empleado.sedeNombre}</span>
            )}
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

      <div className={s.seccion} style={{ marginTop: 18 }}>Sus alumnos</div>
      {alumnos === null ? (
        <div className={s.obs}>Cargando…</div>
      ) : alumnos.length === 0 ? (
        <div className={s.obs}>Todavía no tiene alumnos con él como profe de cabecera.</div>
      ) : (
        <div className={s.obs}>
          <b>{alumnos.length}</b> alumno{alumnos.length === 1 ? '' : 's'}:{' '}
          {alumnos.map((a) => `${a.nombre} ${a.apellido}`).join(', ')}.
        </div>
      )}

      <div className={s.seccion} style={{ marginTop: 18 }}>Sus horarios</div>
      <div className={s.obs}>
        Mirá su agenda en el{' '}
        <Link to="/calendario" onClick={onClose}>calendario</Link>{' '}
        (filtrá por {empleado.nombre} arriba).
      </div>
    </Modal>
  );
}
