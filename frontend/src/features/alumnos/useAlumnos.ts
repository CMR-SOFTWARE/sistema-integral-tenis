import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';
import type {
  AccesoCreado, Alumno, AlumnoCreado, Categoria, CreateAlumno, Estado, Lista, UpdateAlumno,
} from './types';

/**
 * Estado y operaciones de la pantalla Alumnos contra la API .NET.
 * Con React Query: la lista se cachea por (categoría, estado, lista); tras cada
 * mutación se invalida la key "alumnos" y se re-pide sola. La `lista` es lo que
 * separa la pestaña Alumnos (ConClase) de Usuarios (Todos).
 */
export function useAlumnos(
  categoria: Categoria | 'todas',
  estado: Estado | 'todos' = 'todos',
  lista: Lista = 'Todos',
) {
  const qc = useQueryClient();

  const query = useQuery({
    queryKey: ['alumnos', categoria, estado, lista],
    queryFn: () => {
      // El backend filtra por los tres (GET /alumnos?categoria=&estado=&lista=)
      const params = new URLSearchParams();
      if (categoria !== 'todas') params.set('categoria', categoria);
      if (estado !== 'todos') params.set('estado', estado);
      if (lista !== 'Todos') params.set('lista', lista);
      const q = params.toString() === '' ? '' : `?${params}`;
      return api.get<Alumno[]>(`/alumnos${q}`);
    },
  });

  // El cambio de un alumno (ej. su cuota mensual / arancel) también repercute en Cuotas.
  const invalidar = () => Promise.all([
    qc.invalidateQueries({ queryKey: ['alumnos'] }),
    qc.invalidateQueries({ queryKey: ['cuotas'] }),
  ]);

  const crear = async (dto: CreateAlumno) => {
    // Devuelve la ficha + credenciales (la temporal viaja UNA sola vez)
    const creado = await api.post<AlumnoCreado>('/alumnos', dto);
    await invalidar();
    return creado;
  };

  const crearAcceso = async (id: string, telefono?: string) => {
    const acceso = await api.post<AccesoCreado>(`/alumnos/${id}/acceso`, { telefono });
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

  /** Asigna/cambia el profe titular desde la ficha (null = desasignar). Devuelve la ficha actualizada. */
  const cambiarProfe = async (id: string, profesorUserId: string | null) => {
    const actualizado = await api.patch<Alumno>(`/alumnos/${id}/profesor`, { profesorUserId });
    await invalidar();
    return actualizado;
  };

  const darDeBaja = async (id: string) => {
    await api.delete<void>(`/alumnos/${id}`);
    await invalidar();
  };

  /** Borrado REAL: la ficha y todo su historial, sin vuelta atrás. */
  const eliminarDefinitivo = async (id: string) => {
    await api.delete<void>(`/alumnos/${id}/definitivo`);
    await invalidar();
  };

  return {
    alumnos: query.data ?? [],
    cargando: query.isLoading,
    error: query.error ? (query.error.message || 'Error cargando alumnos') : null,
    crear, crearAcceso, editar, cambiarEstado, cambiarProfe, darDeBaja, eliminarDefinitivo,
  };
}
