import s from './Placeholder.module.css';

/**
 * Página provisoria para las secciones que todavía no tienen su vertical.
 * Se va reemplazando a medida que cada módulo cobra vida (la primera: Alumnos).
 */
export default function Placeholder({ titulo }: { titulo: string }) {
  return (
    <div className={s.card}>
      <div className={s.icon}>🎾</div>
      <h2 className={s.title}>{titulo}</h2>
      <p className={s.text}>
        Esta sección llega en una próxima vertical. Estamos construyendo el
        prototipo módulo por módulo, de punta a punta.
      </p>
    </div>
  );
}
