import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { obtenerSesion } from '../auth/sesion';
import { aISO, DIAS, fechaCorta, horaCorta, lunesDe } from '../agenda/types';
import { useSemana } from '../agenda/hooks';
import { useAlumnos } from '../alumnos/useAlumnos';
import { useMiSueldo } from '../sueldos/useMiSueldo';
import { formatoPlata } from '../alumnos/types';
import { MESES } from '../cuotas/types';
import AccesosRapidos from '../dashboard/AccesosRapidos';
import type { Acceso } from '../dashboard/AccesosRapidos';
import CrearAlumnoRapido from '../dashboard/CrearAlumnoRapido';
import Contador from '../../components/tenis/Contador';
import EscenaRally from '../../components/tenis/EscenaRally';
import FranjaTenis from '../../components/tenis/FranjaTenis';
import PelotaTrayectoria from '../../components/tenis/PelotaTrayectoria';
import Seccion from '../../components/tenis/Seccion';
import s from './StaffDashboardPage.module.css';

const DIA_LABEL: Record<string, string> = Object.fromEntries(DIAS.map((d) => [d.valor, d.corto]));

/** Accesos del profe empleado: crear/ver lo suyo (el alta rápida queda a su nombre). */
const ACCESOS: Acceso[] = [
  { to: '/agenda?tab=calendario&nuevo=1', label: 'Nuevo horario', color: '#178a4c', icon: 'M12 5v14M5 12h14' },
  { to: '/agenda?tab=calendario', label: 'Mi agenda', color: '#0e6b3c', icon: 'M3 5h18v16H3zM3 9h18M8 3v4M16 3v4' },
  { to: '/alumnos', label: 'Mis alumnos', color: '#386641', icon: 'M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75' },
];

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
  const miSueldo = useMiSueldo();
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
    <div>
      {/* ── Accesos directos + alta rápida ── */}
      <Seccion>
        <FranjaTenis modo="cancha" />
        <AccesosRapidos accesos={ACCESOS} />
        <CrearAlumnoRapido />
      </Seccion>

    <div className={s.grilla}>
      {/* ── Hero: tu próxima clase (mismo look que el Inicio del alumno) ── */}
      <div className={s.hero}>
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
        <PelotaTrayectoria variante="bote" className={s.heroLinea} />
      </div>

      {/* ── Métricas de la semana ── */}
      <Seccion className={s.tarjeta}>
        <FranjaTenis modo="lateral" />
        <h3 className={s.tarjetaTitulo}>Tu semana</h3>
        <div className={s.metricas}>
          <div className={s.metrica}>
            <div className={s.metricaNumero}><Contador valor={programados.length} /></div>
            <div className={s.metricaLabel}>clases a dar</div>
          </div>
          <div className={s.metrica}>
            <div className={s.metricaNumero}>{horasSemana.toLocaleString('es-AR', { maximumFractionDigits: 1 })}</div>
            <div className={s.metricaLabel}>horas</div>
          </div>
          <div className={s.metrica}>
            <div className={s.metricaNumero}><Contador valor={alumnos.length} /></div>
            <div className={s.metricaLabel}>alumnos</div>
          </div>
        </div>
        <Link to="/agenda?tab=calendario" className={s.btnPrimario}>Ver mi calendario</Link>
      </Seccion>

      {/* ── Mi sueldo del mes (horas dadas × valor hora) ── */}
      <Seccion className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Mi sueldo — {MESES[miSueldo.mes - 1]}</h3>
        {miSueldo.cargando ? (
          <div className={s.vacio}>Calculando…</div>
        ) : miSueldo.error ? (
          <div className={s.vacio}>{miSueldo.error}</div>
        ) : (
          <>
            {miSueldo.sueldo && !miSueldo.sueldo.tieneValorHora && (
              <div style={{ background: '#fef6e7', border: '1px solid #f3dfb6', color: '#b7791f', borderRadius: 10, padding: '10px 12px', fontSize: 13, marginBottom: 12 }}>
                Tu valor hora todavía no está definido — lo carga el director.
              </div>
            )}
            <div className={s.metricas}>
              <div className={s.metrica}>
                <div className={s.metricaNumero}>
                  {(miSueldo.sueldo?.horasTotales ?? 0).toLocaleString('es-AR', { maximumFractionDigits: 1 })}
                </div>
                <div className={s.metricaLabel}>horas este mes</div>
              </div>
              <div className={s.metrica}>
                <div className={s.metricaNumero}>{formatoPlata(miSueldo.sueldo?.calculado ?? 0)}</div>
                <div className={s.metricaLabel}>a cobrar</div>
              </div>
            </div>
            <div style={{ marginTop: 4, marginBottom: (miSueldo.sueldo?.detalle.length ?? 0) > 0 ? 12 : 0 }}>
              {miSueldo.sueldo?.estado === 'Pagado' ? (
                <span style={{ background: '#e7f6ec', color: '#0e6b3c', fontWeight: 600, fontSize: 13, padding: '5px 12px', borderRadius: 8 }}>
                  ✓ Pagado{miSueldo.sueldo.medioPago ? ` · ${miSueldo.sueldo.medioPago}` : ''}
                </span>
              ) : (
                <span style={{ background: '#fef6e7', color: '#b7791f', fontWeight: 700, fontSize: 13, padding: '5px 12px', borderRadius: 8 }}>
                  Pendiente de pago
                </span>
              )}
            </div>
            {miSueldo.sueldo && miSueldo.sueldo.detalle.length > 0 && (
              <div className={s.lista}>
                {miSueldo.sueldo.detalle.map((d) => (
                  <div key={d.horarioId} className={s.fila}>
                    <div className={s.filaDia}>
                      <div className={s.filaFecha}>{DIA_LABEL[d.dia] ?? d.dia}</div>
                      <div className={s.filaHora}>{horaCorta(d.horaInicio)}</div>
                    </div>
                    <div className={s.filaInfo}>
                      <div className={s.filaTitulo}>{d.titulo}</div>
                      <div className={s.filaLugar}>
                        {d.clases} clase{d.clases === 1 ? '' : 's'} · {d.horas} h · {formatoPlata(d.subtotal)}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </>
        )}
      </Seccion>

      {/* ── Próximas clases ── */}
      <Seccion className={s.tarjeta}>
        <FranjaTenis modo="bote" />
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
      </Seccion>
    </div>
      <EscenaRally />
    </div>
  );
}
