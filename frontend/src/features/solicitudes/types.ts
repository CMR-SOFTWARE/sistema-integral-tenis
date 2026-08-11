// Tipos espejo de SolicitudDtos.cs.

/**
 * Por qué alguien está en la lista de espera. `PidioCupo` y `LoAnotoElProfe` pueden
 * ser alumnos que ya vienen: por eso hay quien está en Alumnos y en la espera a la vez.
 */
export type MotivoEspera = 'SinClase' | 'PidioCupo' | 'LoAnotoElProfe';

/** Espejo de SolicitudPendienteDto (una fila de la lista de espera). */
export interface SolicitudPendiente {
  /** Id de la FICHA (el del pedido va en solicitudId). */
  id: string;
  nombre: string;
  apellido: string;
  email: string;
  dni: string | null;
  telefono: string | null;
  fechaNacimiento: string | null;
  esMenor: boolean;
  categoria: string | null;
  mensaje: string | null;
  creadoEl: string;
  motivo: MotivoEspera;
  /** El pedido a rechazar; solo si el motivo es PidioCupo. */
  solicitudId: string | null;
  /** La clase que pidió; solo si el motivo es PidioCupo. */
  clase: string | null;
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
