import LineArt from './LineArt';
import type { LineArtVariante } from './LineArt';
import s from './FranjaTenis.module.css';

/**
 * Franja propia para line art: nunca se superpone al texto.
 * Cada sección elige una variante distinta.
 */
export default function FranjaTenis({ modo = 'golpe' }: { modo?: LineArtVariante }) {
  return (
    <div className={s.franja} aria-hidden>
      <LineArt variante={modo} alScroll={false} compacto={modo !== 'cancha'} />
    </div>
  );
}
