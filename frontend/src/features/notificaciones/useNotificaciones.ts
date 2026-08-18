import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';
import type { Notificacion } from './types';

const INTERVALO_POLL_MS = 60_000; // cada minuto alcanza, no es chat en vivo

export function useContadorNoLeidas() {
  return useQuery({
    queryKey: ['notificaciones-contador'],
    queryFn: () => api.get<number>('/notificaciones/no-leidas/contador'),
    refetchInterval: INTERVALO_POLL_MS,
  });
}

export function useNotificaciones(habilitado: boolean) {
  return useQuery({
    queryKey: ['notificaciones'],
    queryFn: () => api.get<Notificacion[]>('/notificaciones'),
    enabled: habilitado,
  });
}

export function useInvalidarNotificaciones() {
  const qc = useQueryClient();
  return () => {
    qc.invalidateQueries({ queryKey: ['notificaciones'] });
    qc.invalidateQueries({ queryKey: ['notificaciones-contador'] });
  };
}
