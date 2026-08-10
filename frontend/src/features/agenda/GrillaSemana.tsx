import { DIAS, aISO, diaDe, diaDelMes, fechaCorta, horaCorta, horaFin } from './types';
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
  /** El día desplegado en el celular (en escritorio se ven los siete). */
  diaAbierto: string;
  onAbrirDia: (fecha: string) => void;
  onAbrirTurno: (turnoId: string) => void;
  onAbrirClaseVacia: (horario: Horario) => void;
  nombreDe: (userId: string | null | undefined) => string | null;
}

/**
 * La grilla semanal (una columna por día con turnos + bloqueos). Reutilizable: una
 * vez en modo "Todos", una por profe en modo "Por profe", y con un solo día en la
 * vista Día (ahí la columna se estira a todo el ancho).
 *
 * En el CELULAR se dibuja distinta sin cambiar el DOM: arriba un mapa de los siete
 * días con su cuenta de clases, y abajo se despliega solo el día abierto. Siete
 * columnas en 390 px darían 47 px cada una, donde no entran ni los alumnos ni el
 * club ni el profe, que es justo lo que el profe pidió ver. Lo decide el CSS.
 */
export default function GrillaSemana({
  dias, turnos, clasesVacias, bloqueos, diaAbierto, onAbrirDia,
  onAbrirTurno, onAbrirClaseVacia, nombreDe,
}: Props) {
  const hoy = aISO(new Date());
  const vaciasDe = (fecha: string) => clasesVacias.filter((h) => h.dia === diaDe(fecha));
  const cuantasClases = (fecha: string) =>
    turnos.filter((t) => t.fecha === fecha).length + vaciasDe(fecha).length;

  return (
    <>
      {/* La vista Día ya muestra un día solo: el mapa ahí sería ruido. */}
      {dias.length > 1 && (
        <div className={s.mapa}>
          {dias.map((fecha) => {
            const cuantas = cuantasClases(fecha);
            const clases = [
              s.mapaDia,
              fecha === diaAbierto ? s.mapaDiaAbierto : '',
              fecha === hoy ? s.mapaDiaHoy : '',
            ].filter(Boolean).join(' ');
            return (
              <button key={fecha} className={clases} onClick={() => onAbrirDia(fecha)}>
                <span className={s.mapaNombre}>{DIAS.find((d) => d.valor === diaDe(fecha))?.corto}</span>
                <span className={s.mapaNumero}>{diaDelMes(fecha)}</span>
                <span className={s.mapaCuenta}>{cuantas > 0 ? cuantas : '—'}</span>
              </button>
            );
          })}
        </div>
      )}

      <div className={`${s.grilla} ${dias.length === 1 ? s.grillaUnDia : ''}`}>
        {dias.map((fecha) => {
          const delDia = turnos.filter((t) => t.fecha === fecha);
          const bloqueosDia = bloqueos.filter((b) => cubreFecha(b, fecha));
          // La clase vacía se repite todas las semanas como cualquier horario: se
          // ubica por su día de la semana, no por una fecha (no tiene turnos).
          const vaciasDia = vaciasDe(fecha);
          const hoyCol = fecha === hoy;
          const vacio = delDia.length === 0 && bloqueosDia.length === 0 && vaciasDia.length === 0;
          const clases = [
            s.columna,
            hoyCol ? s.columnaHoy : '',
            fecha === diaAbierto ? s.columnaAbierta : '',
          ].filter(Boolean).join(' ');
          return (
            <div key={fecha} className={clases}>
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
    </>
  );
}
