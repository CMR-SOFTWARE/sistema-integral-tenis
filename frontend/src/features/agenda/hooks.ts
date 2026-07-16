import { useCallback, useEffect, useState } from 'react';
import { api } from '../../lib/api';
import type { CreateHorario, Horario, Sede, Turno } from './types';

export function useSedes() {
  const [sedes, setSedes] = useState<Sede[]>([]);
  const [cargando, setCargando] = useState(true);

  const cargar = useCallback(async () => {
    setCargando(true);
    try {
      setSedes(await api.get<Sede[]>('/sedes'));
    } finally {
      setCargando(false);
    }
  }, []);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  const crearSede = async (nombre: string) => {
    await api.post<Sede>('/sedes', { nombre });
    await cargar();
  };

  const agregarCancha = async (sedeId: string, nombre: string) => {
    await api.post(`/sedes/${sedeId}/canchas`, { nombre });
    await cargar();
  };

  /** Baja lógica: deja de ofrecerse para horarios nuevos (la historia queda). */
  const desactivarSede = async (sedeId: string) => {
    await api.delete(`/sedes/${sedeId}`);
    await cargar();
  };

  const reactivarSede = async (sedeId: string) => {
    await api.post(`/sedes/${sedeId}/reactivar`, {});
    await cargar();
  };

  return { sedes, cargando, crearSede, agregarCancha, desactivarSede, reactivarSede };
}

export function useHorarios() {
  const [horarios, setHorarios] = useState<Horario[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    try {
      setHorarios(await api.get<Horario[]>('/horarios'));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error cargando horarios');
    } finally {
      setCargando(false);
    }
  }, []);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  const crear = async (dto: CreateHorario) => {
    await api.post<Horario>('/horarios', dto);
    await cargar();
  };

  const desactivar = async (id: string) => {
    await api.delete(`/horarios/${id}`);
    await cargar();
  };

  return { horarios, cargando, error, crear, desactivar };
}

export function useSemana(lunes: string) {
  const [turnos, setTurnos] = useState<Turno[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    try {
      setTurnos(await api.get<Turno[]>(`/turnos/semana?lunes=${lunes}`));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error cargando la semana');
    } finally {
      setCargando(false);
    }
  }, [lunes]);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  const marcarAsistencia = async (turnoId: string, alumnoId: string, presente: boolean) => {
    await api.patch(`/turnos/${turnoId}/asistencia`, { alumnoId, presente });
    await cargar();
  };

  const cancelar = async (turnoId: string, motivo: string) => {
    await api.post(`/turnos/${turnoId}/cancelar`, { motivo });
    await cargar();
  };

  return { turnos, cargando, error, marcarAsistencia, cancelar };
}
