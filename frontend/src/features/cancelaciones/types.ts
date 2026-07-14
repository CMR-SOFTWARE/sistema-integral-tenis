// Tipos espejo de CancelacionDtos.cs.

/** Espejo de CancelacionDto: turno entero cancelado o aviso de un alumno. */
export interface Cancelacion {
  fecha: string; // "2026-07-15"
  horaInicio: string; // "18:00:00"
  titulo: string;
  motivo: string | null;
  por: 'Profesor' | 'Alumno';
  alumnoNombre: string | null;
  telefono: string | null;
  canceladoEl: string;
}
