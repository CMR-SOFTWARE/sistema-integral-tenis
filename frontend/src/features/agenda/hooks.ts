import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';
import type { CreateHorario, Horario, Sede, Turno, UpdateHorario } from './types';

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

  // Los horarios generan los turnos del calendario y definen quién "toma clase"
  // (solo se cobra cuota al que tiene clase) y las horas del profe (sueldo): al
  // crear/editar/desactivar uno, invalidamos la semana, las cuotas y los sueldos.
  const invalidar = async () => {
    await qc.invalidateQueries({ queryKey: ['horarios'] });
    await qc.invalidateQueries({ queryKey: ['turnos-semana'] });
    await qc.invalidateQueries({ queryKey: ['turnos-mes'] });
    await qc.invalidateQueries({ queryKey: ['cuotas'] });
    await qc.invalidateQueries({ queryKey: ['sueldos'] });
  };

  const crear = async (dto: CreateHorario) => {
    await api.post<Horario>('/horarios', dto);
    await invalidar();
  };

  const editar = async (id: string, dto: UpdateHorario) => {
    await api.put<Horario>(`/horarios/${id}`, dto);
    await invalidar();
  };

  const desactivar = async (id: string) => {
    await api.delete(`/horarios/${id}`);
    await invalidar();
  };

  // Sumar/sacar del roster repone o quita al alumno de los turnos futuros y cambia
  // el divisor de la cuota: el back reconcilia, nosotros invalidamos lo mismo que
  // al tocar el horario. Además de sus clases (la ficha), se invalidan el listado y
  // el inicio: darle su primera clase a alguien de la lista de espera lo convierte
  // en alumno, y las dos vistas lo cuentan.
  const invalidarAlumno = async () => {
    await invalidar();
    await qc.invalidateQueries({ queryKey: ['alumno-horarios'] });
    await qc.invalidateQueries({ queryKey: ['alumnos'] });
    await qc.invalidateQueries({ queryKey: ['dashboard'] });
    // Darle su primera clase lo saca de la lista de espera: la lista y el badge
    // de la pestaña tienen que enterarse (antes el badge solo se refrescaba al
    // cambiar de pestaña, y de paso pedía de más).
    await qc.invalidateQueries({ queryKey: ['solicitudes'] });
    await qc.invalidateQueries({ queryKey: ['solicitudes-conteo'] });
  };

  // Los alumnos se suman de a uno contra el back (cada alta reconcilia el calendario
  // de ese alumno por separado), pero se invalida UNA sola vez al final: invalidar
  // por alta hacía que sumar cuatro alumnos volviera a bajar la semana cuatro veces
  // —y la semana es el request más caro que tenemos—.
  const agregarAlumnos = async (horarioId: string, alumnoIds: string[]) => {
    for (const alumnoId of alumnoIds)
      await api.post(`/horarios/${horarioId}/alumnos`, { alumnoId });
    await invalidarAlumno();
  };

  const quitarAlumno = async (horarioId: string, alumnoId: string) => {
    await api.delete(`/horarios/${horarioId}/alumnos/${alumnoId}`);
    await invalidarAlumno();
  };

  return {
    horarios: query.data ?? [],
    cargando: query.isLoading,
    error: query.error ? (query.error.message || 'Error cargando horarios') : null,
    crear, editar, desactivar, agregarAlumnos, quitarAlumno,
    recargar: () => qc.invalidateQueries({ queryKey: ['horarios'] }),
  };
}

export function useSemana(lunes: string, enabled = true) {
  const qc = useQueryClient();
  const query = useQuery({
    queryKey: ['turnos-semana', lunes],
    queryFn: () => api.get<Turno[]>(`/turnos/semana?lunes=${lunes}`),
    enabled,
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

/** Turnos de un mes (vista mensual). El back genera lo que falte al pedirlo. */
export function useMes(anio: number, mes: number, enabled = true) {
  const qc = useQueryClient();
  const query = useQuery({
    queryKey: ['turnos-mes', anio, mes],
    queryFn: () => api.get<Turno[]>(`/turnos/mes?anio=${anio}&mes=${mes}`),
    enabled,
  });

  // Cancelar/asistencia impacta la misma clase en la semanal: invalidamos ambas.
  const invalidar = async () => {
    await qc.invalidateQueries({ queryKey: ['turnos-mes'] });
    await qc.invalidateQueries({ queryKey: ['turnos-semana'] });
  };

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
    error: query.error ? (query.error.message || 'Error cargando el mes') : null,
    marcarAsistencia, cancelar,
    recargar: invalidar,
  };
}
