// Tipos espejo de AgendaDtos.cs + helpers de fecha/hora de la agenda.

import type { Categoria } from '../alumnos/types';

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

/** Espejo de MiembroHorarioDto: un alumno del roster de la clase. */
export interface MiembroHorario {
  alumnoId: string;
  nombre: string;
  apellido: string;
  categoria: Categoria;
  fechaAlta: string;
}

export interface Horario {
  id: string;
  /** Lo que se muestra: el nombre cargado, o uno armado con el roster. */
  titulo: string;
  /** El nombre TAL CUAL lo cargó el profe (null = se arma solo). Para el formulario. */
  nombre: string | null;
  categoria: Categoria | null;
  /** Cuántos alumnos entran; null = sin límite. */
  cupoMaximo: number | null;
  canchaId: string;
  cancha: string;
  sede: string;
  dia: DiaSemana;
  horaInicio: string; // "18:00:00"
  duracionMinutos: number;
  activo: boolean;
  profesorUserId: string | null;
  valorHoraProfe: number | null;
  /** Los que vienen hoy (sin los dados de baja). */
  miembros: MiembroHorario[];
  miembrosActivos: number;
}

export interface CreateHorario {
  canchaId: string;
  nombre?: string;
  cupoMaximo?: number;
  categoria?: Categoria;
  /** Los alumnos con los que arranca la clase; puede ir vacío y sumarlos después. */
  alumnoIds: string[];
  profesorUserId?: string;
  /** Valor hora del profe para esta clase (override; vacío = usa el base del profe). */
  valorHoraProfe?: number;
  dia: DiaSemana;
  horaInicio: string;
  duracionMinutos: number;
}

/**
 * Edición de una clase. El roster NO va acá: se toca con sus propios endpoints
 * (agregar/quitar alumno), que además reconcilian el calendario.
 */
export interface UpdateHorario {
  canchaId: string;
  nombre?: string;
  cupoMaximo?: number;
  categoria?: Categoria;
  profesorUserId?: string;
  valorHoraProfe?: number;
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
  /** Profe a cargo (del horario); null = suelto o sin asignar. Para el filtro por profe. */
  profesorUserId: string | null;
  /** El horario (plantilla) del que salió; null = clase suelta. Habilita las acciones de horario. */
  horarioId: string | null;
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

/** "2026-08-05" → 'Wednesday' (para cruzar una fecha con el día de un horario). */
export function diaDe(fechaIso: string): DiaSemana {
  const d = new Date(`${fechaIso}T00:00:00`);
  return DIAS[(d.getDay() + 6) % 7].valor; // getDay(): domingo=0 → DIAS va lunes primero
}

/** "18:00:00" → "18:00" */
export function horaCorta(hora: string): string {
  return hora.slice(0, 5);
}

/** "18:00:00" + 90 → "19:30" (para mostrar la franja completa de la clase). */
export function horaFin(horaInicio: string, duracionMinutos: number): string {
  const [h, m] = horaInicio.split(':').map(Number);
  const total = h * 60 + m + duracionMinutos;
  const hh = String(Math.floor(total / 60) % 24).padStart(2, '0');
  const mm = String(total % 60).padStart(2, '0');
  return `${hh}:${mm}`;
}

/** "Martín", "Pérez" → "M. Pérez" (entra en la columna angosta de la semana). */
export function nombreCorto(nombre: string, apellido: string): string {
  const inicial = nombre.trim().charAt(0);
  return inicial ? `${inicial}. ${apellido}` : apellido;
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

/** "2026-08-05" → "Miércoles 5 de agosto" (rótulo de la vista Día) */
export function fechaLarga(iso: string): string {
  const d = new Date(`${iso}T00:00:00`);
  const texto = d.toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' });
  return texto.charAt(0).toUpperCase() + texto.slice(1);
}

/** Rango legible de la semana: "13 al 19 de julio de 2026" */
export function rangoSemana(lunesIso: string): string {
  const lunes = new Date(`${lunesIso}T00:00:00`);
  const domingo = new Date(`${sumarDias(lunesIso, 6)}T00:00:00`);
  const mes = domingo.toLocaleDateString('es-AR', { month: 'long', year: 'numeric' });
  return `${lunes.getDate()} al ${domingo.getDate()} de ${mes}`;
}

/**
 * 42 días (6 semanas, lunes primero) para la grilla mensual estilo calendario.
 * `enMes=false` son los días de relleno del mes anterior/siguiente (atenuados).
 */
export function diasDelMesGrid(anio: number, mes: number): { iso: string; enMes: boolean }[] {
  const primero = new Date(anio, mes - 1, 1);
  const offset = (primero.getDay() + 6) % 7; // getDay(): domingo=0 → lunes=0
  const inicio = new Date(anio, mes - 1, 1 - offset);
  return Array.from({ length: 42 }, (_, i) => {
    const d = new Date(inicio);
    d.setDate(inicio.getDate() + i);
    return { iso: aISO(d), enMes: d.getMonth() === mes - 1 };
  });
}

/** Día del mes de una fecha ISO ("2026-07-14" → 14). */
export function diaDelMes(iso: string): number {
  return Number(iso.slice(8, 10));
}
