// Navegación del rol Profesor — las 10 secciones del mockup CourtSet.
// Cada icono es un path SVG estilo stroke (24x24), igual que en el diseño.

export interface NavItem {
  to: string;
  label: string;
  icon: string;
  /** Solo lo ve el dueño (head pro), no el profe empleado (staff). */
  soloOwner?: boolean;
  /** Solo lo ve el admin de plataforma (dueño de la app). */
  soloAdmin?: boolean;
}

export const profNav: NavItem[] = [
  { to: '/dashboard', label: 'Inicio', icon: 'M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z' },
  {
    // Alumnos y Calendario los ve también el profe empleado (filtrados a lo suyo)
    to: '/alumnos',
    label: 'Alumnos',
    icon: 'M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75',
  },
  {
    to: '/calendario',
    label: 'Calendario',
    icon: 'M3 5h18v16H3zM3 9h18M8 3v4M16 3v4',
  },
  {
    to: '/grupos',
    label: 'Grupos',
    icon: 'M18 21a6 6 0 0 0-12 0M12 13a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM21 10h-4M19 8v4',
    soloOwner: true,
  },
  {
    to: '/profesores',
    label: 'Profesores',
    icon: 'M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75',
    soloOwner: true,
  },
  { to: '/horarios', label: 'Horarios', icon: 'M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18zM12 7v5l3 3', soloOwner: true },
  {
    to: '/cuotas',
    label: 'Cuotas',
    icon: 'M12 1v22M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6',
    soloOwner: true,
  },
  {
    to: '/solicitudes',
    label: 'Solicitudes',
    icon: 'M22 6l-10 7L2 6M2 4h20v16H2z',
    soloOwner: true,
  },
  {
    to: '/avisos',
    label: 'Avisos',
    icon: 'M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 0 1-3.46 0',
    soloOwner: true,
  },
  { to: '/bloqueos', label: 'Bloqueos', icon: 'M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18zM5.6 5.6l12.8 12.8', soloOwner: true },
  {
    to: '/cancelaciones',
    label: 'Cancelaciones',
    icon: 'M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18zM15 9l-6 6M9 9l6 6',
    soloOwner: true,
  },
  { to: '/reportes', label: 'Reportes', icon: 'M18 20V10M12 20V4M6 20v-6', soloOwner: true },
  {
    to: '/configuracion',
    label: 'Configuración',
    icon: 'M4 21v-7M4 10V3M12 21v-9M12 8V3M20 21v-5M20 12V3M1 14h6M9 8h6M17 16h6',
    soloOwner: true,
  },
  {
    to: '/plataforma',
    label: 'Plataforma',
    icon: 'M2 3h20v14H2zM8 21h8M12 17v4',
    soloAdmin: true,
  },
];

/** Título del header según la ruta activa. */
export const pageTitles: Record<string, string> = Object.fromEntries(
  profNav.map((item) => [item.to, item.label]),
);

// ── Portal del alumno (las 4 secciones del mockup) ──

export const alumnoNav: NavItem[] = [
  { to: '/portal', label: 'Inicio', icon: 'M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z' },
  { to: '/portal/turnos', label: 'Mis turnos', icon: 'M3 5h18v16H3zM3 9h18M8 3v4M16 3v4' },
  { to: '/portal/reservar', label: 'Reservar', icon: 'M12 8v8M8 12h8M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18z' },
  { to: '/portal/cuota', label: 'Mi cuota', icon: 'M1 5h22v14H1zM1 10h22' },
  { to: '/portal/servicios', label: 'Servicios', icon: 'M20 7h-9M14 17H5M17 3a4 4 0 1 0 0 8 4 4 0 0 0 0-8zM7 13a4 4 0 1 0 0 8 4 4 0 0 0 0-8z' },
  { to: '/portal/perfil', label: 'Mi perfil', icon: 'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21a8 8 0 0 1 16 0' },
  { to: '/portal/club', label: 'Mi club', icon: 'M3 21h18M5 21V7l7-4 7 4v14M9 21v-6h6v6' },
];

export const portalTitles: Record<string, string> = Object.fromEntries(
  alumnoNav.map((item) => [item.to, item.label]),
);
