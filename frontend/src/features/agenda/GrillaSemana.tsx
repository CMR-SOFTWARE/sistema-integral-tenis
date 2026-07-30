import { aISO, fechaCorta, horaCorta } from './types';
import type { Turno } from './types';
import { cubreFecha, franjaLegible } from '../bloqueos/types';
import type { Bloqueo } from '../bloqueos/types';
import s from './CalendarioPage.module.css';

interface Props {
  /** Las 7 fechas ISO de la semana (lunes a domingo). */
  dias: string[];
  turnos: Turno[];
  bloqueos: Bloqueo[];
  onAbrirTurno: (turnoId: string) => void;
  nombreDe: (userId: string | null | undefined) => string | null;
}

/** La grilla semanal (7 columnas de día con turnos + bloqueos). Reutilizable: una
 *  vez en modo "Todos" y una por profe en modo "Por profe". */
export default function GrillaSemana({ dias, turnos, bloqueos, onAbrirTurno, nombreDe }: Props) {
  return (
    <div className={s.grilla}>
      {dias.map((fecha) => {
        const delDia = turnos.filter((t) => t.fecha === fecha);
        const bloqueosDia = bloqueos.filter((b) => cubreFecha(b, fecha));
        const hoyCol = fecha === aISO(new Date());
        return (
          <div key={fecha} className={`${s.columna} ${hoyCol ? s.columnaHoy : ''}`}>
            <div className={s.columnaTitulo}>{fechaCorta(fecha)}</div>
            {bloqueosDia.map((b) => (
              <div key={b.id} className={s.franjaBloqueada}>
                <div className={s.turnoHora}>{franjaLegible(b.horaInicio, b.horaFin)}</div>
                <div className={s.franjaMotivo}>
                  {b.motivo ?? 'Bloqueo fijo'}{b.cancha ? ` · ${b.cancha}` : ''}
                </div>
              </div>
            ))}
            {delDia.length === 0 && bloqueosDia.length === 0 && <div className={s.libre}>—</div>}
            {delDia.map((t) => {
              const cancelado = t.estado === 'Cancelado';
              const ausentes = t.participantes.filter((p) => !p.presente).length;
              return (
                <button
                  key={t.id}
                  className={`${s.turno} ${cancelado ? s.turnoCancelado : ''}`}
                  onClick={() => onAbrirTurno(t.id)}
                >
                  <div className={s.turnoHora}>{horaCorta(t.horaInicio)}</div>
                  <div className={s.turnoTitulo}>{t.titulo}</div>
                  <div className={s.turnoDetalle}>
                    {cancelado
                      ? `Cancelado: ${t.canceladoMotivo}`
                      : `${t.cancha} · ${t.participantes.length} 👤${ausentes > 0 ? ` · ${ausentes} falta${ausentes > 1 ? 's' : ''}` : ''}`}
                  </div>
                  {!cancelado && t.profesorUserId && (
                    <div className={s.turnoProfe}>{nombreDe(t.profesorUserId)}</div>
                  )}
                </button>
              );
            })}
          </div>
        );
      })}
    </div>
  );
}
