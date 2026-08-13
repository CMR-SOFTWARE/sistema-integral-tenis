import s from './Placeholder.module.css';

interface Props {
  titulo: string;
  /** Qué va a poder hacer acá cuando esté. Sin esto, un "próximamente" seco. */
  mensaje?: string;
  /** Emoji de la sección; el default es la pelotita. */
  icono?: string;
}

/**
 * Sección todavía no construida. El texto está escrito para el USUARIO (un alumno del
 * club), no para nosotros: antes decía "esta sección llega en una próxima vertical,
 * estamos construyendo el prototipo módulo por módulo", que es jerga de desarrollo.
 */
export default function Placeholder({ titulo, mensaje, icono = '🎾' }: Props) {
  return (
    <div className={s.card}>
      <div className={s.icon}>{icono}</div>
      <h2 className={s.title}>{titulo}</h2>
      <p className={s.text}>
        {mensaje ?? 'Estamos preparando esta sección. Muy pronto vas a poder usarla.'}
      </p>
      <span className={s.chip}>Próximamente</span>
    </div>
  );
}
