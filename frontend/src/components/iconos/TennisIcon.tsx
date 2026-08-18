import PelotaOutline from '../tenis/PelotaOutline';
import type { IconProps } from './IconBase';

/**
 * Pelota de tenis en line-art (costuras curvas). Reusa el outline que ya vive
 * en carga/nav: no es un segundo dibujo ni un emoji.
 */
export default function TennisIcon({ size = 20, className, title }: IconProps) {
  return <PelotaOutline size={size} className={className} title={title} />;
}
