import { useEffect, useState } from 'react';
import Modal from '../../components/Modal';
import { api, ApiError } from '../../lib/api';
import { CAT_COLOR, CAT_LABEL, avatarColor, iniciales } from '../alumnos/types';
import type { Alumno } from '../alumnos/types';
import type { Grupo } from './types';
import s from './AsignarAlumnoModal.module.css';
import btn from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  grupo: Grupo;
  onClose: () => void;
  onAsignar: (alumnoId: string) => Promise<unknown>;
}

/** Asignar un alumno ACTIVO al grupo (excluye a los que ya son miembros). */
export default function AsignarAlumnoModal({ grupo, onClose, onAsignar }: Props) {
  const [alumnos, setAlumnos] = useState<Alumno[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [asignando, setAsignando] = useState<string | null>(null);

  useEffect(() => {
    api.get<Alumno[]>('/alumnos?estado=Activo')
      .then((lista) => {
        const yaMiembros = new Set(grupo.miembros.map((m) => m.alumnoId));
        setAlumnos(lista.filter((a) => !yaMiembros.has(a.id)));
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando alumnos'))
      .finally(() => setCargando(false));
  }, [grupo]);

  const asignar = async (alumnoId: string) => {
    setError(null);
    setAsignando(alumnoId);
    try {
      await onAsignar(alumnoId);
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo asignar.');
      setAsignando(null);
    }
  };

  const lugares = grupo.cupoMaximo === null
    ? 'sin límite de cupo'
    : `${grupo.miembrosActivos}/${grupo.cupoMaximo} lugares ocupados`;

  return (
    <Modal titulo={`Asignar a "${grupo.nombre}"`} subtitulo={lugares} onClose={onClose}>
      {error && <div className={btn.error} style={{ marginBottom: 12 }}>{error}</div>}
      {cargando && <div className={s.vacio}>Cargando alumnos…</div>}
      {!cargando && alumnos.length === 0 && (
        <div className={s.vacio}>No hay alumnos activos disponibles para asignar.</div>
      )}
      <div className={s.lista}>
        {alumnos.map((a) => {
          const av = avatarColor(a.nombre + a.apellido);
          const cat = CAT_COLOR[a.categoria];
          return (
            <div key={a.id} className={s.fila}>
              <div className={s.avatar} style={{ background: `${av}1a`, color: av }}>
                {iniciales(a.nombre, a.apellido)}
              </div>
              <div className={s.datos}>
                <div className={s.nombre}>{a.nombre} {a.apellido}</div>
                <span className={s.chip} style={{ background: `${cat}1a`, color: cat }}>
                  {CAT_LABEL[a.categoria]}
                </span>
              </div>
              <button
                className={s.btnAsignar}
                disabled={asignando !== null}
                onClick={() => void asignar(a.id)}
              >
                {asignando === a.id ? 'Asignando…' : 'Asignar'}
              </button>
            </div>
          );
        })}
      </div>
    </Modal>
  );
}
