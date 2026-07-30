// Tipos espejo de StaffDtos.cs (la ficha del empleado).

/** Espejo de StaffDto. */
export interface Staff {
  id: string; // id de la membresía
  userId: string;
  nombre: string;
  apellido: string;
  email: string;
  /** Celular = login del profe. Se muestra, no se edita desde la ficha. */
  telefono: string;
  dni: string | null;
  fechaNacimiento: string | null;
  activo: boolean;
  /** Valor hora base (para el sueldo); null = sin definir. */
  valorHora: number | null;
  /** Club (sede) donde trabaja; null = sin asignar. */
  sedeId: string | null;
  sedeNombre: string | null;
  creadoEl: string;
}

/** Espejo de StaffCreadoDto: usuario/clave se muestran una sola vez. */
export interface StaffCreado {
  staff: Staff;
  usuario: string | null;
  passwordTemporal: string | null;
}

/** Espejo de UpdateStaffDto: el celular (login) no viaja acá. */
export interface UpdateEmpleado {
  nombre: string;
  apellido: string;
  email?: string;
  dni?: string;
  fechaNacimiento?: string;
  valorHora?: number;
  /** Club (sede) donde trabaja; omitir = sin club. */
  sedeId?: string;
}
