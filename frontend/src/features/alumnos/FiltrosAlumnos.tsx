import type { ReactNode } from 'react';
import { useSedes } from '../agenda/hooks';
import { useProfesores } from '../profesores/useProfesores';
import CategoriaOptions from './CategoriaOptions';
import type { Categoria, Estado } from './types';
import s from './AlumnosPage.module.css';
import type { FiltrosAlumnos as Filtros } from './useFiltrosAlumnos';

interface Props {
  filtros: Filtros;
  /** Cuántas filas quedaron visibles y cómo se llaman ("12 alumnos"). */
  contador: ReactNode;
  /**
   * La pestaña Usuarios es la única que ofrece las Bajas: en Alumnos y en la espera
   * los de baja no están (pierden sus clases), así que el filtro no tendría qué traer.
   */
  conBajas?: boolean;
  /** La lista de espera no lo muestra: es siempre gente activa, sería un control muerto. */
  conEstado?: boolean;
  /** Lo que va a la derecha (el botón "Nuevo alumno"). */
  children?: ReactNode;
}

/**
 * La barra de filtros de las tres listas. Los selects van en un contenedor propio que
 * se desliza de costado: sueltos con flex-wrap se desparraman en el celular, que es
 * donde el profe la usa.
 */
export default function FiltrosAlumnos({
  filtros, contador, conBajas = false, conEstado = true, children,
}: Props) {
  const { sedes } = useSedes();
  const { profes } = useProfesores();

  return (
    <div className={s.toolbar}>
      <input
        className={s.buscador}
        type="search"
        value={filtros.busqueda}
        onChange={(e) => filtros.setBusqueda(e.target.value)}
        placeholder="Buscar por nombre o DNI…"
      />

      <div className={s.selects}>
        {/* Categoría: select agrupado Varones/Damas (12 categorías no entran como chips) */}
        <select
          className={s.selectEstado}
          value={filtros.categoria}
          onChange={(e) => filtros.setCategoria(e.target.value as Categoria | 'todas')}
        >
          <option value="todas">Todas las categorías</option>
          <option value="SinCategoria">Sin categoría</option>
          <CategoriaOptions />
        </select>

        {conEstado && (
          <select
            className={s.selectEstado}
            value={filtros.estado}
            onChange={(e) => filtros.setEstado(e.target.value as Estado | 'todos')}
          >
            <option value="todos">Todos los estados</option>
            <option value="Activo">Activos</option>
            <option value="Suspendido">Pausados</option>
            {conBajas && <option value="Inactivo">Bajas</option>}
          </select>
        )}

        {/* Club (sede): un profe que trabaja en dos clubes necesita mirar uno por vez */}
        {sedes.length > 1 && (
          <select
            className={s.selectEstado}
            value={filtros.club}
            onChange={(e) => filtros.setClub(e.target.value)}
          >
            <option value="todos">Todos los clubes</option>
            {sedes.map((sede) => (
              <option key={sede.id} value={sede.id}>{sede.nombre}</option>
            ))}
            <option value="sin">Sin club</option>
          </select>
        )}

        {/* Profe de cabecera (el club puede tener varios profes) */}
        {profes.length > 1 && (
          <select
            className={s.selectEstado}
            value={filtros.profe}
            onChange={(e) => filtros.setProfe(e.target.value)}
          >
            <option value="todos">Todos los profes</option>
            {profes.map((p) => (
              <option key={p.userId} value={p.userId}>{p.nombre}{p.esDueño ? ' (vos)' : ''}</option>
            ))}
          </select>
        )}
      </div>

      <div className={s.spacer} />
      <div className={s.contador}>{contador}</div>
      {children}
    </div>
  );
}
