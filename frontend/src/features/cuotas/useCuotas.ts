import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';
import type { LiquidacionMes, Medio, RecaudadoMes, TipoCargo } from './types';

export function useCuotas(anio: number, mes: number) {
  const qc = useQueryClient();

  const query = useQuery({
    queryKey: ['cuotas', anio, mes],
    queryFn: () => api.get<LiquidacionMes>(`/cuotas/${anio}/${mes}`),
  });

  // Balance simple: recaudado de los últimos meses (independiente del mes que mirás).
  const reporte = useQuery({
    queryKey: ['cuotas-reporte'],
    queryFn: () => api.get<RecaudadoMes[]>('/cuotas/reporte?meses=6'),
  });

  // Mover plata cambia las cuotas Y la señal de deuda de la lista de alumnos.
  const invalidar = async () => {
    await qc.invalidateQueries({ queryKey: ['cuotas'] });
    await qc.invalidateQueries({ queryKey: ['alumnos'] });
  };

  const pagarMes = async (alumnoId: string, medio: Medio) => {
    await api.post(`/cuotas/${anio}/${mes}/pagar`, { alumnoId, medio });
    await invalidar();
  };

  const pagarCargo = async (cargoId: string, medio: Medio) => {
    await api.post(`/cuotas/cargos/${cargoId}/pagar`, { medio });
    await invalidar();
  };

  /** Rechaza el pago informado del mes de un alumno ("no me llegó"). */
  const rechazarMes = async (alumnoId: string) => {
    await api.post(`/cuotas/${anio}/${mes}/rechazar`, { alumnoId });
    await invalidar();
  };

  const agregarCargo = async (dto: {
    alumnoId: string;
    tipo: TipoCargo;
    concepto: string;
    monto: number;
  }) => {
    await api.post('/cuotas/cargos', dto);
    await invalidar();
  };

  /** Ajusta el monto de un cargo impago (ej. cambiar la cuota del mes al cobrar). */
  const editarMonto = async (cargoId: string, monto: number) => {
    await api.put(`/cuotas/cargos/${cargoId}/monto`, { monto });
    await invalidar();
  };

  return {
    datos: query.data ?? null,
    cargando: query.isLoading,
    error: query.error ? (query.error.message || 'Error cargando el mes') : null,
    reporte: reporte.data ?? [],
    pagarMes, pagarCargo, rechazarMes, agregarCargo, editarMonto,
    recargar: () => qc.invalidateQueries({ queryKey: ['cuotas', anio, mes] }),
  };
}
