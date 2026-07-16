import type { Sede } from './types';
import s from './HorariosPage.module.css';

interface Props {
  sedes: Sede[];
  /** Nombre de la sede elegida; '' = todas. */
  valor: string;
  onChange: (sede: string) => void;
}

/**
 * Filtro por sede de la agenda (Horarios y Calendario). Filtra por NOMBRE
 * porque es lo que traen los DTOs de horario/turno. Si el profe tiene una
 * sola sede no se muestra: no hay nada que elegir.
 */
export default function SelectSede({ sedes, valor, onChange }: Props) {
  if (sedes.length < 2) return null;

  return (
    <select className={s.selectSede} value={valor} onChange={(e) => onChange(e.target.value)}>
      <option value="">Todas las sedes</option>
      {sedes.map((sede) => (
        <option key={sede.id} value={sede.nombre}>{sede.nombre}</option>
      ))}
    </select>
  );
}
