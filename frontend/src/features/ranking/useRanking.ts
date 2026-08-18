import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';
import type { Desafio, MiPerfilRanking, PerfilPublicoRanking, RankingFila } from './types';

// Cross-tenant a propósito: sin useFichaActiva ni fichaId en la key — el
// jugador es 1:1 con el Usuario (identidad global), no con una ficha de club.

/** @param enVivo Si está viendo la pestaña "En vivo", refresca sola cada 4s
 *  (se pausa mientras la pestaña del navegador está en background). */
export function useLeaderboard(enVivo = false) {
  return useQuery({
    queryKey: ['ranking'],
    queryFn: () => api.get<RankingFila[]>('/ranking'),
    refetchInterval: enVivo ? 4000 : false,
  });
}

export function useMiPerfilRanking() {
  return useQuery({
    queryKey: ['ranking-mi-perfil'],
    queryFn: () => api.get<MiPerfilRanking>('/ranking/mi-perfil'),
  });
}

export function useMisDesafios() {
  return useQuery({
    queryKey: ['desafios-mis-pendientes'],
    queryFn: () => api.get<Desafio[]>('/desafios/mis-pendientes'),
  });
}

export function useMisFinalizados() {
  return useQuery({
    queryKey: ['desafios-mis-finalizados'],
    queryFn: () => api.get<Desafio[]>('/desafios/mis-finalizados'),
  });
}

/** Perfil público de CUALQUIER jugador (se abre al tocar su fila en la tabla). */
export function usePerfilPublico(jugadorId: string | null) {
  return useQuery({
    queryKey: ['ranking-perfil-publico', jugadorId],
    queryFn: () => api.get<PerfilPublicoRanking>(`/ranking/jugadores/${jugadorId}`),
    enabled: !!jugadorId,
  });
}

export function useHistorialJugador(jugadorId: string | null) {
  return useQuery({
    queryKey: ['desafios-finalizados-jugador', jugadorId],
    queryFn: () => api.get<Desafio[]>(`/desafios/jugador/${jugadorId}/finalizados`),
    enabled: !!jugadorId,
  });
}

export function useRankingOficial(scope: string, valor: string) {
  return useQuery({
    queryKey: ['ranking-oficial', scope, valor],
    queryFn: () => api.get<RankingFila[]>(`/ranking/oficial?scope=${scope}&valor=${encodeURIComponent(valor)}`),
    enabled: scope === 'Global' || valor.trim().length > 0,
  });
}

export function useInvalidarRanking() {
  const qc = useQueryClient();
  return () => {
    qc.invalidateQueries({ queryKey: ['ranking'] });
    qc.invalidateQueries({ queryKey: ['ranking-mi-perfil'] });
    qc.invalidateQueries({ queryKey: ['desafios-mis-pendientes'] });
    qc.invalidateQueries({ queryKey: ['desafios-mis-finalizados'] });
  };
}
