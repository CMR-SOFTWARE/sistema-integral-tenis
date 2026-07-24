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
  /** "owner" (dueño/head pro) o "staff" (profe empleado); null si no es profe. */
  rol: 'owner' | 'staff' | null;
  /** Admin de plataforma (dueño de la app): habilita la sección "Plataforma". */
  esAdmin: boolean;
  /** Estado del club propio; "PendientePago" manda al checkout. */
  estadoTenant: 'PendientePago' | 'Activo' | 'Suspendido' | null;
  /** Nació con contraseña inicial del profe (informativo). */
  debeCambiarPassword: boolean;
  /** Mis datos deportivos (el jugador sin club los completa en su perfil). */
  dni: string | null;
  telefono: string | null;
  fechaNacimiento: string | null;
  categoria: string | null;
  /** Ficha por defecto (la primera de la familia); null si no está en ningún club. */
  alumno: Ficha | null;
  /** TODAS las fichas de la familia (Capa 2): el titular gestiona a varios miembros. */
  alumnos: Ficha[];
}

/** ¿Tiene lo necesario para mandar una solicitud? (espejo de la regla del back) */
export function datosCompletos(s: Sesion | null): boolean {
  return !!s && !!s.dni && !!s.telefono && !!s.fechaNacimiento;
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
    const s = { token: null, ...(JSON.parse(crudo) as Omit<Sesion, 'token'>) };
    // Compatibilidad con sesiones viejas sin la lista de familia
    if (!Array.isArray(s.alumnos)) s.alumnos = s.alumno ? [s.alumno] : [];
    return s;
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
