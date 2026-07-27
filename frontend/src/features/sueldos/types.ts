// Tipos espejo de SueldoDtos.cs.

export type Medio = 'Efectivo' | 'Transferencia' | 'Otro';
export type EstadoSueldo = 'Pagado' | 'Pendiente';

/** Un horario que suma al sueldo del profe: sus clases del mes × valor hora. */
export interface SueldoHorario {
  horarioId: string;
  titulo: string;
  dia: string; // "Monday" (DayOfWeek)
  horaInicio: string; // "18:00:00"
  valorHora: number | null;
  clases: number;
  horas: number;
  subtotal: number;
}

/** La fila de un empleado en la pantalla Sueldos: calculado vs pagado. */
export interface EmpleadoSueldo {
  userId: string;
  membresiaId: string;
  nombre: string;
  apellido: string;
  activo: boolean;
  calculado: number;
  pagado: number;
  saldo: number;
  estado: EstadoSueldo;
  horasTotales: number;
  /** false = alguna clase quedó sin tarifa → chip "sin valor hora". */
  tieneValorHora: boolean;
  medioPago: string | null;
  pagadoEl: string | null;
  detalle: SueldoHorario[];
}

export interface LiquidacionSueldos {
  anio: number;
  mes: number;
  totalAPagar: number;
  totalPagado: number;
  totalPendiente: number;
  empleados: EmpleadoSueldo[];
}

/** Egreso de sueldos pagado en un mes (balance simple). */
export interface SueldoMes {
  anio: number;
  mes: number;
  pagado: number;
}
