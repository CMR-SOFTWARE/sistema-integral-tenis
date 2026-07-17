import { useCallback, useEffect, useState } from 'react';
import { api } from '../../lib/api';
import type { LiquidacionMes, Medio, TipoCargo } from './types';

export function useCuotas(anio: number, mes: number) {
  const [datos, setDatos] = useState<LiquidacionMes | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    try {
      setDatos(await api.get<LiquidacionMes>(`/cuotas/${anio}/${mes}`));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error cargando el mes');
    } finally {
      setCargando(false);
    }
  }, [anio, mes]);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  const pagarMes = async (alumnoId: string, medio: Medio) => {
    await api.post(`/cuotas/${anio}/${mes}/pagar`, { alumnoId, medio });
    await cargar();
  };

  const pagarCargo = async (cargoId: string, medio: Medio) => {
    await api.post(`/cuotas/cargos/${cargoId}/pagar`, { medio });
    await cargar();
  };

  /** Rechaza el pago informado del mes de un alumno ("no me llegó"). */
  const rechazarMes = async (alumnoId: string) => {
    await api.post(`/cuotas/${anio}/${mes}/rechazar`, { alumnoId });
    await cargar();
  };

  const agregarCargo = async (dto: {
    alumnoId: string;
    tipo: TipoCargo;
    concepto: string;
    monto: number;
  }) => {
    await api.post('/cuotas/cargos', dto);
    await cargar();
  };

  return { datos, cargando, error, pagarMes, pagarCargo, rechazarMes, agregarCargo, recargar: cargar };
}
