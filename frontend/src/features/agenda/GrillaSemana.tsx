import { aISO, diaDe, fechaCorta, horaCorta, horaFin } from './types';
import type { Horario, Turno } from './types';
import { cubreFecha, franjaLegible } from '../bloqueos/types';
import type { Bloqueo } from '../bloqueos/types';
import TarjetaTurno from './TarjetaTurno';
import s from './CalendarioPage.module.css';

interface Props {
  /** Las fechas ISO a mostrar: las 7 de la semana, o una sola en la vista Día. */
  dias: string[];
  turnos: Turno[];
  /** Clases sin alumnos: no generan turnos pero ocupan la cancha (ver abajo). */
  clasesVacias: Horario[];
  bloqueos: Bloqueo[];
  onAbrirTurno: (turnoId: string) => void;
  onAbrirClaseVacia: (horario: Horario) => void;
  nombreDe: (userId: string | null | undefined) => string | null;
}

/** La grilla semanal (una columna por día con turnos + bloqueos). Reutilizable: una
 *  vez en modo "Todos", una por profe en modo "Por profe", y con un solo día en la
 *  vista Día (ahí la columna se estira a todo el ancho). */
export default function GrillaSemana({
  dias, turnos, clasesVacias, bloqueos, onAbrirTurno, onAbrirClaseVacia, nombreDe,
}: Props) {
  return (
    <div className={`${s.grilla} ${dias.length === 1 ? s.grillaUnDia : ''}`}>
      {dias.map((fecha) => {
        const delDia = turnos.filter((t) => t.fecha === fecha);
        const bloqueosDia = bloqueos.filter((b) => cubreFecha(b, fecha));
        // La clase vacía se repite todas las semanas como cualquier horario: se
        // ubica por su día de la semana, no por una fecha (no tiene turnos).
        const vaciasDia = clasesVacias.filter((h) => h.dia === diaDe(fecha));
        const hoyCol = fecha === aISO(new Date());
        const vacio = delDia.length === 0 && bloqueosDia.length === 0 && vaciasDia.length === 0;
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
            {vacio && <div className={s.libre}>—</div>}
            {delDia.map((t) => (
              <TarjetaTurno
                key={t.id}
                turno={t}
                nombreProfe={nombreDe(t.profesorUserId)}
                onAbrir={(turno) => onAbrirTurno(turno.id)}
              />
            ))}
            {vaciasDia.map((h) => (
              <button key={h.id} className={s.claseVacia} onClick={() => onAbrirClaseVacia(h)}>
                <div className={s.turnoHora}>
                  {horaCorta(h.horaInicio)} – {horaFin(h.horaInicio, h.duracionMinutos)}
                </div>
                <div className={s.claseVaciaTitulo}>{h.titulo}</div>
                <div className={s.claseVaciaAviso}>Sin alumnos — no genera clases</div>
                <div className={s.claseVaciaLugar}>
                  {h.sede ? `${h.sede} · ` : ''}{h.cancha}
                </div>
              </button>
            ))}
          </div>
        );
      })}
    </div>
  );
}
