// Tipos espejo de SolicitudDtos.cs.

/** Espejo de SolicitudPendienteDto (lo que ve el profe para decidir). */
export interface SolicitudPendiente {
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
