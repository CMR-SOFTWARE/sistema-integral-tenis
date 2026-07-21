// Tipos espejo de AgendaDtos.cs + helpers de fecha/hora de la agenda.

export type DiaSemana =
  | 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday'
  | 'Friday' | 'Saturday' | 'Sunday';

export interface Cancha {
  id: string;
  nombre: string;
  activo: boolean;
}

export interface Sede {
  id: string;
  nombre: string;
  activo: boolean;
  canchas: Cancha[];
}

export interface Horario {
  id: string;
  titulo: string;
  categoria: string | null;
  esIndividual: boolean;
  canchaId: string;
  cancha: string;
  sede: string;
  dia: DiaSemana;
  horaInicio: string; // "18:00:00"
  duracionMinutos: number;
  activo: boolean;
  profesorUserId: string | null;
}

export interface CreateHorario {
  canchaId: string;
  grupoId?: string;
  alumnoId?: string;
  profesorUserId?: string;
  dia: DiaSemana;
  horaInicio: string;
  duracionMinutos: number;
}

export interface ParticipanteTurno {
  alumnoId: string;
  nombre: string;
  apellido: string;
  presente: boolean;
  /** Cuota vencida (pasó el día 10 sin pagar): señal para el profe. */
  deudaVencida: boolean;
}

export interface Turno {
  id: string;
  fecha: string; // "2026-07-14"
  horaInicio: string;
  duracionMinutos: number;
  estado: 'Programado' | 'Cancelado';
  canceladoMotivo: string | null;
  titulo: string;
  cancha: string;
  sede: string;
  participantes: ParticipanteTurno[];
}

/** Orden y etiquetas de la grilla semanal (lunes primero). */
export const DIAS: { valor: DiaSemana; label: string; corto: string }[] = [
  { valor: 'Monday', label: 'Lunes', corto: 'Lun' },
  { valor: 'Tuesday', label: 'Martes', corto: 'Mar' },
  { valor: 'Wednesday', label: 'Miércoles', corto: 'Mié' },
  { valor: 'Thursday', label: 'Jueves', corto: 'Jue' },
  { valor: 'Friday', label: 'Viernes', corto: 'Vie' },
  { valor: 'Saturday', label: 'Sábado', corto: 'Sáb' },
  { valor: 'Sunday', label: 'Domingo', corto: 'Dom' },
];

/** "18:00:00" → "18:00" */
export function horaCorta(hora: string): string {
  return hora.slice(0, 5);
}

/** Lunes de la semana que contiene a la fecha dada, como "YYYY-MM-DD". */
export function lunesDe(fecha: Date): string {
  const d = new Date(fecha);
  const offset = (d.getDay() + 6) % 7; // getDay(): domingo=0 → lunes=0
  d.setDate(d.getDate() - offset);
  return aISO(d);
}

export function aISO(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

export function sumarDias(iso: string, dias: number): string {
  const d = new Date(`${iso}T00:00:00`);
  d.setDate(d.getDate() + dias);
  return aISO(d);
}

/** "2026-07-14" → "mar 14/07" */
export function fechaCorta(iso: string): string {
  const d = new Date(`${iso}T00:00:00`);
  return d.toLocaleDateString('es-AR', { weekday: 'short', day: '2-digit', month: '2-digit' });
}

/** Rango legible de la semana: "13 al 19 de julio de 2026" */
export function rangoSemana(lunesIso: string): string {
  const lunes = new Date(`${lunesIso}T00:00:00`);
  const domingo = new Date(`${sumarDias(lunesIso, 6)}T00:00:00`);
  const mes = domingo.toLocaleDateString('es-AR', { month: 'long', year: 'numeric' });
  return `${lunes.getDate()} al ${domingo.getDate()} de ${mes}`;
}
