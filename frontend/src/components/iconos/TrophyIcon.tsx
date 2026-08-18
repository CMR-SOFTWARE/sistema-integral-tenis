import IconBase from './IconBase';
import type { IconProps } from './IconBase';

/** Trofeo geométrico: copa, asas y peana. Sin cartoon ni relleno 3D. */
export default function TrophyIcon(props: IconProps) {
  return (
    <IconBase {...props}>
      <path d="M8 4h8v4.5a4 4 0 0 1-8 0V4z" />
      <path d="M8 6H5.2a2.3 2.3 0 0 0 0 4.6H8" />
      <path d="M16 6h2.8a2.3 2.3 0 0 1 0 4.6H16" />
      <path d="M12 12.5V16" />
      <path d="M9 21h6" />
      <path d="M10 16h4v3h-4z" />
    </IconBase>
  );
}
