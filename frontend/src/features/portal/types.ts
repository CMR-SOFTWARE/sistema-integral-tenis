// Tipos espejo de PortalDtos.cs + AlumnoLiquidacionDto (lo que ve el alumno).

import type { CargoLinea, EstadoLiquidacion } from '../cuotas/types';
import type { Raqueta } from '../alumnos/types';

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

/** Espejo de CuotaFamiliaDto (Capa 2b): la cuota consolidada de la familia. */
export interface CuotaFamilia {
  anio: number;
  mes: number;
  miembros: MiLiquidacion[];
  total: number;
  pagado: number;
  saldo: number;
  puedeInformar: boolean;
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

/** Espejo de PublicidadDto: un banner de publicidad del club. */
export interface Publicidad {
  id: string;
  nombre: string;
  imagenUrl: string;
  enlace: string | null;
}

/** Espejo de NoticiaDto: una noticia del club. */
export interface Noticia {
  id: string;
  titulo: string;
  mensaje: string;
  /** Destacada: sube al Inicio. Las demás solo se ven en la sección Noticias. */
  importante: boolean;
  venceEl: string | null; // "2026-07-25"
  activo: boolean;
  creadoEl: string;
}

/** Espejo de NotaAlumnoDto: una nota que el profe me compartió. */
export interface NotaProfe {
  id: string;
  texto: string;
  compartida: boolean;
  creadoEl: string;
}

/** Espejo de DatosPagoDto: a dónde transfiero (para el modal de informar pago). */
export interface DatosPago {
  club: string;
  aliasCbu: string | null;
  titular: string | null;
}

// Las raquetas viven en alumnos/types: son del alumno y las ven los dos lados
// (él desde su perfil, el profe desde la ficha).
export type { Encordado, Raqueta } from '../alumnos/types';

/** Cómo veo una franja de la grilla de Reservar. */
export type EstadoSlot = 'Disponible' | 'Ocupado' | 'Mia';

/**
 * Espejo de SlotReservaDto: una franja de la grilla de Reservar.
 *
 * Lo que NO trae es lo importante: ni título de clase, ni cancha, ni precio, ni cuánta
 * gente hay adentro. El título de una clase de una sola persona es el nombre de esa
 * persona, así que el backend directamente no lo manda.
 */
export interface SlotReserva {
  /** Solo cuando estado es 'Disponible': es lo único que se puede pedir. */
  horarioId: string | null;
  estado: EstadoSlot;
  dia: string; // "Tuesday"
  horaInicio: string; // "18:00:00"
  duracionMinutos: number;
  sede: string;
  /** La categoría de la CLASE. Solo si es 'Disponible'. */
  categoria: string | null;
  /** Cuántos lugares quedan. Null = cupo abierto, o la franja no es 'Disponible'. */
  lugaresLibres: number | null;
  /** Ya mandé un pedido pendiente para esta clase (el botón queda deshabilitado). */
  solicitudPendiente: boolean;
}

/**
 * Espejo de MiSolicitudCupoDto: mi pedido de lugar, identificado por CUÁNDO es la
 * clase y no por su nombre (ver SlotReserva).
 */
export interface MiSolicitudCupo {
  id: string;
  dia: string; // "Tuesday"
  horaInicio: string; // "18:00:00"
  duracionMinutos: number;
  sede: string;
  estado: 'Pendiente' | 'Aceptada' | 'Rechazada';
  creadoEl: string;
  resueltoEl: string | null;
}

/** Espejo de DisponibilidadDto: ¿hay cancha libre en esa sede a ese día/hora? */
export interface Disponibilidad {
  hayLugar: boolean;
  canchasLibres: number;
}

/** Espejo de SedeReservaDto: una sede del club (para elegir dónde). */
export interface SedeReserva {
  id: string;
  nombre: string;
}

/** Espejo de ClaseSueltaDto: mi clase suelta (una fecha, pago informado). */
export interface ClaseSuelta {
  id: string;
  sede: string;
  fecha: string; // "2026-07-25"
  horaInicio: string;
  duracionMinutos: number;
  monto: number;
  estado: 'Pendiente' | 'Confirmada' | 'Rechazada';
  pagoInformado: boolean;
  pagado: boolean;
  cancha: string | null;
}

/** Espejo de SolicitudHorarioDto: mi pedido de clase individual fija. */
export interface SolicitudHorario {
  id: string;
  dia: string; // "Tuesday"
  horaInicio: string;
  duracionMinutos: number;
  sede: string;
  estado: 'Pendiente' | 'Aceptada' | 'Rechazada';
  cancha: string | null;
  creadoEl: string;
  resueltoEl: string | null;
}

export interface MiPerfil {
  nombre: string;
  apellido: string;
  fechaNacimiento: string | null;
  dni: string | null;
  telefono: string;
  email: string | null;
  categoria: string;
  estado: string;
  modalidad: string;
  club: string;
  fotoUrl: string | null;
  raquetas: Raqueta[];
}
