import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';
import type {
  AccesoCreado, Alumno, AlumnoCreado, Categoria, CreateAlumno, Estado, UpdateAlumno,
} from './types';

/**
 * Estado y operaciones de la pantalla Alumnos contra la API .NET.
 * Con React Query: la lista se cachea por (categoría, estado); tras cada
 * mutación se invalida la key "alumnos" y se re-pide sola.
 */
export function useAlumnos(categoria: Categoria | 'todas', estado: Estado | 'todos' = 'todos') {
  const qc = useQueryClient();

  const query = useQuery({
    queryKey: ['alumnos', categoria, estado],
    queryFn: () => {
      // El backend filtra por ambos (GET /alumnos?categoria=&estado=)
      const params = new URLSearchParams();
      if (categoria !== 'todas') params.set('categoria', categoria);
      if (estado !== 'todos') params.set('estado', estado);
      const q = params.toString() === '' ? '' : `?${params}`;
      return api.get<Alumno[]>(`/alumnos${q}`);
    },
  });

  const invalidar = () => qc.invalidateQueries({ queryKey: ['alumnos'] });

  const crear = async (dto: CreateAlumno) => {
    // Devuelve la ficha + credenciales (la temporal viaja UNA sola vez)
    const creado = await api.post<AlumnoCreado>('/alumnos', dto);
    await invalidar();
    return creado;
  };

  const crearAcceso = async (id: string, email?: string) => {
    const acceso = await api.post<AccesoCreado>(`/alumnos/${id}/acceso`, { email });
    await invalidar();
    return acceso;
  };

  const editar = async (id: string, dto: UpdateAlumno) => {
    const actualizado = await api.put<Alumno>(`/alumnos/${id}`, dto);
    await invalidar();
    return actualizado;
  };

  const cambiarEstado = async (id: string, nuevoEstado: Estado) => {
    await api.patch<Alumno>(`/alumnos/${id}/estado`, { estado: nuevoEstado });
    await invalidar();
  };

  const darDeBaja = async (id: string) => {
    await api.delete<void>(`/alumnos/${id}`);
    await invalidar();
  };

  return {
    alumnos: query.data ?? [],
    cargando: query.isLoading,
    error: query.error ? (query.error.message || 'Error cargando alumnos') : null,
    crear, crearAcceso, editar, cambiarEstado, darDeBaja,
  };
}
