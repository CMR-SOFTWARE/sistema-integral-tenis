import { useCallback, useEffect, useState } from 'react';
import { api } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { formatoPlata } from '../alumnos/types';
import AccesoCreadoModal from '../alumnos/AccesoCreadoModal';
import NuevaAcademiaModal, { type AltaClub, type ClubCreado } from './NuevaAcademiaModal';
import PersonasPage from './PersonasPage';
import s from './PlataformaPage.module.css';
import sAlumnos from '../alumnos/AlumnosPage.module.css';

type Tab = 'clubes' | 'usuarios';

interface Metricas {
  totalClubes: number;
  clubesActivos: number;
  clubesPendientes: number;
  clubesSuspendidos: number;
  totalProfes: number;
  /** Todas las personas dadas de alta en los clubes, tengan clase o no. */
  totalUsuarios: number;
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

interface RevisionPendiente {
  id: string;
  juegoPendienteId: string | null;
  juegoDoblesPendienteId: string | null;
  creadoPorNombre: string;
  comentario: string;
  creadoEl: string;
}

/**
 * Panel de PLATAFORMA (solo admin): métricas globales de todos los clubes, gestión
 * (activar/suspender/alta) y el padrón de personas (Bloque 6). Cross-tenant: pega a
 * /api/admin/*.
 */
export default function PlataformaPage() {
  const [tab, setTab] = useState<Tab>('clubes');
  const [metricas, setMetricas] = useState<Metricas | null>(null);
  const [clubes, setClubes] = useState<Club[]>([]);
  const [revisiones, setRevisiones] = useState<RevisionPendiente[]>([]);
  const [cargando, setCargando] = useState(true);
  const [resolviendoId, setResolviendoId] = useState<string | null>(null);
  const [respuestas, setRespuestas] = useState<Record<string, string>>({});
  const [modalNuevo, setModalNuevo] = useState(false);
  const [credenciales, setCredenciales] = useState<{ nombre: string; usuario: string; passwordTemporal: string } | null>(null);
  const confirmar = useConfirmar();

  const cargar = useCallback(() => {
    setCargando(true);
    Promise.all([
      api.get<Metricas>('/admin/metricas').catch(() => null),
      api.get<Club[]>('/admin/clubes').catch(() => [] as Club[]),
      api.get<RevisionPendiente[]>('/revisiones/pendientes').catch(() => [] as RevisionPendiente[]),
    ])
      .then(([m, c, r]) => { setMetricas(m); setClubes(c); setRevisiones(r); })
      .finally(() => setCargando(false));
  }, []);

  useEffect(() => { cargar(); }, [cargar]);

  const resolverRevision = async (id: string) => {
    const respuesta = (respuestas[id] ?? '').trim();
    if (!respuesta) return;
    setResolviendoId(id);
    try {
      await api.post(`/revisiones/${id}/resolver`, { respuesta });
      cargar();
    } finally {
      setResolviendoId(null);
    }
  };

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

  const crearClub = (dto: AltaClub) => api.post<ClubCreado>('/admin/clubes', dto);

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
            <div className={s.statNumero}>{metricas.totalUsuarios}</div>
            <div className={s.statLabel}>Usuarios</div>
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

      {revisiones.length > 0 && (
        <>
          <h2 className={s.seccion}>Revisiones pendientes</h2>
          <div className={`${s.tarjeta} ${s.revisiones}`}>
            {revisiones.map((r) => (
              <div key={r.id} className={s.revisionFila}>
                <div className={s.revisionTexto}>
                  <div className={s.nombre}>{r.creadoPorNombre}</div>
                  <div className={s.sub}>{r.comentario}</div>
                </div>
                <div className={s.revisionAccion}>
                  <input
                    className={s.buscador}
                    placeholder="Respuesta…"
                    value={respuestas[r.id] ?? ''}
                    onChange={(e) => setRespuestas((prev) => ({ ...prev, [r.id]: e.target.value }))}
                  />
                  <button
                    className={s.btnVerde}
                    disabled={resolviendoId === r.id || !(respuestas[r.id] ?? '').trim()}
                    onClick={() => void resolverRevision(r.id)}
                  >
                    {resolviendoId === r.id ? 'Enviando…' : 'Resolver'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </>
      )}

      <div className={s.tabs}>
        <button className={tab === 'clubes' ? s.tabActivo : s.tab} onClick={() => setTab('clubes')}>
          Clubes
        </button>
        <button className={tab === 'usuarios' ? s.tabActivo : s.tab} onClick={() => setTab('usuarios')}>
          Usuarios
        </button>
      </div>

      {tab === 'clubes' && (
        <div>
          <div className={sAlumnos.toolbar}>
            <div className={sAlumnos.spacer} />
            <button className={sAlumnos.btnNuevo} onClick={() => setModalNuevo(true)}>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.5" strokeLinecap="round">
                <path d="M12 5v14M5 12h14" />
              </svg>
              Nueva academia
            </button>
          </div>

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
      )}

      {tab === 'usuarios' && <PersonasPage />}

      {modalNuevo && (
        <NuevaAcademiaModal
          onClose={() => setModalNuevo(false)}
          onCrear={crearClub}
          onCreado={(creado) => {
            cargar();
            setCredenciales({ nombre: creado.club.profesor, usuario: creado.usuario, passwordTemporal: creado.passwordTemporal });
          }}
        />
      )}

      {credenciales && (
        <AccesoCreadoModal
          nombre={credenciales.nombre}
          usuario={credenciales.usuario}
          passwordTemporal={credenciales.passwordTemporal}
          vinculado={false}
          titular={null}
          onClose={() => setCredenciales(null)}
        />
      )}
    </div>
  );
}
