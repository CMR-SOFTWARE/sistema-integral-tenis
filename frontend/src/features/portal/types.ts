// Tipos espejo de PortalDtos.cs + AlumnoLiquidacionDto (lo que ve el alumno).

import type { CargoLinea, EstadoLiquidacion } from '../cuotas/types';

export interface MiTurno {
  id: string;
  fecha: string; // "2026-07-14"
  horaInicio: string; // "18:00:00"
  duracionMinutos: number;
  titulo: string;
  categoria: string | null; // "Cuarta" (null en individual)
  sede: string;
  cancha: string;
  estado: 'Programado' | 'Cancelado';
  canceladoMotivo: string | null;
  presente: boolean;
  companeros: string[]; // "con Mateo, Lucas"
  /** Ya avisé que no vengo (mi aviso; el turno puede seguir en pie). */
  canceladoPorMi: boolean;
  /** Puedo cancelar: vigente, futuro y sin aviso previo. */
  puedoCancelar: boolean;
}

export interface MisTurnos {
  proximos: MiTurno[];
  historial: MiTurno[];
}

/** Espejo de AlumnoLiquidacionDto: MI liquidación del mes. */
export interface MiLiquidacion {
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

/** Espejo de DatosPagoDto: a dónde transfiero (para el modal de informar pago). */
export interface DatosPago {
  club: string;
  aliasCbu: string | null;
  titular: string | null;
}

export interface Raqueta {
  id: string;
  marca: string;
  tension: string | null;
  marcaEncordado: string | null;
}

export interface HorarioDisponible {
  dia: string; // "Tuesday"
  horaInicio: string; // "18:00:00"
  duracionMinutos: number;
  sede: string;
  cancha: string;
  precioEstimado: number | null;
}

export interface GrupoDisponible {
  grupoId: string;
  nombre: string;
  categoria: string | null;
  miembrosActivos: number;
  cupoMaximo: number | null;
  horarios: HorarioDisponible[];
  solicitudPendiente: boolean;
}

export interface SolicitudGrupo {
  id: string;
  alumnoId: string;
  alumnoNombre: string;
  grupoId: string;
  grupoNombre: string;
  estado: 'Pendiente' | 'Aceptada' | 'Rechazada';
  creadoEl: string;
  resueltoEl: string | null;
}

export interface MiPerfil {
  nombre: string;
  apellido: string;
  fechaNacimiento: string;
  dni: string;
  telefono: string;
  email: string | null;
  categoria: string;
  estado: string;
  modalidad: string;
  club: string;
  fotoUrl: string | null;
  raquetas: Raqueta[];
}
