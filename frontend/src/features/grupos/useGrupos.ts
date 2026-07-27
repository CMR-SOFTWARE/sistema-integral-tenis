import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';
import type { CreateGrupo, Grupo, UpdateGrupo } from './types';

/** Estado y operaciones de la pantalla Grupos contra la API .NET. */
export function useGrupos() {
  const qc = useQueryClient();

  const query = useQuery({
    queryKey: ['grupos'],
    queryFn: () => api.get<Grupo[]>('/grupos'),
  });

  // Sumar/quitar a un grupo repone o saca al alumno de los turnos futuros e
  // impacta en quién tiene cuota (solo se cobra al que toma clases): invalidamos
  // también el calendario y las cuotas para que se vea el cambio.
  const invalidar = async () => {
    await qc.invalidateQueries({ queryKey: ['grupos'] });
    await qc.invalidateQueries({ queryKey: ['turnos-semana'] });
    await qc.invalidateQueries({ queryKey: ['cuotas'] });
  };

  const crear = async (dto: CreateGrupo) => {
    const creado = await api.post<Grupo>('/grupos', dto);
    await qc.invalidateQueries({ queryKey: ['grupos'] });
    return creado;
  };

  const editar = async (id: string, dto: UpdateGrupo) => {
    const actualizado = await api.put<Grupo>(`/grupos/${id}`, dto);
    await qc.invalidateQueries({ queryKey: ['grupos'] });
    return actualizado;
  };

  // Borrado real: se va el grupo, sus horarios y turnos → invalidar también el calendario.
  const eliminar = async (id: string) => {
    await api.delete<void>(`/grupos/${id}`);
    await invalidar();
  };

  const asignar = async (grupoId: string, alumnoId: string) => {
    await api.post<void>(`/grupos/${grupoId}/alumnos`, { alumnoId });
    await invalidar();
  };

  const quitar = async (grupoId: string, alumnoId: string) => {
    await api.delete<void>(`/grupos/${grupoId}/alumnos/${alumnoId}`);
    await invalidar();
  };

  return {
    grupos: query.data ?? [],
    cargando: query.isLoading,
    error: query.error ? (query.error.message || 'Error cargando grupos') : null,
    crear, editar, eliminar, asignar, quitar,
    recargar: () => qc.invalidateQueries({ queryKey: ['grupos'] }),
  };
}
