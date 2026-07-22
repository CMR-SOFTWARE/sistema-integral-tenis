import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { obtenerSesion } from '../auth/sesion';
import { aISO, fechaCorta, horaCorta, lunesDe } from '../agenda/types';
import { useSemana } from '../agenda/hooks';
import { useAlumnos } from '../alumnos/useAlumnos';
import s from './StaffDashboardPage.module.css';

/**
 * Inicio del profe EMPLEADO: un resumen de su semana (clases a dar, horas,
 * sus alumnos) + sus próximas clases. Reusa los hooks del profe (/turnos/semana
 * y /alumnos, ya filtrados a lo suyo) → comparte caché con Calendario y Alumnos.
 */
export default function StaffDashboardPage() {
  const nombre = obtenerSesion()?.nombre ?? '';
  const lunes = useMemo(() => lunesDe(new Date()), []);
  const semana = useSemana(lunes);
  const { alumnos, cargando: cargandoAlumnos } = useAlumnos('todas', 'todos');
  const turnos = semana.turnos;
  const cargando = semana.cargando || cargandoAlumnos;

  const programados = useMemo(() => turnos.filter((t) => t.estado === 'Programado'), [turnos]);

  const horasSemana = useMemo(
    () => programados.reduce((sum, t) => sum + t.duracionMinutos, 0) / 60,
    [programados],
  );

  const proximas = useMemo(() => {
    const hoy = aISO(new Date());
    return programados
      .filter((t) => t.fecha >= hoy)
      .sort((a, b) => (a.fecha + a.horaInicio).localeCompare(b.fecha + b.horaInicio));
  }, [programados]);

  const proxima = proximas[0];

  if (cargando) return <div className={s.vacio}>Cargando tu semana…</div>;

  return (
    <div className={s.grilla}>
      {/* ── Hero: tu próxima clase (mismo look que el Inicio del alumno) ── */}
      <div className={s.hero}>
        <div className={s.heroPelota} />
        <div className={s.heroEyebrow}>Hola, {nombre.split(' ')[0]} — tu próxima clase</div>
        {proxima ? (
          <>
            <div className={s.heroFecha}>{fechaCorta(proxima.fecha)}</div>
            <div className={s.heroDetalle}>
              {horaCorta(proxima.horaInicio)} hs · {proxima.titulo}
            </div>
            <div className={s.heroLugar}>{proxima.sede} · {proxima.cancha}</div>
          </>
        ) : (
          <div className={s.heroDetalle}>No tenés clases programadas esta semana.</div>
        )}
      </div>

      {/* ── Métricas de la semana ── */}
      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Tu semana</h3>
        <div className={s.metricas}>
          <div className={s.metrica}>
            <div className={s.metricaNumero}>{programados.length}</div>
            <div className={s.metricaLabel}>clases a dar</div>
          </div>
          <div className={s.metrica}>
            <div className={s.metricaNumero}>{horasSemana.toLocaleString('es-AR', { maximumFractionDigits: 1 })}</div>
            <div className={s.metricaLabel}>horas</div>
          </div>
          <div className={s.metrica}>
            <div className={s.metricaNumero}>{alumnos.length}</div>
            <div className={s.metricaLabel}>alumnos</div>
          </div>
        </div>
        <Link to="/calendario" className={s.btnPrimario}>Ver mi calendario</Link>
      </div>

      {/* ── Próximas clases ── */}
      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Próximas clases</h3>
        {proximas.length === 0 && <div className={s.vacio}>Sin clases próximas.</div>}
        <div className={s.lista}>
          {proximas.slice(0, 6).map((t) => (
            <div key={t.id} className={s.fila}>
              <div className={s.filaDia}>
                <div className={s.filaFecha}>{fechaCorta(t.fecha)}</div>
                <div className={s.filaHora}>{horaCorta(t.horaInicio)}</div>
              </div>
              <div className={s.filaInfo}>
                <div className={s.filaTitulo}>{t.titulo}</div>
                <div className={s.filaLugar}>
                  {t.sede} · {t.cancha} · {t.participantes.length} alumno{t.participantes.length === 1 ? '' : 's'}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
