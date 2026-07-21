import type { Categoria } from '../alumnos/types';

/** Espejo de MiembroGrupoDto. */
export interface MiembroGrupo {
  alumnoId: string;
  nombre: string;
  apellido: string;
  categoria: Categoria;
  fechaAlta: string;
}

/** Espejo de GrupoResponseDto. */
export interface Grupo {
  id: string;
  nombre: string;
  categoria: Categoria | null;
  cupoMaximo: number | null;
  activo: boolean;
  miembrosActivos: number;
  profesorUserId: string | null;
  miembros: MiembroGrupo[];
}

/** Espejo de CreateGrupoDto. */
export interface CreateGrupo {
  nombre: string;
  categoria?: Categoria;
  cupoMaximo?: number;
  profesorUserId?: string;
}
