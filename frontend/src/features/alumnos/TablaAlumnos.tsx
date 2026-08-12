import type { ReactNode } from 'react';
import Avatar from '../../components/Avatar';
import { useProfesores } from '../profesores/useProfesores';
import { CAT_COLOR, CAT_LABEL, ESTADO_UI, subPorEdad } from './types';
import type { Alumno } from './types';
import s from './AlumnosPage.module.css';

interface Props<T extends Alumno> {
  alumnos: T[];
  /**
   * Una columna propia de la pantalla, entre Categoría y Cuota. La usa la lista de
   * espera para mostrar por qué está y desde cuándo.
   */
  columna?: { titulo: string; render: (a: T) => ReactNode };
  /** Los botones de la fila: cada lista tiene los suyos. */
  acciones: (a: T) => ReactNode;
  /** Qué decir cuando no hay filas (cambia si es "no hay nadie" o "no hay resultados"). */
  vacio: ReactNode;
  /**
   * Marcar "En espera" al que no tiene ninguna clase. Solo en Usuarios: en Alumnos
   * todos tienen por definición, y en la espera lo dice la columna del motivo.
   */
  marcarSinClase?: boolean;
}

/**
 * La tabla de personas del profe. La comparten las tres listas —Alumnos, Usuarios y
 * Lista de espera— porque son recortes de la misma gente: si mañana la fila gana un
 * dato, aparece en las tres sin que nadie se olvide de una.
 *
 * Lo que cambia por pantalla son las ACCIONES (y una columna opcional), que es lo
 * único que de verdad se diferencia: sacar de la espera no es lo mismo que dar de baja.
 */
export default function TablaAlumnos<T extends Alumno>({
  alumnos, columna, acciones, vacio, marcarSinClase = false,
}: Props<T>) {
  const { nombreDe } = useProfesores();

  // Cuenta familiar: fichas que comparten familiaId (mismo login) son una familia. Se
  // calcula sobre lo que trajo la lista, así que en Alumnos un hermano que todavía no
  // tiene clase no cuenta y el chip no aparece (en Usuarios sí). Se prefiere eso a
  // pedir el padrón entero en cada carga.
  const conteoFamilia = new Map<string, number>();
  for (const a of alumnos) if (a.familiaId) conteoFamilia.set(a.familiaId, (conteoFamilia.get(a.familiaId) ?? 0) + 1);

  return (
    <>
      <table className={s.tabla}>
        <thead>
          <tr>
            <th>Alumno</th>
            <th>Categoría</th>
            {columna && <th>{columna.titulo}</th>}
            <th>Cuota</th>
            <th>Estado</th>
            <th className={s.thAcciones}>Acciones</th>
          </tr>
        </thead>
        <tbody>
          {alumnos.map((a) => {
            const cat = CAT_COLOR[a.categoria];
            const estado = ESTADO_UI[a.estado];
            const sub = subPorEdad(a.fechaNacimiento);
            const enFamilia = !!a.familiaId && (conteoFamilia.get(a.familiaId) ?? 0) > 1;
            return (
              <tr key={a.id}>
                <td className={s.tdAlumno}>
                  <div className={s.celdaAlumno}>
                    <Avatar nombre={a.nombre} apellido={a.apellido} fotoUrl={a.fotoUrl} size={40} radius={12} />
                    <div>
                      <div className={s.nombre}>{a.apellido}, {a.nombre}</div>
                      <div className={s.dni}>
                        {a.dni ? `DNI ${a.dni}` : 'Sin DNI'}{a.esMenor ? (a.tutorId ? ' · con tutor' : ' · falta el tutor') : ''}
                        {enFamilia ? ` · 👪 Familia (${conteoFamilia.get(a.familiaId!)})` : ''}
                      </div>
                      {/* Dónde entrena y con quién: lo que el profe mira para ubicar a
                          alguien. Va acá y no como dos columnas porque en 390 px no
                          entran siete, y porque reemplaza al teléfono que estaba antes. */}
                      <div className={s.clubProfe}>
                        {a.sedeNombre ?? 'Sin club'} · {nombreDe(a.profesorUserId) ?? 'Sin profe'}
                      </div>
                    </div>
                  </div>
                </td>
                <td className={s.tdCategoria}>
                  <span className={s.chip} style={{ background: `${cat}1a`, color: cat }}>
                    {CAT_LABEL[a.categoria]}
                  </span>
                  {sub && (
                    <span className={s.chip} style={{ background: '#eef2ff', color: '#4f46e5', marginLeft: 6 }}>
                      {sub}
                    </span>
                  )}
                </td>
                {columna && <td className={s.tdExtra}>{columna.render(a)}</td>}
                <td className={s.tdCuota}>
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
                <td className={s.tdEstado}>
                  <span className={s.chip} style={{ background: estado.bg, color: estado.fg }}>
                    {estado.label}
                  </span>
                  {/* El anotado a mano se marca en todas las listas: es la única forma de
                      ver desde acá que además está esperando otra clase. */}
                  {a.estado === 'Activo' && (a.enEspera || (marcarSinClase && !a.tieneClase)) && (
                    <span className={s.chipEspera}>En espera</span>
                  )}
                </td>
                <td className={s.tdAcciones}>
                  <div className={s.acciones}>{acciones(a)}</div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
      {alumnos.length === 0 && <div className={s.vacio}>{vacio}</div>}
    </>
  );
}
