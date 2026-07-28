import { useProfesores } from '../profesores/useProfesores';
import s from './HorariosPage.module.css';

interface Props {
  /** userId del profe elegido; '' = todos. */
  valor: string;
  onChange: (userId: string) => void;
}

/**
 * Filtro por profe del calendario. Filtra por userId (lo trae el turno). Si el
 * club tiene un solo profe (el dueño) no se muestra: no hay nada que filtrar.
 */
export default function SelectProfe({ valor, onChange }: Props) {
  const { profes } = useProfesores();
  if (profes.length < 2) return null;

  return (
    <select className={s.selectSede} value={valor} onChange={(e) => onChange(e.target.value)}>
      <option value="">Todos los profes</option>
      {profes.map((p) => (
        <option key={p.userId} value={p.userId}>{p.nombre}{p.esDueño ? ' (vos)' : ''}</option>
      ))}
    </select>
  );
}
