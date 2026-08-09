// Tipos espejo de los DTOs de la API .NET + mapas de presentación
// (colores y etiquetas calcados del mockup CourtSet).

export type Categoria =
  | 'Primera' | 'Segunda' | 'Tercera' | 'Cuarta' | 'Quinta' | 'Sexta' // varones
  | 'A' | 'B1' | 'B2' | 'C1' | 'C2' | 'D'                             // damas
  | 'SinCategoria';

export type Estado = 'Activo' | 'Suspendido' | 'Inactivo';

/** Cómo liquida el alumno (ADR-0009). */
export type Modalidad = 'Mensual' | 'PorClase';

export type RelacionTutor = 'Padre' | 'Madre' | 'TutorLegal' | 'Otro';

/** Espejo de AlumnoResponseDto. */
export interface Alumno {
  id: string;
  nombre: string;
  apellido: string;
  dni: string | null;
  telefono: string;
  email: string | null;
  fechaNacimiento: string | null;
  esMenor: boolean;
  categoria: Categoria;
  estado: Estado;
  modalidad: Modalidad;
  arancel: number | null;
  notas: string | null;
  tutorId: string | null;
  creadoEl: string;
  /** Cuota vencida (pasó el día 10 sin pagar): bloquea asignaciones nuevas. */
  deudaVencida: boolean;
  /** Ya tiene acceso al portal (usuario creado). */
  tieneUsuario: boolean;
  /** Cuenta a la que pertenece (= titular). Las fichas con el mismo familiaId son una familia. */
  familiaId: string | null;
  /** Foto de perfil (data URL) que cargó el alumno, o null. */
  fotoUrl: string | null;
  /** Profe de cabecera (dueño o staff); null = sin asignar. */
  profesorUserId: string | null;
  /** Club (sede) de la ficha, heredado del profe de cabecera; null = sin club. */
  sedeId: string | null;
  sedeNombre: string | null;
}

/** Espejo de CreateAlumnoDto. Mínimo: nombre, apellido y teléfono (el usuario). */
export interface CreateAlumno {
  nombre: string;
  apellido: string;
  /** Obligatorio: es el usuario de login y la contraseña inicial. */
  telefono: string;
  /** Opcional. */
  dni?: string;
  /** Opcional. */
  email?: string;
  /** Opcional. */
  fechaNacimiento?: string;
  /** Lo marca el profe: dispara la regla del tutor. */
  esMenor: boolean;
  categoria: Categoria;
  arancel?: number;
  /** El club donde entrena; acota los profes que se le pueden asignar. */
  sedeId?: string;
  profesorUserId?: string;
  notas?: string;
  consentimientoWhatsapp: boolean;
  consentimientoDatos: boolean;
  tutor?: {
    nombre: string;
    apellido: string;
    dni: string;
    telefono: string;
    relacion: RelacionTutor;
  };
}

/** Espejo de UpdateAlumnoDto: el profe corrige la ficha (sin credenciales). */
export interface UpdateAlumno {
  nombre: string;
  apellido: string;
  dni?: string;
  telefono: string;
  email?: string;
  fechaNacimiento?: string;
  esMenor: boolean;
  categoria: Categoria;
  modalidad: Modalidad;
  /** Cuota mensual del alumno (la fuente de verdad del cobro). */
  arancel?: number;
  /** El club donde entrena; acota los profes que se le pueden asignar. */
  sedeId?: string;
  profesorUserId?: string;
  notas?: string;
  /** Tutor a cargar DESPUÉS (opcional): solo si el alumno todavía no tiene uno. */
  tutor?: {
    nombre: string;
    apellido: string;
    dni: string;
    telefono: string;
    relacion: RelacionTutor;
  };
}

/** Espejo de AlumnoCreadoDto: usuario/temporal se muestran UNA sola vez. */
export interface AlumnoCreado {
  alumno: Alumno;
  accesoCreado: boolean;
  usuario: string | null;
  passwordTemporal: string | null;
  /** Se sumó a una familia existente (mismo celular): entra con el login del titular. */
  sumadoAFamilia: boolean;
  /** Nombre del titular a cuya familia se sumó (para el aviso). */
  familiaTitular: string | null;
}

/** Espejo de AccesoCreadoDto (botón "Crear acceso" en fichas sin login). */
export interface AccesoCreado {
  /** Credenciales nuevas; null si se vinculó a una cuenta existente. */
  usuario: string | null;
  passwordTemporal: string | null;
  /** Se vinculó a una cuenta existente (mismo celular): entra con ESE login. */
  vinculado: boolean;
  /** Titular de la cuenta a la que se vinculó (para el aviso al profe). */
  titular: string | null;
}

/** Espejo de AlumnoHorarioDto: un horario asignado del alumno (para la ficha). */
export interface AlumnoHorario {
  dia: string;         // "Lunes", "Martes"...
  horaInicio: string;  // "18:00"
  duracionMinutos: number;
  cancha: string;
  sede: string;
  /** Cómo se llama la clase (el nombre que le puso el profe, o el automático). */
  titulo: string;
  /** Con cuántos la comparte (0 = la tiene para él solo). */
  companeros: number;
}

/** Espejo de EncordadoDto: un encordado del historial de una raqueta. */
export interface Encordado {
  id: string;
  cuerdaVertical: string;
  tensionVertical: string | null;
  /** Null = encordado simple (la misma cuerda en las dos direcciones). */
  cuerdaHorizontal: string | null;
  tensionHorizontal: string | null;
  fecha: string; // "2026-08-01"
  esHibrido: boolean;
}

/** Espejo de RaquetaDto: una raqueta del alumno con su historial. */
export interface Raqueta {
  id: string;
  marca: string;
  modelo: string | null;
  /** Del más nuevo al más viejo (lo ordena el back). */
  encordados: Encordado[];
  /** Ya resuelto por el back; null si nunca se encordó. */
  ultimoEncordado: Encordado | null;
}

/** "Luxilon ALU Power · 24 kg", y con barra si el encordado es híbrido. */
export function resumenEncordado(e: Encordado): string {
  const vertical = [e.cuerdaVertical, e.tensionVertical].filter(Boolean).join(' · ');
  if (!e.esHibrido) return vertical;
  const horizontal = [e.cuerdaHorizontal, e.tensionHorizontal].filter(Boolean).join(' · ');
  return `${vertical} / ${horizontal}`;
}

/** Hoy como "YYYY-MM-DD" (valor por defecto de los campos de fecha). */
export function hoyIso(): string {
  return new Date().toISOString().slice(0, 10);
}

/** "2026-08-03" → "03/08/2026". El dato exacto, para cuando el aproximado no alcanza. */
export function fechaLegible(fechaIso: string): string {
  return new Date(`${fechaIso}T00:00:00`).toLocaleDateString('es-AR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
  });
}

/** Espejo de CargoResumenDto. */
export interface CargoResumen {
  id: string;
  concepto: string;
  monto: number;
  fecha: string;
  pagado: boolean;
  pagoInformado: boolean;
  medioPago: string | null;
}

/** Espejo de AlumnoCuentaDto: la cuenta corriente del alumno para la ficha. */
export interface AlumnoCuenta {
  deudaVencida: boolean;
  totalAdeudado: number;
  cargos: CargoResumen[];
}

/** Dos escalas por género (el género es implícito en el valor). */
export const CATEGORIAS_VARONES: Categoria[] = ['Primera', 'Segunda', 'Tercera', 'Cuarta', 'Quinta', 'Sexta'];
export const CATEGORIAS_DAMAS: Categoria[] = ['A', 'B1', 'B2', 'C1', 'C2', 'D'];
export const CATEGORIAS: Categoria[] = [...CATEGORIAS_VARONES, ...CATEGORIAS_DAMAS, 'SinCategoria'];

export const CAT_LABEL: Record<Categoria, string> = {
  Primera: '1ra', Segunda: '2da', Tercera: '3ra', Cuarta: '4ta', Quinta: '5ta', Sexta: '6ta',
  A: 'A', B1: 'B1', B2: 'B2', C1: 'C1', C2: 'C2', D: 'D',
  SinCategoria: 'S/C',
};

/** CAT_COLOR: varones (frío/verde) y damas (magenta/violeta) para distinguirlas de un vistazo. */
export const CAT_COLOR: Record<Categoria, string> = {
  Primera: '#7c3aed', Segunda: '#2563eb', Tercera: '#0891b2', Cuarta: '#178a4c', Quinta: '#ca8a04', Sexta: '#ea580c',
  A: '#db2777', B1: '#c026d3', B2: '#9333ea', C1: '#7c3aed', C2: '#e11d48', D: '#be123c',
  SinCategoria: '#6b7770',
};

/** estadoChip del mockup (Suspendido se muestra "Pausado", Inactivo "Baja"). */
export const ESTADO_UI: Record<Estado, { label: string; bg: string; fg: string }> = {
  Activo: { label: 'Activo', bg: '#e7f6ec', fg: '#0e6b3c' },
  Suspendido: { label: 'Pausado', bg: '#f3f4f6', fg: '#6b7280' },
  Inactivo: { label: 'Baja', bg: '#fdeaea', fg: '#b91c1c' },
};

/** AV_PAL del mockup: color de avatar estable según el nombre. */
const AV_PAL = ['#178a4c', '#2563eb', '#7c3aed', '#0891b2', '#ea580c', '#be123c', '#ca8a04', '#0e7490'];

export function avatarColor(semilla: string): string {
  let hash = 0;
  for (const ch of semilla) hash = (hash * 31 + ch.charCodeAt(0)) % 997;
  return AV_PAL[hash % AV_PAL.length];
}

export function iniciales(nombre: string, apellido: string): string {
  return `${nombre.charAt(0)}${apellido.charAt(0)}`.toUpperCase();
}

export function formatoPlata(n: number | null): string {
  return n === null ? '—' : '$' + n.toLocaleString('es-AR');
}

/**
 * "2026-05-20" → "hace 2 meses". Redondeo grueso a propósito: lo que importa es
 * si la cuerda está vencida, y para eso "hace 3 meses" dice más que la fecha.
 */
export function haceCuanto(fechaIso: string): string {
  const dias = Math.floor(
    (Date.now() - new Date(`${fechaIso}T00:00:00`).getTime()) / 86_400_000,
  );
  if (dias < 0) return 'programado';
  if (dias === 0) return 'hoy';
  if (dias === 1) return 'ayer';
  if (dias < 30) return `hace ${dias} días`;
  const meses = Math.floor(dias / 30);
  if (meses < 12) return `hace ${meses} ${meses === 1 ? 'mes' : 'meses'}`;
  const anios = Math.floor(dias / 365);
  return `hace ${anios} ${anios === 1 ? 'año' : 'años'}`;
}

export function edad(fechaNacimiento: string): number {
  const nac = new Date(fechaNacimiento);
  const hoy = new Date();
  let e = hoy.getFullYear() - nac.getFullYear();
  const m = hoy.getMonth() - nac.getMonth();
  if (m < 0 || (m === 0 && hoy.getDate() < nac.getDate())) e--;
  return e;
}

/**
 * Categoría por EDAD (Sub), derivada de la fecha de nacimiento e INDEPENDIENTE de
 * la categoría de nivel. Un jugador puede ser Sub-16 y a la vez 5ta o C1. null si
 * es adulto (>18) o no cargó la fecha.
 */
export function subPorEdad(fechaNacimiento: string | null): string | null {
  if (!fechaNacimiento) return null;
  const e = edad(fechaNacimiento);
  if (e <= 10) return 'Sub-10';
  if (e <= 12) return 'Sub-12';
  if (e <= 14) return 'Sub-14';
  if (e <= 16) return 'Sub-16';
  if (e <= 18) return 'Sub-18';
  return null;
}
