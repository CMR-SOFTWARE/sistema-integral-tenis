import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../../lib/api';
import { CAT_COLOR, CAT_LABEL, formatoPlata } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import { fechaCorta, horaCorta } from '../agenda/types';
import s from './DashboardPage.module.css';

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
  participantes: number;
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
  const [resumen, setResumen] = useState<Resumen | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pedidosPend, setPedidosPend] = useState(0);

  useEffect(() => {
    api.get<Resumen>('/dashboard')
      .then(setResumen)
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando el dashboard'));
    api.get<number>('/pedidos/pendientes/cuenta').then(setPedidosPend).catch(() => setPedidosPend(0));
  }, []);

  if (error) {
    return <div className={s.error}>{error} — ¿está corriendo la API? (dotnet run)</div>;
  }
  if (!resumen) {
    return <div className={s.cargando}>Cargando…</div>;
  }

  const maxCategoria = Math.max(1, ...resumen.porCategoria.map((c) => c.cantidad));
  const { cuotasPendientes: cuotas } = resumen;

  const metricas = [
    {
      label: 'Alumnos activos',
      valor: String(resumen.alumnosActivos),
      iconBg: '#e7f6ec',
      iconColor: '#0e6b3c',
      icono: 'M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75',
    },
    {
      label: 'Nuevos este mes',
      valor: String(resumen.nuevosEsteMes),
      iconBg: '#eef2fe',
      iconColor: '#2563eb',
      icono: 'M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2M8.5 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM20 8v6M23 11h-6',
    },
    {
      label: 'Pausados',
      valor: String(resumen.pausados),
      iconBg: '#fef6e7',
      iconColor: '#b7791f',
      icono: 'M10 4H6v16h4zM18 4h-4v16h4z',
    },
    {
      label: 'Recaudación del mes',
      valor: formatoPlata(resumen.recaudacionDelMes),
      iconBg: '#f3eefe',
      iconColor: '#7c3aed',
      icono: 'M12 1v22M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6',
    },
  ];

  return (
    <div>
      {pedidosPend > 0 && (
        <Link to="/cuotas" className={s.avisoPedidos}>
          <span className={s.avisoPedidosBadge}>{pedidosPend}</span>
          {pedidosPend === 1 ? 'pedido de servicio sin resolver' : 'pedidos de servicios sin resolver'} — resolvé en Cuotas
        </Link>
      )}

      {/* ── Métricas (datos reales) ── */}
      <div className={s.metricas}>
        {metricas.map((m) => (
          <div key={m.label} className={s.metrica}>
            <div className={s.metricaIcono} style={{ background: m.iconBg }}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke={m.iconColor} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d={m.icono} />
              </svg>
            </div>
            <div className={s.metricaValor}>{m.valor}</div>
            <div className={s.metricaLabel}>{m.label}</div>
          </div>
        ))}
      </div>

      <div className={s.filaPrincipal}>
        {/* ── Clases de hoy (datos reales) ── */}
        <div className={s.tarjeta}>
          <div className={s.tarjetaHeader}>
            <h3 className={s.tarjetaTitulo}>Próximas clases de hoy</h3>
            <Link to="/calendario" className={s.linkReal}>Ver calendario →</Link>
          </div>
          {resumen.clasesHoy.length === 0 ? (
            <div className={s.vacio}>Hoy no hay clases programadas.</div>
          ) : (
            <div className={s.lista}>
              {resumen.clasesHoy.map((c) => (
                <div key={c.turnoId} className={c.estado === 'Cancelado' ? s.filaCancelada : s.fila}>
                  <span className={s.filaHora}>{horaCorta(c.horaInicio)}</span>
                  <div className={s.filaCuerpo}>
                    <div className={s.filaTitulo}>{c.titulo}</div>
                    <div className={s.filaMeta}>
                      {c.cancha} · {c.participantes} 👤 · {c.duracionMinutos}'
                      {c.estado === 'Cancelado' && ' · Cancelada'}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* ── Ranking por categoría (datos reales) ── */}
        <div className={s.tarjeta}>
          <h3 className={s.tarjetaTitulo}>Alumnos por categoría</h3>
          <div className={s.ranking}>
            {resumen.porCategoria.map((c) => {
              const color = CAT_COLOR[c.categoria];
              return (
                <div key={c.categoria} className={s.rankingFila}>
                  <span className={s.chip} style={{ background: `${color}1a`, color }}>
                    {CAT_LABEL[c.categoria]}
                  </span>
                  <div className={s.barraFondo}>
                    <div
                      className={s.barra}
                      style={{ width: `${Math.round((c.cantidad / maxCategoria) * 100)}%`, background: color }}
                    />
                  </div>
                  <span className={s.rankingNum}>{c.cantidad}</span>
                </div>
              );
            })}
          </div>
          <Link to="/alumnos" className={s.linkReal}>Ver alumnos →</Link>
        </div>
      </div>

      <div className={s.filaSecundaria}>
        {/* ── Cuotas del mes (cargos ya generados; la liquidación vive en Cuotas) ── */}
        <div className={s.tarjeta}>
          <div className={s.tarjetaHeader}>
            <h3 className={s.tarjetaTitulo}>Cuotas pendientes</h3>
            <Link to="/cuotas" className={s.linkReal}>Ver cuotas →</Link>
          </div>
          {cuotas.alumnosPendientes === 0 && cuotas.alumnosVencidos === 0 ? (
            <div className={s.vacio}>Nadie debe nada este mes. 🎾</div>
          ) : (
            <div className={s.statsCuotas}>
              <div className={s.statCuota}>
                <div className={s.statValor}>{cuotas.alumnosPendientes}</div>
                <div className={s.statLabel}>Pendientes</div>
              </div>
              <div className={s.statCuota}>
                <div className={`${s.statValor} ${s.statVencida}`}>{cuotas.alumnosVencidos}</div>
                <div className={s.statLabel}>Vencidas</div>
              </div>
              <div className={s.statCuota}>
                <div className={s.statValor}>{formatoPlata(cuotas.totalPendiente)}</div>
                <div className={s.statLabel}>Por cobrar</div>
              </div>
            </div>
          )}
        </div>

        {/* ── Cancelaciones recientes (turnos enteros + avisos de alumnos) ── */}
        <div className={s.tarjeta}>
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
        </div>
      </div>
    </div>
  );
}
