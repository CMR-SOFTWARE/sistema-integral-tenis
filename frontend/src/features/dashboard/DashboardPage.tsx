import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { api } from '../../lib/api';
import { CAT_LABEL } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import { fechaCorta, horaCorta } from '../agenda/types';
import { useProfesores } from '../profesores/useProfesores';
import AccesosRapidos from './AccesosRapidos';
import type { Acceso } from './AccesosRapidos';
import CrearAlumnoRapido from './CrearAlumnoRapido';
import Contador from '../../components/tenis/Contador';
import CanchaTejo from '../../components/tenis/CanchaTejo';
import EscenaRally from '../../components/tenis/EscenaRally';
import FranjaTenis from '../../components/tenis/FranjaTenis';
import Seccion from '../../components/tenis/Seccion';
import s from './DashboardPage.module.css';

/** Accesos directos del dueño: llevan a la acción con un toque (algunos abren el modal). */
const ACCESOS: Acceso[] = [
  { to: '/agenda?tab=calendario&nuevo=1', label: 'Nuevo horario', color: '#178a4c', icon: 'M12 5v14M5 12h14' },
  { to: '/finanzas?tab=cuotas', label: 'Cobrar cuotas', color: '#0e6b3c', icon: 'M12 1v22M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6' },
  { to: '/agenda?tab=calendario', label: 'Ver agenda', color: '#6f8f7b', icon: 'M3 5h18v16H3zM3 9h18M8 3v4M16 3v4' },
  { to: '/alumnos', label: 'Ver alumnos', color: '#386641', icon: 'M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75' },
];

interface CategoriaConteo {
  categoria: Categoria;
  cantidad: number;
}

interface ClaseHoy {
  turnoId: string;
  horaInicio: string;
  duracionMinutos: number;
  titulo: string;
  cancha: string;
  sede: string;
  profesorUserId: string | null;
  participantes: number;
  alumnos: string[];
  estado: 'Programado' | 'Cancelado';
}

interface CuotasPendientes {
  alumnosPendientes: number;
  alumnosVencidos: number;
  totalPendiente: number;
}

interface CancelacionReciente {
  fecha: string;
  horaInicio: string;
  titulo: string;
  motivo: string | null;
  por: 'Profesor' | 'Alumno';
  alumnoNombre: string | null;
  canceladoEl: string;
}

interface Resumen {
  alumnosActivos: number;
  nuevosEsteMes: number;
  pausados: number;
  recaudacionDelMes: number;
  porCategoria: CategoriaConteo[];
  clasesHoy: ClaseHoy[];
  cuotasPendientes: CuotasPendientes;
  cancelacionesRecientes: CancelacionReciente[];
}

/** Dashboard del profesor: métricas, clases de hoy, cuotas y cancelaciones,
 *  todo con datos REALES del tenant. */
export default function DashboardPage() {
  const resumenQuery = useQuery({
    queryKey: ['dashboard'],
    queryFn: () => api.get<Resumen>('/dashboard'),
  });
  const { data: pedidosPend = 0 } = useQuery({
    queryKey: ['pedidos-pendientes-cuenta'],
    queryFn: () => api.get<number>('/pedidos/pendientes/cuenta'),
  });
  const { nombreDe } = useProfesores();

  if (resumenQuery.error) {
    const msg = resumenQuery.error.message || 'Error cargando el dashboard';
    return <div className={s.error}>{msg}</div>;
  }
  const resumen = resumenQuery.data;
  if (!resumen) {
    return <div className={s.cargando}>Cargando…</div>;
  }

  const { cuotasPendientes: cuotas } = resumen;

  const maxCat = Math.max(1, ...resumen.porCategoria.map((c) => c.cantidad));

  return (
    <div>
      {pedidosPend > 0 && (
        <Link to="/mi-academia?tab=pedidos" className={s.avisoPedidos}>
          <span className={s.avisoPedidosBadge}>{pedidosPend}</span>
          {pedidosPend === 1 ? 'pedido del Shop sin resolver' : 'pedidos del Shop sin resolver'} — resolvelos en Mi academia
        </Link>
      )}

      <section className={s.hero}>
        <div className={s.heroContenido}>
        <div className={s.heroEyebrow}>Centro de cancha</div>
        <h2 className={s.heroTitulo}>Hoy en tu academia</h2>
        <CanchaTejo />
        <div className={s.heroMetricas}>
          <div className={s.heroMetrica}>
            <div className={s.heroNum}><Contador valor={resumen.alumnosActivos} /></div>
            <div className={s.heroLabel}>Activos</div>
          </div>
          <div className={s.heroMetrica}>
            <div className={s.heroNum}><Contador valor={resumen.nuevosEsteMes} /></div>
            <div className={s.heroLabel}>Nuevos</div>
          </div>
          <div className={s.heroMetrica}>
            <div className={s.heroNum}><Contador valor={resumen.pausados} /></div>
            <div className={s.heroLabel}>Pausados</div>
          </div>
          <div className={s.heroMetrica}>
            <div className={s.heroNum}><Contador valor={resumen.recaudacionDelMes} prefijo="$" /></div>
            <div className={s.heroLabel}>Este mes</div>
          </div>
        </div>
        {resumen.porCategoria.length > 0 && (
          <Seccion className={s.ranking}>
            {resumen.porCategoria.slice(0, 6).map((c) => (
              <div key={c.categoria} className={s.rankingFila}>
                <span className={s.chip}>{CAT_LABEL[c.categoria]}</span>
                <div className={s.barraFondo}>
                  <div className={s.barra} style={{ width: `${(c.cantidad / maxCat) * 100}%` }} />
                </div>
                <span className={s.rankingNum}>{c.cantidad}</span>
              </div>
            ))}
          </Seccion>
        )}
        </div>
      </section>

      <FranjaTenis modo="cancha" />

      <Seccion className={s.sectorSage}>
        <FranjaTenis modo="golpe" />
        <AccesosRapidos accesos={ACCESOS} />
        <CrearAlumnoRapido />
      </Seccion>

      <Seccion className={`${s.tarjeta} ${s.sectorSand} ${s.tarjetaSola}`}>
        <FranjaTenis modo="lateral" />
        <div className={s.tarjetaHeader}>
          <h3 className={s.tarjetaTitulo}>Marcador de hoy</h3>
          <Link to="/agenda?tab=calendario" className={s.linkReal}>Ver calendario →</Link>
        </div>
        {resumen.clasesHoy.length === 0 ? (
          <div className={s.vacio}>Hoy no hay clases programadas.</div>
        ) : (
          <div className={s.lista}>
            {resumen.clasesHoy.map((c) => {
              const profe = nombreDe(c.profesorUserId);
              return (
                <Link
                  key={c.turnoId}
                  to={`/agenda?tab=calendario&turno=${c.turnoId}`}
                  className={`${c.estado === 'Cancelado' ? `${s.match} ${s.matchOff}` : s.match} ${s.filaClases}`}
                >
                  <span className={s.matchHora}>{horaCorta(c.horaInicio)}</span>
                  <div className={s.filaCuerpo}>
                    <div className={s.filaTitulo}>{c.titulo}</div>
                    <div className={s.filaMeta}>
                      {c.sede} · {c.cancha} · {c.participantes} jugador{c.participantes === 1 ? '' : 'es'}
                    </div>
                    {(profe || c.alumnos.length > 0) && (
                      <div className={s.filaMeta}>
                        {profe && `Profe: ${profe}`}
                        {profe && c.alumnos.length > 0 && ' · '}
                        {c.alumnos.join(', ')}
                      </div>
                    )}
                  </div>
                  <div className={s.matchScore}>
                    {c.estado === 'Programado' && (
                      <span className={s.live}><i className={s.liveDot} />LIVE</span>
                    )}
                    <span>{c.duracionMinutos}'</span>
                    {c.estado === 'Cancelado' && <span className={s.matchBadgeOff}>OFF</span>}
                  </div>
                </Link>
              );
            })}
          </div>
        )}
      </Seccion>

      <div className={s.filaSecundaria}>
        {/* ── Cuotas del mes (cargos ya generados; la liquidación vive en Cuotas) ── */}
        <Seccion className={`${s.tarjeta} ${s.sectorDeep}`}>
          <FranjaTenis modo="globo" />
          <div className={s.tarjetaHeader}>
            <h3 className={s.tarjetaTitulo}>Cuotas pendientes</h3>
            <Link to="/finanzas?tab=cuotas" className={s.linkReal}>Ver cuotas →</Link>
          </div>
          {cuotas.alumnosPendientes === 0 && cuotas.alumnosVencidos === 0 ? (
            <div className={s.vacio}>Nadie debe nada este mes.</div>
          ) : (
            <div className={s.statsCuotas}>
              <div className={s.statCuota}>
                <div className={s.statValor}><Contador valor={cuotas.alumnosPendientes} /></div>
                <div className={s.statLabel}>Pendientes</div>
              </div>
              <div className={s.statCuota}>
                <div className={`${s.statValor} ${s.statVencida}`}><Contador valor={cuotas.alumnosVencidos} /></div>
                <div className={s.statLabel}>Vencidas</div>
              </div>
              <div className={s.statCuota}>
                <div className={s.statValor}><Contador valor={cuotas.totalPendiente} prefijo="$" /></div>
                <div className={s.statLabel}>Por cobrar</div>
              </div>
            </div>
          )}
        </Seccion>

        {/* ── Cancelaciones recientes (turnos enteros + avisos de alumnos) ── */}
        <Seccion className={s.tarjeta}>
          <FranjaTenis modo="slice" />
          <div className={s.tarjetaHeader}>
            <h3 className={s.tarjetaTitulo}>Cancelaciones recientes</h3>
            <Link to="/cancelaciones" className={s.linkReal}>Ver todas →</Link>
          </div>
          {resumen.cancelacionesRecientes.length === 0 ? (
            <div className={s.vacio}>Sin cancelaciones recientes.</div>
          ) : (
            <div className={s.lista}>
              {resumen.cancelacionesRecientes.map((c) => (
                <div key={c.canceladoEl} className={s.fila}>
                  <span className={s.filaHora}>{fechaCorta(c.fecha)}</span>
                  <div className={s.filaCuerpo}>
                    <div className={s.filaTitulo}>
                      {c.alumnoNombre ?? c.titulo} · {horaCorta(c.horaInicio)}
                      <span className={c.por === 'Alumno' ? s.chipPorAlumno : s.chipPorProfe}>
                        {c.por === 'Alumno' ? 'alumno' : 'profe'}
                      </span>
                    </div>
                    {c.motivo && <div className={s.filaMeta}>{c.motivo}</div>}
                  </div>
                </div>
              ))}
            </div>
          )}
        </Seccion>
      </div>

      <EscenaRally />
    </div>
  );
}
