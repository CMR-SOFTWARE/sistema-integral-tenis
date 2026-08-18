import IconBase from './IconBase';
import type { IconProps } from './IconBase';

/** Calendario del mismo trazo que el icono de Agenda en la nav. */
export default function CalendarIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <rect x="3.5" y="5" width="17" height="15.5" rx="2" />
      <path d="M3.5 9.5h17" />
      <path d="M8 3.5v3.5" />
      <path d="M16 3.5v3.5" />
    </IconBase>
  );
}
