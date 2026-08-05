import { useEffect, useMemo, useState } from 'react';
import { api } from '../../lib/api';
import { CAT_COLOR, CAT_LABEL, avatarColor, iniciales } from '../alumnos/types';
import type { Alumno } from '../alumnos/types';
import s from './SelectorAlumnos.module.css';

interface Props {
  /** Los alumnos tildados (ids). El padre es el dueño del estado. */
  elegidos: string[];
  onCambiar: (ids: string[]) => void;
  /** Cupo de la clase; al llegar, los no tildados se deshabilitan. null = sin límite. */
  cupo: number | null;
  /** Los que ya vienen a la clase (al editar): no se ofrecen de nuevo. */
  excluir?: string[];
}

/**
 * Lista de alumnos con checkboxes y buscador, para armar el roster de una clase de
 * una sola pasada (antes había que crear un grupo aparte y cargarle la gente).
 *
 * Se ofrecen los ACTIVOS y los de la LISTA DE ESPERA: asignarles una clase es
 * justo lo que los convierte en alumnos. Los que tienen la cuota vencida se
 * muestran marcados pero no se pueden tildar — el back los rechaza igual.
 */
export default function SelectorAlumnos({ elegidos, onCambiar, cupo, excluir = [] }: Props) {
  const [alumnos, setAlumnos] = useState<Alumno[]>([]);
  const [cargando, setCargando] = useState(true);
  const [busqueda, setBusqueda] = useState('');

  useEffect(() => {
    void Promise.all([
      api.get<Alumno[]>('/alumnos?estado=Activo'),
      api.get<Alumno[]>('/alumnos?estado=EnEspera'),
    ])
      .then(([activos, espera]) => setAlumnos([...activos, ...espera]))
      .catch(() => setAlumnos([]))
      .finally(() => setCargando(false));
  }, []);

  const fuera = useMemo(() => new Set(excluir), [excluir]);
  const termino = busqueda.trim().toLowerCase();
  const visibles = useMemo(
    () => alumnos
      .filter((a) => !fuera.has(a.id))
      .filter((a) => termino === ''
        || `${a.nombre} ${a.apellido}`.toLowerCase().includes(termino)
        || `${a.apellido} ${a.nombre}`.toLowerCase().includes(termino))
      .sort((a, b) => a.apellido.localeCompare(b.apellido)),
    [alumnos, fuera, termino],
  );

  const lleno = cupo !== null && elegidos.length >= cupo;

  const alternar = (id: string) => {
    if (elegidos.includes(id)) onCambiar(elegidos.filter((x) => x !== id));
    else if (!lleno) onCambiar([...elegidos, id]);
  };

  return (
    <div className={s.caja}>
      <div className={s.barra}>
        <input
          className={s.buscador}
          type="search"
          value={busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
          placeholder="Buscar por nombre…"
        />
        <span className={lleno ? s.contadorLleno : s.contador}>
          {elegidos.length}{cupo !== null ? `/${cupo}` : ''} elegidos
        </span>
      </div>

      <div className={s.lista}>
        {cargando && <div className={s.vacio}>Cargando alumnos…</div>}
        {!cargando && visibles.length === 0 && (
          <div className={s.vacio}>
            {termino === '' ? 'No hay alumnos para sumar.' : 'Ninguno coincide con la búsqueda.'}
          </div>
        )}
        {visibles.map((a) => {
          const elegido = elegidos.includes(a.id);
          // Con la cuota vencida no entra a clases nuevas (regla del back).
          const bloqueado = a.deudaVencida || (!elegido && lleno);
          const av = avatarColor(a.nombre + a.apellido);
          const cat = CAT_COLOR[a.categoria];
          return (
            <label
              key={a.id}
              className={bloqueado ? s.filaBloqueada : elegido ? s.filaElegida : s.fila}
              title={a.deudaVencida ? 'Tiene la cuota vencida: registrá el pago antes de sumarlo' : undefined}
            >
              <input
                type="checkbox"
                checked={elegido}
                disabled={bloqueado}
                onChange={() => alternar(a.id)}
              />
              <span className={s.avatar} style={{ background: `${av}1a`, color: av }}>
                {iniciales(a.nombre, a.apellido)}
              </span>
              <span className={s.datos}>
                <span className={s.nombre}>{a.apellido}, {a.nombre}</span>
                <span className={s.chip} style={{ background: `${cat}1a`, color: cat }}>
                  {CAT_LABEL[a.categoria]}
                </span>
                {/* El tipo Estado del front lista solo los "reales"; en espera viene igual del back. */}
                {(a.estado as string) === 'EnEspera' && <span className={s.chipEspera}>en espera</span>}
                {a.deudaVencida && <span className={s.deuda}>cuota vencida</span>}
              </span>
            </label>
          );
        })}
      </div>

      {lleno && <p className={s.nota}>Llegaste al cupo: para sumar a otro, subí el cupo o destildá a alguien.</p>}
    </div>
  );
}
