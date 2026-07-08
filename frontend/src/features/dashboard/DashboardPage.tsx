import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../../lib/api';
import { CAT_COLOR, CAT_LABEL, formatoPlata } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import s from './DashboardPage.module.css';

interface CategoriaConteo {
  categoria: Categoria;
  cantidad: number;
}

interface Resumen {
  alumnosActivos: number;
  nuevosEsteMes: number;
  pausados: number;
  ingresoMensualEstimado: number;
  porCategoria: CategoriaConteo[];
}

/** Dashboard del profesor: métricas y ranking con datos REALES del tenant;
 *  clases/cuotas/cancelaciones llegan con sus verticales (placeholders). */
export default function DashboardPage() {
  const [resumen, setResumen] = useState<Resumen | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<Resumen>('/dashboard')
      .then(setResumen)
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando el dashboard'));
  }, []);

  if (error) {
    return <div className={s.error}>{error} — ¿está corriendo la API? (dotnet run)</div>;
  }
  if (!resumen) {
    return <div className={s.cargando}>Cargando…</div>;
  }

  const maxCategoria = Math.max(1, ...resumen.porCategoria.map((c) => c.cantidad));

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
      label: 'Ingreso mensual estimado',
      valor: formatoPlata(resumen.ingresoMensualEstimado),
      iconBg: '#f3eefe',
      iconColor: '#7c3aed',
      icono: 'M12 1v22M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6',
    },
  ];

  return (
    <div>
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
        {/* ── Próximas clases: vertical futura ── */}
        <div className={s.tarjeta}>
          <div className={s.tarjetaHeader}>
            <h3 className={s.tarjetaTitulo}>Próximas clases de hoy</h3>
            <span className={s.linkFuturo}>Ver calendario →</span>
          </div>
          <div className={s.placeholder}>Llega con la vertical de Horarios y Calendario.</div>
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
        <div className={s.tarjeta}>
          <h3 className={s.tarjetaTitulo}>Cuotas pendientes</h3>
          <div className={s.placeholder}>Llega con la vertical de Cuotas.</div>
        </div>
        <div className={s.tarjeta}>
          <h3 className={s.tarjetaTitulo}>Cancelaciones recientes</h3>
          <div className={s.placeholder}>Llega con la vertical de Bloqueos y Cancelaciones.</div>
        </div>
      </div>
    </div>
  );
}
