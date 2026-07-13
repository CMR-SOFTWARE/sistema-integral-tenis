import s from './HoraSelect.module.css';

interface Props {
  /** "18:00" o "" (sin elegir). */
  value: string;
  onChange: (hora: string) => void;
}

const HORAS = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, '0'));
const MINUTOS = ['00', '15', '30', '45'];

/**
 * Selector de hora SIEMPRE en formato 24h (dos selects). El input type="time"
 * nativo muestra a.m./p.m. según la config regional de Windows y no se puede
 * forzar; con selects se ve igual en cualquier máquina.
 */
export default function HoraSelect({ value, onChange }: Props) {
  const [hora = '', minuto = ''] = value.split(':');

  return (
    <div className={s.fila}>
      <select
        className={s.select}
        value={hora}
        onChange={(e) => onChange(`${e.target.value}:${minuto || '00'}`)}
      >
        <option value="" disabled>hh</option>
        {HORAS.map((h) => (
          <option key={h} value={h}>{h}</option>
        ))}
      </select>
      <span className={s.separador}>:</span>
      <select
        className={s.select}
        value={minuto}
        onChange={(e) => onChange(`${hora || '00'}:${e.target.value}`)}
      >
        <option value="" disabled>mm</option>
        {MINUTOS.map((m) => (
          <option key={m} value={m}>{m}</option>
        ))}
      </select>
    </div>
  );
}
