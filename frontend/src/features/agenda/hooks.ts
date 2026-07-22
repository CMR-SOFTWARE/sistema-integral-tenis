import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';
import type { CreateHorario, Horario, Sede, Turno } from './types';

export function useSedes() {
  const qc = useQueryClient();
  const query = useQuery({
    queryKey: ['sedes'],
    queryFn: () => api.get<Sede[]>('/sedes'),
  });
  const invalidar = () => qc.invalidateQueries({ queryKey: ['sedes'] });

  const crearSede = async (nombre: string) => {
    await api.post<Sede>('/sedes', { nombre });
    await invalidar();
  };

  const agregarCancha = async (sedeId: string, nombre: string) => {
    await api.post(`/sedes/${sedeId}/canchas`, { nombre });
    await invalidar();
  };

  /** Baja lógica: deja de ofrecerse para horarios nuevos (la historia queda). */
  const desactivarSede = async (sedeId: string) => {
    await api.delete(`/sedes/${sedeId}`);
    await invalidar();
  };

  const reactivarSede = async (sedeId: string) => {
    await api.post(`/sedes/${sedeId}/reactivar`, {});
    await invalidar();
  };

  return {
    sedes: query.data ?? [],
    cargando: query.isLoading,
    crearSede, agregarCancha, desactivarSede, reactivarSede,
  };
}

export function useHorarios() {
  const qc = useQueryClient();
  const query = useQuery({
    queryKey: ['horarios'],
    queryFn: () => api.get<Horario[]>('/horarios'),
  });

  // Los horarios generan los turnos del calendario: al crear/desactivar uno,
  // también invalidamos la semana para que el calendario refleje el cambio.
  const invalidar = async () => {
    await qc.invalidateQueries({ queryKey: ['horarios'] });
    await qc.invalidateQueries({ queryKey: ['turnos-semana'] });
  };

  const crear = async (dto: CreateHorario) => {
    await api.post<Horario>('/horarios', dto);
    await invalidar();
  };

  const desactivar = async (id: string) => {
    await api.delete(`/horarios/${id}`);
    await invalidar();
  };

  return {
    horarios: query.data ?? [],
    cargando: query.isLoading,
    error: query.error ? (query.error.message || 'Error cargando horarios') : null,
    crear, desactivar,
    recargar: () => qc.invalidateQueries({ queryKey: ['horarios'] }),
  };
}

export function useSemana(lunes: string) {
  const qc = useQueryClient();
  const query = useQuery({
    queryKey: ['turnos-semana', lunes],
    queryFn: () => api.get<Turno[]>(`/turnos/semana?lunes=${lunes}`),
  });
  const invalidar = () => qc.invalidateQueries({ queryKey: ['turnos-semana'] });

  const marcarAsistencia = async (turnoId: string, alumnoId: string, presente: boolean) => {
    await api.patch(`/turnos/${turnoId}/asistencia`, { alumnoId, presente });
    await invalidar();
  };

  const cancelar = async (turnoId: string, motivo: string) => {
    await api.post(`/turnos/${turnoId}/cancelar`, { motivo });
    await invalidar();
  };

  return {
    turnos: query.data ?? [],
    cargando: query.isLoading,
    error: query.error ? (query.error.message || 'Error cargando la semana') : null,
    marcarAsistencia, cancelar,
    recargar: invalidar,
  };
}
