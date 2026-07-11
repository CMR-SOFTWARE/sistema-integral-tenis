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
}
