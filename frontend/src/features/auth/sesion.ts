// Sesión del usuario logueado: espejo de SesionDto + storage local.
// El token viaja en Authorization (lib/api.ts); acá vive su persistencia.

export interface Ficha {
  alumnoId: string;
  nombre: string;
  apellido: string;
  club: string;
}

export interface Sesion {
  token: string | null;
  nombre: string;
  apellido: string;
  email: string;
  esProfesor: boolean;
  alumno: Ficha | null;
  fichasPorReclamar: Ficha[];
}

const KEY_TOKEN = 'token';
const KEY_SESION = 'sesion';

/** Guarda token (si vino) y datos de sesión. El token no se re-guarda en /yo. */
export function guardarSesion(s: Sesion): void {
  if (s.token) localStorage.setItem(KEY_TOKEN, s.token);
  const { token: _token, ...datos } = s;
  localStorage.setItem(KEY_SESION, JSON.stringify(datos));
}

export function obtenerToken(): string | null {
  return localStorage.getItem(KEY_TOKEN);
}

export function obtenerSesion(): Sesion | null {
  const crudo = localStorage.getItem(KEY_SESION);
  if (!crudo) return null;
  try {
    return { token: null, ...(JSON.parse(crudo) as Omit<Sesion, 'token'>) };
  } catch {
    return null;
  }
}

export function cerrarSesion(): void {
  localStorage.removeItem(KEY_TOKEN);
  localStorage.removeItem(KEY_SESION);
}

/** Jugador logueado sin ficha vinculada: entra al portal en modo "sin club". */
export function sinClub(s: Sesion | null): boolean {
  return s !== null && !s.esProfesor && s.alumno === null;
}
