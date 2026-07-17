// Tipos espejo de CuotaDtos.cs + helpers del mes.

export type TipoCargo = 'Clase' | 'Producto' | 'Ajuste';
export type Medio = 'Efectivo' | 'Transferencia' | 'Otro';
export type EstadoLiquidacion = 'Pagada' | 'Informado' | 'Pendiente' | 'Vencida';

export interface CargoLinea {
  id: string;
  tipo: TipoCargo;
  concepto: string;
  monto: number;
  fecha: string;
  pagado: boolean;
  pagadoEl: string | null;
  medioPago: string | null;
  /** El alumno avisó que transfirió; el profe todavía no confirmó. */
  pagoInformado: boolean;
  pagoInformadoEl: string | null;
}

export interface Liquidacion {
  alumnoId: string;
  nombre: string;
  apellido: string;
  modalidad: 'Mensual' | 'PorClase';
  total: number;
  pagado: number;
  saldo: number;
  estado: EstadoLiquidacion;
  cargos: CargoLinea[];
}

export interface LiquidacionMes {
  anio: number;
  mes: number;
  totalFacturado: number;
  totalCobrado: number;
  totalPendiente: number;
  alumnosVencidos: number;
  liquidaciones: Liquidacion[];
}

export interface Precios {
  valorHoraGrupal: number | null;
  valorClaseIndividual: number | null;
}

export const MESES = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre',
];

/** Chips de estado calcados del mockup (pagado/pendiente/vencido). */
export const ESTADO_LIQ_UI: Record<EstadoLiquidacion, { bg: string; fg: string }> = {
  Pagada: { bg: '#e7f6ec', fg: '#0e6b3c' },
  Informado: { bg: '#e8f0fe', fg: '#1a56db' }, // avisó, esperando confirmación
  Pendiente: { bg: '#fef6e7', fg: '#b7791f' },
  Vencida: { bg: '#fdeaea', fg: '#b91c1c' },
};
