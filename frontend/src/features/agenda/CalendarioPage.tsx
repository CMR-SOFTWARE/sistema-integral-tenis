import { useMemo, useState } from 'react';
import { useSemana } from './hooks';
import TurnoModal from './TurnoModal';
import { aISO, fechaCorta, horaCorta, lunesDe, rangoSemana, sumarDias } from './types';
import type { Turno } from './types';
import { useBloqueos } from '../bloqueos/useBloqueos';
import { cubreFecha, franjaLegible } from '../bloqueos/types';
import s from './CalendarioPage.module.css';

/** Calendario semanal: los turnos CONCRETOS (la semana se genera sola al pedirla). */
export default function CalendarioPage() {
  const [lunes, setLunes] = useState(() => lunesDe(new Date()));
  const { turnos, cargando, error, marcarAsistencia, cancelar } = useSemana(lunes);
  const { bloqueos } = useBloqueos();
  const [abierto, setAbierto] = useState<string | null>(null); // turnoId

  const dias = useMemo(
    () => Array.from({ length: 7 }, (_, i) => sumarDias(lunes, i)),
    [lunes],
  );
  const hoy = useMemo(() => lunesDe(new Date()) === lunes, [lunes]);
  const turnoAbierto: Turno | null = turnos.find((t) => t.id === abierto) ?? null;

  return (
    <div>
      <div className={s.toolbar}>
        <button className={s.nav} onClick={() => setLunes(sumarDias(lunes, -7))}>‹</button>
        <div className={s.rango}>
          Semana del {rangoSemana(lunes)}
          {!hoy && (
            <button className={s.hoy} onClick={() => setLunes(lunesDe(new Date()))}>
              volver a hoy
            </button>
          )}
        </div>
        <button className={s.nav} onClick={() => setLunes(sumarDias(lunes, 7))}>›</button>
      </div>

      <div className={s.leyenda}>
        <span className={s.leyendaItem}><i className={s.puntoProgramado} /> Programado</span>
        <span className={s.leyendaItem}><i className={s.puntoCancelado} /> Cancelado</span>
        <span className={s.leyendaItem}><i className={s.puntoBloqueado} /> Bloqueado</span>
      </div>

      {error && <div className={s.error}>{error} — ¿está corriendo la API?</div>}
      {cargando && <div className={s.vacio}>Cargando la semana…</div>}

      {!cargando && !error && (
        <div className={s.grilla}>
          {dias.map((fecha) => {
            const delDia = turnos.filter((t) => t.fecha === fecha);
            const bloqueosDia = bloqueos.filter((b) => cubreFecha(b, fecha));
            const esHoy = fecha === aISO(new Date());
            return (
              <div key={fecha} className={`${s.columna} ${esHoy ? s.columnaHoy : ''}`}>
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
                      onClick={() => setAbierto(t.id)}
                    >
                      <div className={s.turnoHora}>{horaCorta(t.horaInicio)}</div>
                      <div className={s.turnoTitulo}>{t.titulo}</div>
                      <div className={s.turnoDetalle}>
                        {cancelado
                          ? `Cancelado: ${t.canceladoMotivo}`
                          : `${t.cancha} · ${t.participantes.length} 👤${ausentes > 0 ? ` · ${ausentes} falta${ausentes > 1 ? 's' : ''}` : ''}`}
                      </div>
                    </button>
                  );
                })}
              </div>
            );
          })}
        </div>
      )}

      {!cargando && !error && turnos.length === 0 && (
        <div className={s.vacioCard}>
          No hay turnos esta semana. Los turnos nacen de los <b>Horarios</b> (sidebar):
          creá las plantillas y esta pantalla los genera sola.
        </div>
      )}

      {turnoAbierto && (
        <TurnoModal
          turno={turnoAbierto}
          onClose={() => setAbierto(null)}
          onAsistencia={marcarAsistencia}
          onCancelar={cancelar}
        />
      )}
    </div>
  );
}
