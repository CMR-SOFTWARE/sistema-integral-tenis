// Tipos espejo de SolicitudDtos.cs.

import type { Alumno } from '../alumnos/types';

/**
 * Por qué alguien está en la lista de espera. `PidioCupo` y `LoAnotoElProfe` pueden
 * ser alumnos que ya vienen: por eso hay quien está en Alumnos y en la espera a la vez.
 */
export type MotivoEspera = 'SinClase' | 'PidioCupo' | 'LoAnotoElProfe';

/**
 * Espejo de EsperaResponseDto: una fila de la lista de espera. Extiende `Alumno`
 * porque la espera muestra la MISMA tabla que Alumnos —el back manda la ficha entera—
 * y le agrega por qué está y desde cuándo.
 */
export interface SolicitudPendiente extends Alumno {
  motivo: MotivoEspera;
  /** El pedido a rechazar; solo si el motivo es PidioCupo. */
  solicitudId: string | null;
  /** La clase que pidió; solo si el motivo es PidioCupo. */
  clase: string | null;
  /** Desde cuándo espera. No es `creadoEl`: al anotado a mano se le cuenta desde la marca. */
  esperaDesde: string;
}

/** Espejo de MiSolicitudDto (lo que ve el alumno en el portal). */
export interface MiSolicitud {
  id: string;
  club: string;
  estado: 'Pendiente' | 'Aprobada' | 'Rechazada';
  mensaje: string | null;
  creadoEl: string;
  resueltoEl: string | null;
}
