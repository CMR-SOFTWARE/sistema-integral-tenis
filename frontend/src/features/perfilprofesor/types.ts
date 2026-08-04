// Espejo de los DTOs de PerfilProfesorDtos.cs

export interface FotoPerfil {
  id: string;
  url: string;
  pieDeFoto: string | null;
  orden: number;
}

export interface HitoTrayectoria {
  id: string;
  anio: number;
  titulo: string;
  detalle: string | null;
  orden: number;
}

/** El perfil como lo ve su dueño (incluye lo no publicado). */
export interface MiPerfilProfesor {
  nombre: string;
  apellido: string;
  club: string;
  titular: string | null;
  subtitulo: string | null;
  bio: string | null;
  especialidades: string[];
  portadaUrl: string | null;
  avatarUrl: string | null;
  publicado: boolean;
  fotos: FotoPerfil[];
  hitos: HitoTrayectoria[];
}

export interface GuardarPerfil {
  titular: string | null;
  subtitulo: string | null;
  bio: string | null;
  especialidades: string[];
  publicado: boolean;
}

export interface GuardarHito {
  anio: number;
  titulo: string;
  detalle: string | null;
}

/** La tarjeta de un profe en la lista del club. */
export interface ProfesorTarjeta {
  userId: string;
  nombre: string;
  apellido: string;
  esDueño: boolean;
  titular: string | null;
  avatarUrl: string | null;
  especialidades: string[];
  tienePerfil: boolean;
}

export interface PerfilProfesorPublico {
  userId: string;
  nombre: string;
  apellido: string;
  club: string;
  titular: string | null;
  subtitulo: string | null;
  bio: string | null;
  especialidades: string[];
  portadaUrl: string | null;
  avatarUrl: string | null;
  fotos: FotoPerfil[];
  hitos: HitoTrayectoria[];
}

/** Los topes que valida el back; el front avisa antes de que rebote. */
export const TOPES = {
  fotos: 12,
  hitos: 15,
  especialidades: 8,
  titular: 80,
  subtitulo: 120,
  bio: 2000,
  pieDeFoto: 120,
  tituloHito: 120,
  detalleHito: 400,
} as const;
