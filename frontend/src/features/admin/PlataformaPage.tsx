import { useCallback, useEffect, useState } from 'react';
import { api } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { formatoPlata } from '../alumnos/types';
import s from './PlataformaPage.module.css';

interface Metricas {
  totalClubes: number;
  clubesActivos: number;
  clubesPendientes: number;
  clubesSuspendidos: number;
  totalProfes: number;
  totalAlumnos: number;
  ingresosMes: number;
  clubesNuevos30d: number;
  alumnosNuevos30d: number;
}

interface Club {
  id: string;
  nombre: string;
  subdominio: string;
  estado: 'Activo' | 'PendientePago' | 'Suspendido';
  profesor: string;
  alumnos: number;
  creadoEl: string;
}

const ESTADO_UI: Record<Club['estado'], { label: string; cls: string }> = {
  Activo: { label: 'Activo', cls: 'chipVerde' },
  PendientePago: { label: 'Pendiente de pago', cls: 'chipAmarillo' },
  Suspendido: { label: 'Suspendido', cls: 'chipRojo' },
};

/**
 * Panel de PLATAFORMA (solo admin): métricas globales de todos los clubes +
 * gestión (activar/suspender). Cross-tenant: pega a /api/admin/*.
 */
export default function PlataformaPage() {
  const [metricas, setMetricas] = useState<Metricas | null>(null);
  const [clubes, setClubes] = useState<Club[]>([]);
  const [cargando, setCargando] = useState(true);
  const confirmar = useConfirmar();

  const cargar = useCallback(() => {
    setCargando(true);
    Promise.all([
      api.get<Metricas>('/admin/metricas').catch(() => null),
      api.get<Club[]>('/admin/clubes').catch(() => [] as Club[]),
    ])
      .then(([m, c]) => { setMetricas(m); setClubes(c); })
      .finally(() => setCargando(false));
  }, []);

  useEffect(() => { cargar(); }, [cargar]);

  const cambiarEstado = async (club: Club, estado: 'Activo' | 'Suspendido') => {
    if (estado === 'Suspendido' && !(await confirmar({
      titulo: `Suspender ${club.nombre}`,
      mensaje: 'El club deja de funcionar (sus profes y alumnos no van a poder entrar hasta que lo reactives).',
      confirmar: 'Suspender',
      peligro: true,
    }))) return;
    await api.patch(`/admin/clubes/${club.id}/estado`, { estado });
    cargar();
  };

  if (cargando) return <div className={s.vacio}>Cargando la plataforma…</div>;

  return (
    <div>
      {metricas && (
        <div className={s.metricas}>
          <div className={s.stat}>
            <div className={s.statNumero}>{metricas.totalClubes}</div>
            <div className={s.statLabel}>Clubes</div>
            <div className={s.statDetalle}>
              {metricas.clubesActivos} activos · {metricas.clubesPendientes} pend. · {metricas.clubesSuspendidos} susp.
            </div>
          </div>
          <div className={s.stat}>
            <div className={s.statNumero}>{metricas.totalProfes}</div>
            <div className={s.statLabel}>Profesores</div>
            <div className={s.statDetalle}>dueños + empleados</div>
          </div>
          <div className={s.stat}>
            <div className={s.statNumero}>{metricas.totalAlumnos}</div>
            <div className={s.statLabel}>Alumnos activos</div>
            <div className={s.statDetalle}>en toda la plataforma</div>
          </div>
          <div className={`${s.stat} ${s.statPlata}`}>
            <div className={s.statNumero}>{formatoPlata(metricas.ingresosMes)}</div>
            <div className={s.statLabel}>Ingresos del mes</div>
            <div className={s.statDetalle}>pagos confirmados</div>
          </div>
          <div className={s.stat}>
            <div className={s.statNumero}>+{metricas.clubesNuevos30d}</div>
            <div className={s.statLabel}>Clubes nuevos</div>
            <div className={s.statDetalle}>últimos 30 días</div>
          </div>
          <div className={s.stat}>
            <div className={s.statNumero}>+{metricas.alumnosNuevos30d}</div>
            <div className={s.statLabel}>Alumnos nuevos</div>
            <div className={s.statDetalle}>últimos 30 días</div>
          </div>
        </div>
      )}

      <h2 className={s.seccion}>Clubes</h2>
      <div className={s.tarjeta}>
        <div className={s.tablaWrap}>
        <table className={s.tabla}>
          <thead>
            <tr>
              <th>Club</th>
              <th>Profesor</th>
              <th>Alumnos</th>
              <th>Estado</th>
              <th className={s.thAcciones}>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {clubes.map((c) => (
              <tr key={c.id}>
                <td>
                  <div className={s.nombre}>{c.nombre}</div>
                  <div className={s.sub}>{c.subdominio}</div>
                </td>
                <td>{c.profesor}</td>
                <td>{c.alumnos}</td>
                <td>
                  <span className={`${s.chip} ${s[ESTADO_UI[c.estado].cls]}`}>
                    {ESTADO_UI[c.estado].label}
                  </span>
                </td>
                <td>
                  <div className={s.acciones}>
                    {c.estado === 'Activo' ? (
                      <button className={s.btnRojo} onClick={() => void cambiarEstado(c, 'Suspendido')}>
                        Suspender
                      </button>
                    ) : (
                      <button className={s.btnVerde} onClick={() => void cambiarEstado(c, 'Activo')}>
                        Activar
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
            {clubes.length === 0 && (
              <tr><td colSpan={5} className={s.vacio}>No hay clubes todavía.</td></tr>
            )}
          </tbody>
        </table>
        </div>
      </div>
    </div>
  );
}
