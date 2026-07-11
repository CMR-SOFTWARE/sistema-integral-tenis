// Navegación del rol Profesor — las 10 secciones del mockup CourtSet.
// Cada icono es un path SVG estilo stroke (24x24), igual que en el diseño.

export interface NavItem {
  to: string;
  label: string;
  icon: string;
}

export const profNav: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', icon: 'M3 3h7v7H3zM14 3h7v7h-7zM14 14h7v7h-7zM3 14h7v7H3z' },
  {
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
  },
  { to: '/horarios', label: 'Horarios', icon: 'M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18zM12 7v5l3 3' },
  {
    to: '/cuotas',
    label: 'Cuotas',
    icon: 'M12 1v22M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6',
  },
  { to: '/bloqueos', label: 'Bloqueos', icon: 'M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18zM5.6 5.6l12.8 12.8' },
  {
    to: '/cancelaciones',
    label: 'Cancelaciones',
    icon: 'M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18zM15 9l-6 6M9 9l6 6',
  },
  { to: '/reportes', label: 'Reportes', icon: 'M18 20V10M12 20V4M6 20v-6' },
  {
    to: '/configuracion',
    label: 'Configuración',
    icon: 'M4 21v-7M4 10V3M12 21v-9M12 8V3M20 21v-5M20 12V3M1 14h6M9 8h6M17 16h6',
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
  { to: '/portal/cuota', label: 'Mi cuota', icon: 'M1 5h22v14H1zM1 10h22' },
  { to: '/portal/perfil', label: 'Mi perfil', icon: 'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21a8 8 0 0 1 16 0' },
];

export const portalTitles: Record<string, string> = Object.fromEntries(
  alumnoNav.map((item) => [item.to, item.label]),
);
