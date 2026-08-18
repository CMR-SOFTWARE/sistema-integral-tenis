import IconBase from './IconBase';
import type { IconProps } from './IconBase';

export function ChevronLeftIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M15 5.5 8.5 12 15 18.5" />
    </IconBase>
  );
}

export function ChevronRightIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M9 5.5 15.5 12 9 18.5" />
    </IconBase>
  );
}

export function ChevronDownIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M5.5 9 12 15.5 18.5 9" />
    </IconBase>
  );
}
