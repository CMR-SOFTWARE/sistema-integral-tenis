import LineArt from './LineArt';
import type { LineArtVariante } from './LineArt';

/** Line art en loop: héroes, login y bloques deportivos. */
export default function PelotaTrayectoria({
  className,
  variante = 'golpe',
  compacto = false,
}: {
  className?: string;
  variante?: LineArtVariante;
  compacto?: boolean;
}) {
  return <LineArt variante={variante} className={className} alScroll={false} compacto={compacto} />;
}
