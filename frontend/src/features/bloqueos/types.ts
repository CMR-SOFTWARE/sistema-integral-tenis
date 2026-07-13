// Tipos espejo de BloqueoDtos.cs + etiquetas de la vertical Bloqueos.

import type { DiaSemana } from '../agenda/types';

export type TipoBloqueo = 'Fijo' | 'Rango';

export type MotivoBloqueo = 'MalClima' | 'MotivosPersonales' | 'Torneo' | 'MantenimientoCancha';

export const MOTIVOS: MotivoBloqueo[] = ['MalClima', 'MotivosPersonales', 'Torneo', 'MantenimientoCancha'];

export const MOTIVO_LABEL: Record<MotivoBloqueo, string> = {
  MalClima: 'Mal clima',
  MotivosPersonales: 'Motivos personales',
  Torneo: 'Torneo',
  MantenimientoCancha: 'Mantenimiento de cancha',
};

/** Espejo de BloqueoResponseDto (motivo llega ya legible: "Mal clima"). */
export interface Bloqueo {
  id: string;
  tipo: TipoBloqueo;
  dia: DiaSemana | null;
  fecha: string | null; // "2026-07-21"
  horaInicio: string;   // "18:00:00"
  horaFin: string;
  canchaId: string | null;
  cancha: string | null; // null = todas las canchas
  motivo: string | null;
  creadoEl: string;
}

/** Espejo de CreateBloqueoDto. */
export interface CreateBloqueo {
  tipo: TipoBloqueo;
  dia?: DiaSemana;
  fecha?: string;
  horaInicio: string; // "18:00"
  horaFin: string;
  canchaId?: string;
  motivo?: MotivoBloqueo;
}

/** Espejo de AlumnoAfectadoDto (una fila del modal Impacto). */
export interface AlumnoAfectado {
  fecha: string;
  horaInicio: string;
  titulo: string;
  alumnoNombre: string;
  telefono: string | null;
}

/** Espejo de ImpactoBloqueoDto. */
export interface Impacto {
  turnosAfectados: number;
  afectados: AlumnoAfectado[];
}

/** "00:00–23:59" se muestra como "Todo el día"; el resto como "18:00–20:00". */
export function franjaLegible(horaInicio: string, horaFin: string): string {
  const corta = (h: string) => h.slice(0, 5);
  if (corta(horaInicio) === '00:00' && corta(horaFin) === '23:59') return 'Todo el día';
  return `${corta(horaInicio)}–${corta(horaFin)}`;
}

/** ¿El bloqueo cubre esta fecha? (espejo del criterio de fecha de BloqueoService.Cubre) */
export function cubreFecha(b: Bloqueo, fechaIso: string): boolean {
  if (b.tipo === 'Rango') return b.fecha === fechaIso;
  const DIAS_JS: DiaSemana[] = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  return b.dia === DIAS_JS[new Date(`${fechaIso}T00:00:00`).getDay()];
}
