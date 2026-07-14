import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../../lib/api';
import { CAT_COLOR, CAT_LABEL, formatoPlata } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import s from './ReportesPage.module.css';

interface MesRecaudacion {
  anio: number;
  mes: number;
  total: number;
}

interface CategoriaConteo {
  categoria: Categoria;
  cantidad: number;
}

interface Reportes {
  recaudacionMensual: MesRecaudacion[];
  porCategoria: CategoriaConteo[];
}

const MES_CORTO = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

/** Reportes (mockup): recaudación últimos 6 meses (cargos pagados, por mes
 *  del cargo — misma definición que el dashboard) + ranking por categoría. */
export default function ReportesPage() {
  const [reportes, setReportes] = useState<Reportes | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<Reportes>('/reportes')
      .then(setReportes)
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando reportes'));
  }, []);

  if (error) return <div className={s.error}>{error} — ¿está corriendo la API?</div>;
  if (!reportes) return <div className={s.cargando}>Cargando…</div>;

  const maxMes = Math.max(1, ...reportes.recaudacionMensual.map((m) => m.total));
  const maxCategoria = Math.max(1, ...reportes.porCategoria.map((c) => c.cantidad));

  return (
    <div className={s.grilla}>
      {/* ── Recaudación últimos 6 meses (barras CSS, sin librerías) ── */}
      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Recaudación últimos 6 meses</h3>
        <div className={s.grafico}>
          {reportes.recaudacionMensual.map((m) => (
            <div key={`${m.anio}-${m.mes}`} className={s.columnaMes}>
              <div className={s.valor}>
                {m.total > 0 ? formatoPlata(m.total) : '—'}
              </div>
              <div className={s.barraZona}>
                <div
                  className={s.barraMes}
                  style={{ height: `${Math.round((m.total / maxMes) * 100)}%` }}
                />
              </div>
              <div className={s.mesLabel}>{MES_CORTO[m.mes - 1]}</div>
            </div>
          ))}
        </div>
        <div className={s.pie}>Cuotas confirmadas, por mes del cargo.</div>
      </div>

      {/* ── Ranking por categoría (mismos datos que el dashboard) ── */}
      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Alumnos por categoría</h3>
        <div className={s.ranking}>
          {reportes.porCategoria.map((c) => {
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
  );
}
