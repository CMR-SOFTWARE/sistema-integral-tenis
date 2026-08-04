import { aISO, fechaCorta } from './types';
import type { Turno } from './types';
import { cubreFecha, franjaLegible } from '../bloqueos/types';
import type { Bloqueo } from '../bloqueos/types';
import TarjetaTurno from './TarjetaTurno';
import s from './CalendarioPage.module.css';

interface Props {
  /** Las fechas ISO a mostrar: las 7 de la semana, o una sola en la vista Día. */
  dias: string[];
  turnos: Turno[];
  bloqueos: Bloqueo[];
  onAbrirTurno: (turnoId: string) => void;
  nombreDe: (userId: string | null | undefined) => string | null;
}

/** La grilla semanal (una columna por día con turnos + bloqueos). Reutilizable: una
 *  vez en modo "Todos", una por profe en modo "Por profe", y con un solo día en la
 *  vista Día (ahí la columna se estira a todo el ancho). */
export default function GrillaSemana({ dias, turnos, bloqueos, onAbrirTurno, nombreDe }: Props) {
  return (
    <div className={`${s.grilla} ${dias.length === 1 ? s.grillaUnDia : ''}`}>
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
            {delDia.map((t) => (
              <TarjetaTurno
                key={t.id}
                turno={t}
                nombreProfe={nombreDe(t.profesorUserId)}
                onAbrir={(turno) => onAbrirTurno(turno.id)}
              />
            ))}
          </div>
        );
      })}
    </div>
  );
}
