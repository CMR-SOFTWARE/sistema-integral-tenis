import { ChevronLeftIcon, ChevronRightIcon } from './ChevronIcons';

interface Props {
  direccion: 'anterior' | 'siguiente';
  onClick: () => void;
  label: string;
  className?: string;
  disabled?: boolean;
}

/** Flecha de período (semana/mes/día): SVG + a11y. El look lo pone className del padre. */
export default function BotonNavFecha({ direccion, onClick, label, className, disabled }: Props) {
  return (
    <button
      type="button"
      className={`${className ?? ''} motion-nav`}
      onClick={onClick}
      aria-label={label}
      disabled={disabled}
    >
      {direccion === 'anterior'
        ? <ChevronLeftIcon size={18} />
        : <ChevronRightIcon size={18} />}
    </button>
  );
}
