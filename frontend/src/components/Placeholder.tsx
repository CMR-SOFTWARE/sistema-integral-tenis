import RaquetaLineArt from './tenis/RaquetaLineArt';
import { TrophyIcon } from './iconos';
import s from './Placeholder.module.css';

interface Props {
  titulo: string;
  /** Qué va a poder hacer acá cuando esté. Sin esto, un "próximamente" seco. */
  mensaje?: string;
  /** Icono de la sección; el default es la raqueta. */
  icono?: 'raqueta' | 'trofeo';
}

/**
 * Sección todavía no construida. El texto está escrito para el USUARIO (un alumno del
 * club), no para nosotros: antes decía "esta sección llega en una próxima vertical,
 * estamos construyendo el prototipo módulo por módulo", que es jerga de desarrollo.
 */
export default function Placeholder({ titulo, mensaje, icono = 'raqueta' }: Props) {
  return (
    <div className={s.card}>
      {icono === 'trofeo'
        ? <TrophyIcon size={40} className={s.iconSvg} />
        : <RaquetaLineArt oscilar className={s.icon} />}
      <h2 className={s.title}>{titulo}</h2>
      <p className={s.text}>
        {mensaje ?? 'Estamos preparando esta sección. Muy pronto vas a poder usarla.'}
      </p>
      <span className={s.chip}>Próximamente</span>
    </div>
  );
}
