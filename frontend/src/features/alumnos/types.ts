// Tipos espejo de los DTOs de la API .NET + mapas de presentación
// (colores y etiquetas calcados del mockup CourtSet).

export type Categoria =
  | 'Primera' | 'Segunda' | 'Tercera' | 'Cuarta'
  | 'Quinta' | 'Sexta' | 'Septima' | 'SinCategoria';

export type Estado = 'Activo' | 'Suspendido' | 'Inactivo';

export type RelacionTutor = 'Padre' | 'Madre' | 'TutorLegal' | 'Otro';

/** Espejo de AlumnoResponseDto. */
export interface Alumno {
  id: string;
  nombre: string;
  apellido: string;
  dni: string;
  telefono: string;
  email: string | null;
  fechaNacimiento: string;
  esMenor: boolean;
  categoria: Categoria;
  estado: Estado;
  arancel: number | null;
  notas: string | null;
  tutorId: string | null;
  creadoEl: string;
  /** Cuota vencida (pasó el día 10 sin pagar): bloquea asignaciones nuevas. */
  deudaVencida: boolean;
}

/** Espejo de CreateAlumnoDto. */
export interface CreateAlumno {
  nombre: string;
  apellido: string;
  dni: string;
  telefono: string;
  email?: string;
  fechaNacimiento: string;
  categoria: Categoria;
  arancel?: number;
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

export const CATEGORIAS: Categoria[] = [
  'Primera', 'Segunda', 'Tercera', 'Cuarta', 'Quinta', 'Sexta', 'Septima', 'SinCategoria',
];

export const CAT_LABEL: Record<Categoria, string> = {
  Primera: '1ra', Segunda: '2da', Tercera: '3ra', Cuarta: '4ta',
  Quinta: '5ta', Sexta: '6ta', Septima: '7ma', SinCategoria: 'S/C',
};

/** CAT_COLOR del mockup. */
export const CAT_COLOR: Record<Categoria, string> = {
  Primera: '#7c3aed', Segunda: '#2563eb', Tercera: '#0891b2', Cuarta: '#178a4c',
  Quinta: '#ca8a04', Sexta: '#ea580c', Septima: '#be123c', SinCategoria: '#6b7770',
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

export function edad(fechaNacimiento: string): number {
  const nac = new Date(fechaNacimiento);
  const hoy = new Date();
  let e = hoy.getFullYear() - nac.getFullYear();
  const m = hoy.getMonth() - nac.getMonth();
  if (m < 0 || (m === 0 && hoy.getDate() < nac.getDate())) e--;
  return e;
}
