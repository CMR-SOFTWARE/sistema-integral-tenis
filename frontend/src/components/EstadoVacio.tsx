import type { ReactNode } from 'react';
import { CalendarIcon, TennisIcon, TrophyIcon } from './iconos';
import s from './EstadoVacio.module.css';

type Variante = 'pelota' | 'calendario' | 'trofeo';

interface Props {
  children: ReactNode;
  variante?: Variante;
}

const ICONO = {
  pelota: TennisIcon,
  calendario: CalendarIcon,
  trofeo: TrophyIcon,
} as const;

/** Vacío de pantalla: icono vectorial chico, sin emoji ni ilustración grande. */
export default function EstadoVacio({ children, variante = 'pelota' }: Props) {
  const Icono = ICONO[variante];
  return (
    <div className={s.wrap}>
      <Icono size={28} className={s.icono} />
      <div className={s.texto}>{children}</div>
    </div>
  );
}
