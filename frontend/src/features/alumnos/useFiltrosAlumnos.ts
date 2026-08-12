import { useMemo, useState } from 'react';
import type { Alumno, Categoria, Estado } from './types';

/**
 * El estado de la barra de filtros, compartido por Alumnos, Usuarios y la Lista de
 * espera: las tres son recortes de la misma gente, así que se filtran igual.
 *
 * Todo se aplica en el CLIENTE sobre lo ya cargado. Categoría y estado además viajan
 * al back en la pestaña Alumnos (son parte de su query key); filtrar de nuevo acá es
 * idempotente y le da los mismos filtros a la espera, que trae siempre los activos.
 */
export function useFiltrosAlumnos() {
  const [busqueda, setBusqueda] = useState('');
  const [categoria, setCategoria] = useState<Categoria | 'todas'>('todas');
  const [estado, setEstado] = useState<Estado | 'todos'>('todos');
  const [profe, setProfe] = useState('todos');
  const [club, setClub] = useState('todos');

  /** ¿Hay algo puesto? Sirve para distinguir "no hay nadie" de "no hay resultados". */
  const hayFiltros =
    busqueda.trim() !== '' || categoria !== 'todas' || estado !== 'todos'
    || profe !== 'todos' || club !== 'todos';

  const aplicar = useMemo(() => {
    const termino = busqueda.trim().toLowerCase();

    return <T extends Alumno>(lista: T[]): T[] => lista
      .filter((a) => {
        const coincideTexto = termino === ''
          || `${a.nombre} ${a.apellido}`.toLowerCase().includes(termino)
          || (a.dni ?? '').toLowerCase().includes(termino);
        const coincideCategoria = categoria === 'todas' || a.categoria === categoria;
        const coincideEstado = estado === 'todos' || a.estado === estado;
        const coincideProfe = profe === 'todos' || a.profesorUserId === profe;
        // "sin" = las fichas que todavía no tienen club: son las que el profe busca
        // para completarlas, así que merecen su propia opción y no caer en "todos".
        const coincideClub = club === 'todos'
          || (club === 'sin' ? a.sedeId === null : a.sedeId === club);
        return coincideTexto && coincideCategoria && coincideEstado && coincideProfe && coincideClub;
      })
      // Orden alfabético por apellido y después nombre (es-AR, sin distinguir acentos/mayús).
      .sort((a, b) =>
        a.apellido.localeCompare(b.apellido, 'es', { sensitivity: 'base' })
        || a.nombre.localeCompare(b.nombre, 'es', { sensitivity: 'base' }));
  }, [busqueda, categoria, estado, profe, club]);

  return {
    busqueda, setBusqueda,
    categoria, setCategoria,
    estado, setEstado,
    profe, setProfe,
    club, setClub,
    hayFiltros,
    aplicar,
  };
}

export type FiltrosAlumnos = ReturnType<typeof useFiltrosAlumnos>;
