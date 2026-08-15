import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';
import type { DesafioDobles, MiPerfilDobles, PerfilPublicoRankingDobles, RankingFilaDobles } from './types';

/** @param enVivo Igual que useLeaderboard: polling cada 4s solo en "En vivo". */
export function useLeaderboardDobles(enVivo = false) {
  return useQuery({
    queryKey: ['ranking-dobles'],
    queryFn: () => api.get<RankingFilaDobles[]>('/ranking/dobles'),
    refetchInterval: enVivo ? 4000 : false,
  });
}

export function useMiPerfilDobles() {
  return useQuery({
    queryKey: ['ranking-dobles-mi-perfil'],
    queryFn: () => api.get<MiPerfilDobles>('/ranking/dobles/mi-perfil'),
  });
}

export function useMisDesafiosDobles() {
  return useQuery({
    queryKey: ['desafios-dobles-mis-pendientes'],
    queryFn: () => api.get<DesafioDobles[]>('/desafios/dobles/mis-pendientes'),
  });
}

export function useMisFinalizadosDobles() {
  return useQuery({
    queryKey: ['desafios-dobles-mis-finalizados'],
    queryFn: () => api.get<DesafioDobles[]>('/desafios/dobles/mis-finalizados'),
  });
}

/** Perfil público de dobles de CUALQUIER jugador (se abre al tocar su fila). */
export function usePerfilPublicoDobles(jugadorId: string | null) {
  return useQuery({
    queryKey: ['ranking-dobles-perfil-publico', jugadorId],
    queryFn: () => api.get<PerfilPublicoRankingDobles>(`/ranking/dobles/jugadores/${jugadorId}`),
    enabled: !!jugadorId,
  });
}

export function useHistorialJugadorDobles(jugadorId: string | null) {
  return useQuery({
    queryKey: ['desafios-dobles-finalizados-jugador', jugadorId],
    queryFn: () => api.get<DesafioDobles[]>(`/desafios/dobles/jugador/${jugadorId}/finalizados`),
    enabled: !!jugadorId,
  });
}

export function useRankingOficialDobles(scope: string, valor: string) {
  return useQuery({
    queryKey: ['ranking-dobles-oficial', scope, valor],
    queryFn: () => api.get<RankingFilaDobles[]>(`/ranking/dobles/oficial?scope=${scope}&valor=${encodeURIComponent(valor)}`),
    enabled: scope === 'Global' || valor.trim().length > 0,
  });
}

export function useInvalidarRankingDobles() {
  const qc = useQueryClient();
  return () => {
    qc.invalidateQueries({ queryKey: ['ranking-dobles'] });
    qc.invalidateQueries({ queryKey: ['ranking-dobles-mi-perfil'] });
    qc.invalidateQueries({ queryKey: ['desafios-dobles-mis-pendientes'] });
    qc.invalidateQueries({ queryKey: ['desafios-dobles-mis-finalizados'] });
  };
}
