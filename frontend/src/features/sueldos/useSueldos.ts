import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';
import type { LiquidacionSueldos, Medio, SueldoMes } from './types';

export function useSueldos(anio: number, mes: number) {
  const qc = useQueryClient();

  const query = useQuery({
    queryKey: ['sueldos', anio, mes],
    queryFn: () => api.get<LiquidacionSueldos>(`/sueldos/${anio}/${mes}`),
  });

  // Balance simple: egreso de sueldos de los últimos meses (independiente del mes que mirás).
  const reporte = useQuery({
    queryKey: ['sueldos-reporte'],
    queryFn: () => api.get<SueldoMes[]>('/sueldos/reporte?meses=6'),
  });

  const invalidar = async () => {
    await qc.invalidateQueries({ queryKey: ['sueldos'] });
    await qc.invalidateQueries({ queryKey: ['sueldos-reporte'] });
  };

  /** Registra el pago del sueldo de un empleado (monto ajustable). */
  const pagar = async (userId: string, monto: number, medio: Medio) => {
    await api.post('/sueldos/pagar', { userId, anio, mes, monto, medio });
    await invalidar();
  };

  /** Borra el pago registrado (por si se cargó mal). */
  const revertir = async (userId: string) => {
    await api.post('/sueldos/revertir', { userId, anio, mes });
    await invalidar();
  };

  return {
    datos: query.data ?? null,
    cargando: query.isLoading,
    error: query.error ? (query.error.message || 'Error cargando el mes') : null,
    reporte: reporte.data ?? [],
    pagar, revertir,
    recargar: () => qc.invalidateQueries({ queryKey: ['sueldos', anio, mes] }),
  };
}
