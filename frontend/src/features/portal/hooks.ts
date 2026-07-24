import { useQuery } from '@tanstack/react-query';
import { api } from '../../lib/api';
import { obtenerSesion } from '../auth/sesion';
import { useFichaActiva } from './FichaActivaContext';
import type { Servicio, Pedido } from '../cuotas/types';
import type {
  Aviso,
  ClaseSuelta,
  GrupoDisponible,
  MiLiquidacion,
  MisTurnos,
  NotaProfe,
  Publicidad,
  SedeReserva,
  SolicitudGrupo,
  SolicitudHorario,
} from './types';

/**
 * Lecturas del portal del alumno con React Query.
 * Las keys se comparten entre páginas: Inicio, Mis Turnos y Mi Cuota piden los
 * mismos endpoints, así navegar entre ellas reusa lo cacheado (staleTime 15s).
 * Sin ficha (sin club) no hay nada que pedir → `enabled: false`.
 */
const tieneFicha = () => obtenerSesion()?.alumno != null;

export function useMisTurnos() {
  const { alumnoId } = useFichaActiva();
  return useQuery({
    queryKey: ['portal-mis-turnos', alumnoId],
    queryFn: () => api.get<MisTurnos>('/portal/mis-turnos'),
    enabled: tieneFicha(),
  });
}

export function useMiCuota(anio: number, mes: number) {
  const { alumnoId } = useFichaActiva();
  return useQuery({
    queryKey: ['portal-mi-cuota', alumnoId, anio, mes],
    // 204 (sin movimientos) → api.get resuelve undefined; lo normalizamos a null.
    queryFn: () =>
      api.get<MiLiquidacion | undefined>(`/portal/mi-cuota/${anio}/${mes}`).then((c) => c ?? null),
    enabled: tieneFicha(),
  });
}

export function usePublicidad() {
  return useQuery({
    queryKey: ['portal-publicidad'],
    queryFn: () => api.get<Publicidad[]>('/portal/publicidad'),
    enabled: tieneFicha(),
  });
}

export function useAvisos() {
  return useQuery({
    queryKey: ['portal-avisos'],
    queryFn: () => api.get<Aviso[]>('/portal/avisos'),
    enabled: tieneFicha(),
  });
}

export function useNotas() {
  const { alumnoId } = useFichaActiva();
  return useQuery({
    queryKey: ['portal-notas', alumnoId],
    queryFn: () => api.get<NotaProfe[]>('/portal/notas'),
    enabled: tieneFicha(),
  });
}

export interface ServiciosData {
  servicios: Servicio[];
  pedidos: Pedido[];
}

export function useServiciosYPedidos() {
  const { alumnoId } = useFichaActiva();
  return useQuery({
    queryKey: ['portal-servicios', alumnoId],
    queryFn: async (): Promise<ServiciosData> => {
      const [servicios, pedidos] = await Promise.all([
        api.get<Servicio[]>('/portal/servicios'),
        api.get<Pedido[]>('/portal/pedidos'),
      ]);
      return { servicios, pedidos };
    },
    enabled: tieneFicha(),
  });
}

export interface ReservarData {
  grupos: GrupoDisponible[];
  solGrupo: SolicitudGrupo[];
  solHorario: SolicitudHorario[];
  clasesSueltas: ClaseSuelta[];
}

export function useReservarData() {
  const { alumnoId } = useFichaActiva();
  return useQuery({
    queryKey: ['portal-reservar', alumnoId],
    queryFn: async (): Promise<ReservarData> => {
      const [grupos, solGrupo, solHorario, clasesSueltas] = await Promise.all([
        api.get<GrupoDisponible[]>('/portal/grupos-disponibles'),
        api.get<SolicitudGrupo[]>('/portal/solicitudes-grupo'),
        api.get<SolicitudHorario[]>('/portal/solicitudes-horario'),
        api.get<ClaseSuelta[]>('/portal/clases-sueltas'),
      ]);
      return { grupos, solGrupo, solHorario, clasesSueltas };
    },
    enabled: tieneFicha(),
  });
}

export function usePortalSedes() {
  return useQuery({
    queryKey: ['portal-sedes'],
    queryFn: () => api.get<SedeReserva[]>('/portal/sedes'),
    enabled: tieneFicha(),
  });
}
