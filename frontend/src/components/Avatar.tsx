import { avatarColor, iniciales } from '../features/alumnos/types';

interface Props {
  nombre: string;
  apellido: string;
  fotoUrl?: string | null;
  /** Lado en px (cuadrado). */
  size?: number;
  /** Radio del borde en px (default: redondeado tipo tarjeta). */
  radius?: number;
}

/**
 * Avatar único del alumno: muestra su FOTO si la cargó, o sus iniciales con
 * un color derivado del nombre. Un solo lugar para que la identidad visual
 * sea consistente en toda la app (sidebar, listas, perfil…).
 */
export default function Avatar({ nombre, apellido, fotoUrl, size = 40, radius }: Props) {
  const r = radius ?? Math.round(size * 0.29); // ~18px en un avatar de 62
  if (fotoUrl) {
    return (
      <img
        src={fotoUrl}
        alt={`${nombre} ${apellido}`}
        style={{ width: size, height: size, borderRadius: r, objectFit: 'cover', display: 'block', flexShrink: 0 }}
      />
    );
  }
  const color = avatarColor(nombre + apellido);
  return (
    <div
      style={{
        width: size,
        height: size,
        borderRadius: r,
        background: `${color}1a`,
        color,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        fontWeight: 800,
        fontSize: Math.round(size * 0.38),
        flexShrink: 0,
      }}
    >
      {iniciales(nombre, apellido)}
    </div>
  );
}
