import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';
import type { Bloqueo, CreateBloqueo, Impacto } from './types';

export function useBloqueos() {
  const qc = useQueryClient();
  const query = useQuery({
    queryKey: ['bloqueos'],
    queryFn: () => api.get<Bloqueo[]>('/bloqueos'),
  });

  // Un bloqueo cancela/repone turnos futuros: invalidamos también el calendario.
  const invalidar = async () => {
    await qc.invalidateQueries({ queryKey: ['bloqueos'] });
    await qc.invalidateQueries({ queryKey: ['turnos-semana'] });
  };

  /** Preview del impacto: NO persiste nada (alimenta el modal Impacto). */
  const previsualizar = (dto: CreateBloqueo) =>
    api.post<Impacto>('/bloqueos/impacto', dto);

  const crear = async (dto: CreateBloqueo) => {
    await api.post('/bloqueos', dto);
    await invalidar();
  };

  const eliminar = async (id: string) => {
    await api.delete(`/bloqueos/${id}`);
    await invalidar();
  };

  return {
    bloqueos: query.data ?? [],
    cargando: query.isLoading,
    error: query.error ? (query.error.message || 'Error cargando bloqueos') : null,
    previsualizar, crear, eliminar,
  };
}
