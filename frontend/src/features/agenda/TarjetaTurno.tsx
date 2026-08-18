import { horaCorta, horaFin, nombreCorto } from './types';
import type { Turno } from './types';
import s from './TarjetaTurno.module.css';

interface Props {
  turno: Turno;
  /** Nombre del profe que la da (null si el turno no tiene profe asignado). */
  nombreProfe: string | null;
  onAbrir: (turno: Turno) => void;
}

/** Cuántos alumnos se nombran antes de resumir con "+N". */
const MAX_NOMBRES = 3;

/**
 * Una clase en el calendario. Es la MISMA tarjeta en las tres vistas (día, semana
 * y el detalle del mes): el profe pidió ver de un vistazo quiénes vienen, quién la
 * da y en qué club, sin tener que abrir clase por clase.
 */
export default function TarjetaTurno({ turno, nombreProfe, onAbrir }: Props) {
  const cancelado = turno.estado === 'Cancelado';
  const ausentes = turno.participantes.filter((p) => !p.presente).length;
  const hayDeuda = turno.participantes.some((p) => p.deudaVencida);

  // En una clase de a uno el título YA es el nombre del alumno: no se repite.
  const nombres = turno.participantes.length > 1
    ? turno.participantes.slice(0, MAX_NOMBRES).map((p) => nombreCorto(p.nombre, p.apellido)).join(', ')
    : null;
  const restantes = turno.participantes.length - MAX_NOMBRES;

  return (
    <button
      className={`${s.tarjeta} motion-card ${cancelado ? s.cancelada : ''}`}
      onClick={() => onAbrir(turno)}
    >
      <div className={s.hora}>
        {horaCorta(turno.horaInicio)} – {horaFin(turno.horaInicio, turno.duracionMinutos)}
      </div>

      <div className={s.titulo}>
        <span className={s.tituloTexto}>{turno.titulo}</span>
        {/* El dato ya viajaba: hasta ahora solo se veía abriendo el turno */}
        {!cancelado && hayDeuda && <span className={s.puntoDeuda} title="Hay cuota vencida" />}
      </div>

      {cancelado ? (
        <div className={s.motivo}>Cancelado: {turno.canceladoMotivo}</div>
      ) : (
        <>
          {nombres && (
            <div className={s.alumnos}>
              {nombres}{restantes > 0 ? ` +${restantes}` : ''}
            </div>
          )}
          <div className={s.lugar}>
            {turno.sede ? `${turno.sede} · ` : ''}{turno.cancha}
          </div>
          <div className={s.pie}>
            {nombreProfe ?? 'Sin profe'}
            {ausentes > 0 && (
              <span className={s.faltas}>· {ausentes} falta{ausentes > 1 ? 's' : ''}</span>
            )}
          </div>
        </>
      )}
    </button>
  );
}
