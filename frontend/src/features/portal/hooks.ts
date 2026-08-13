import { useQuery } from '@tanstack/react-query';
import { api } from '../../lib/api';
import { obtenerSesion } from '../auth/sesion';
import { useFichaActiva } from './FichaActivaContext';
import type { Servicio, Pedido } from '../cuotas/types';
import type {
  Noticia,
  ClaseSuelta,
  CuotaFamilia,
  MiLiquidacion,
  MiSolicitudCupo,
  MisTurnos,
  NotaProfe,
  Publicidad,
  SedeReserva,
  SlotReserva,
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

/** Cuota consolidada de la familia (Capa 2b): a nivel familia, no del miembro activo. */
export function useMiCuotaFamilia(anio: number, mes: number) {
  return useQuery({
    queryKey: ['portal-cuota-familia', anio, mes],
    queryFn: () => api.get<CuotaFamilia>(`/portal/mi-cuota-familia/${anio}/${mes}`),
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

/** Las noticias vigentes del club. Vienen con las importantes primero (lo ordena el back). */
export function useNoticias() {
  return useQuery({
    queryKey: ['portal-noticias'],
    queryFn: () => api.get<Noticia[]>('/portal/noticias'),
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
  /** La grilla entera de mi club: disponibles, ocupadas y las mías. */
  slots: SlotReserva[];
  solCupo: MiSolicitudCupo[];
  solHorario: SolicitudHorario[];
  clasesSueltas: ClaseSuelta[];
}

export function useReservarData() {
  const { alumnoId } = useFichaActiva();
  return useQuery({
    queryKey: ['portal-reservar', alumnoId],
    queryFn: async (): Promise<ReservarData> => {
      const [slots, solCupo, solHorario, clasesSueltas] = await Promise.all([
        api.get<SlotReserva[]>('/portal/clases-disponibles'),
        api.get<MiSolicitudCupo[]>('/portal/solicitudes-cupo'),
        api.get<SolicitudHorario[]>('/portal/solicitudes-horario'),
        api.get<ClaseSuelta[]>('/portal/clases-sueltas'),
      ]);
      return { slots, solCupo, solHorario, clasesSueltas };
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
