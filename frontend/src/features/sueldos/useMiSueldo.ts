import { useQuery } from '@tanstack/react-query';
import { api } from '../../lib/api';
import type { EmpleadoSueldo } from './types';

/** El sueldo del PROPIO profe empleado en el mes actual (para su panel). */
export function useMiSueldo() {
  const hoy = new Date();
  const anio = hoy.getFullYear();
  const mes = hoy.getMonth() + 1;

  const query = useQuery({
    queryKey: ['mi-sueldo', anio, mes],
    queryFn: () => api.get<EmpleadoSueldo>(`/mi-sueldo/${anio}/${mes}`),
  });

  return {
    mes,
    sueldo: query.data ?? null,
    cargando: query.isLoading,
    error: query.error ? (query.error.message || 'Error cargando tu sueldo') : null,
  };
}
