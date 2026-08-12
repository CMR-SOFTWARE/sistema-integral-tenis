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
  /** Cuenta (= titular). Las liquidaciones con el mismo familiaId son una familia. */
  familiaId: string | null;
  modalidad: 'Mensual' | 'PorClase';
  total: number;
  pagado: number;
  saldo: number;
  estado: EstadoLiquidacion;
  /** La cuota mensual del alumno está definida; si es false, se muestra "sin definir". */
  cuotaDefinida: boolean;
  cargos: CargoLinea[];
}

/** Recaudado de un mes (balance simple del panel financiero). */
export interface RecaudadoMes {
  anio: number;
  mes: number;
  recaudado: number;
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

/** El precio de la clase suelta del portal; la cuota mensual sale del arancel del alumno. */
export interface Precios {
  valorClaseIndividual: number | null;
}

// ── Servicios + pedidos (M4) ──

export type EstadoPedido = 'Pendiente' | 'Aceptado' | 'Rechazado';

export interface Servicio {
  id: string;
  nombre: string;
  precio: number;
  activo: boolean;
}

export interface Pedido {
  id: string;
  alumnoId: string;
  alumnoNombre: string;
  nombreServicio: string;
  precio: number;
  estado: EstadoPedido;
  pedidoEl: string;
  resueltoEl: string | null;
}

export const MESES = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre',
];

/** Chips de estado calcados del mockup (pagado/pendiente/vencido). */
export const ESTADO_LIQ_UI: Record<EstadoLiquidacion, { bg: string; fg: string }> = {
  Pagada: { bg: '#eaf3d8', fg: '#386641' },
  Informado: { bg: '#efe6c8', fg: '#6a994e' }, // avisó, esperando confirmación
  Pendiente: { bg: '#fef6e7', fg: '#a67c2a' },
  Vencida: { bg: '#fdeaea', fg: '#bc4749' },
};
