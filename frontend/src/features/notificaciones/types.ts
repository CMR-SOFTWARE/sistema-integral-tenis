export interface Notificacion {
  id: string;
  tipo: string;
  mensaje: string;
  entidadId: string | null;
  leida: boolean;
  creadaEl: string;
}
