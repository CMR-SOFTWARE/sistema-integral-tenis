import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import { datosCompletos, guardarSesion, obtenerSesion } from '../auth/sesion';
import type { Sesion } from '../auth/sesion';
import type { MiSolicitud } from '../solicitudes/types';
import s from './PortalPages.module.css';

interface ProfesorPublico {
  tenantId: string;
  club: string;
  profesor: string;
}

const ESTADO_SOL_UI: Record<MiSolicitud['estado'], { label: string; clase: 'chipAmbar' | 'chipVerde' | 'chipRojo' }> = {
  Pendiente: { label: 'Pendiente', clase: 'chipAmbar' },
  Aprobada: { label: 'Aprobada', clase: 'chipVerde' },
  Rechazada: { label: 'Rechazada', clase: 'chipRojo' },
};

/**
 * Mi club (plan v2): buscá a tu profesor por nombre o club y mandale una
 * solicitud; cuando la apruebe, tu ficha nace en su club y el portal se
 * habilita completo.
 */
export default function BuscarClubPage() {
  const sesion = obtenerSesion();
  const [buscar, setBuscar] = useState('');
  const [profes, setProfes] = useState<ProfesorPublico[]>([]);
  const [solicitudes, setSolicitudes] = useState<MiSolicitud[]>([]);
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState<string | null>(null); // tenantId en curso
  const [error, setError] = useState<string | null>(null);

  const cargarSolicitudes = useCallback(() => {
    api.get<MiSolicitud[]>('/portal/solicitudes').then(setSolicitudes).catch(() => {});
  }, []);

  useEffect(() => {
    cargarSolicitudes();
  }, [cargarSolicitudes]);

  // Buscador con debounce simple
  useEffect(() => {
    const timer = setTimeout(() => {
      const q = buscar.trim() === '' ? '' : `?buscar=${encodeURIComponent(buscar.trim())}`;
      api.get<ProfesorPublico[]>(`/auth/profesores${q}`).then(setProfes).catch(() => setProfes([]));
    }, 300);
    return () => clearTimeout(timer);
  }, [buscar]);

  const solicitar = async (p: ProfesorPublico) => {
    setError(null);
    setEnviando(p.tenantId);
    try {
      const lista = await api.post<MiSolicitud[]>('/portal/solicitudes', {
        tenantId: p.tenantId,
        mensaje: mensaje.trim() || undefined,
      });
      setSolicitudes(lista);
      setMensaje('');
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo enviar la solicitud.');
    } finally {
      setEnviando(null);
    }
  };

  // Si el profe aprobó, el refresh de sesión del layout ya trae la ficha;
  // acá refrescamos por las dudas al montar
  useEffect(() => {
    api.get<Sesion>('/auth/yo').then(guardarSesion).catch(() => {});
  }, []);

  if (sesion?.alumno) {
    return (
      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Tu club</h3>
        <p className={s.sinClubTexto}>
          Estás vinculado a <b>{sesion.alumno.club}</b> como {sesion.alumno.nombre} {sesion.alumno.apellido}.
        </p>
      </div>
    );
  }

  const pendiente = solicitudes.find((x) => x.estado === 'Pendiente');

  return (
    <div className={s.perfilCol}>
      {/* ── Mis solicitudes ── */}
      {solicitudes.length > 0 && (
        <div className={s.tarjeta}>
          <h3 className={s.tarjetaTitulo}>Mis solicitudes</h3>
          <div className={s.horariosLista}>
            {solicitudes.map((sol) => {
              const ui = ESTADO_SOL_UI[sol.estado];
              return (
                <div key={sol.id} className={s.horarioFila}>
                  <div className={s.horarioInfo}>
                    <div className={s.horarioTitulo}>{sol.club}</div>
                    <div className={s.horarioDetalle}>
                      Enviada el {new Date(sol.creadoEl).toLocaleDateString('es-AR')}
                    </div>
                  </div>
                  <span className={`${s.chip} ${s[ui.clase]}`}>{ui.label}</span>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* ── Buscar profesor ── */}
      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Buscar a tu profesor</h3>
        {!datosCompletos(sesion) ? (
          <>
            <p className={s.sinClubTexto}>
              Antes de solicitar necesitamos tu DNI, teléfono y fecha de
              nacimiento (con ellos se arma tu ficha en el club).
            </p>
            <Link to="/portal/perfil" className={s.btnPrimario}>Completar mis datos</Link>
          </>
        ) : pendiente ? (
          <p className={s.sinClubTexto}>
            Tenés una solicitud pendiente en <b>{pendiente.club}</b>. Cuando tu
            profe la apruebe, tu portal se habilita solo.
          </p>
        ) : (
          <>
            <div className={s.perfilEdicion}>
              <label className={s.perfilCampo}>
                <span>Nombre del profe o del club</span>
                <input
                  value={buscar}
                  onChange={(e) => setBuscar(e.target.value)}
                  placeholder="Ej: Academia Río Cuarto"
                  autoFocus
                />
              </label>
              <label className={s.perfilCampo}>
                <span>Mensaje para el profe (opcional)</span>
                <input
                  value={mensaje}
                  onChange={(e) => setMensaje(e.target.value)}
                  placeholder="Juego los martes a la tarde…"
                  maxLength={200}
                />
              </label>
            </div>

            {error && <div className={s.error}>{error}</div>}

            {profes.length === 0 ? (
              <p className={s.sinClubTexto}>
                {buscar.trim() === ''
                  ? 'Escribí el nombre de tu profe o de su club para buscarlo.'
                  : 'No encontramos clubes con ese nombre.'}
              </p>
            ) : (
              <div className={s.horariosLista}>
                {profes.map((p) => (
                  <div key={p.tenantId} className={s.horarioFila}>
                    <div className={s.horarioInfo}>
                      <div className={s.horarioTitulo}>{p.club}</div>
                      <div className={s.horarioDetalle}>Prof. {p.profesor}</div>
                    </div>
                    <button
                      className={s.btnGuardar}
                      disabled={enviando !== null}
                      onClick={() => void solicitar(p)}
                    >
                      {enviando === p.tenantId ? 'Enviando…' : 'Solicitar'}
                    </button>
                  </div>
                ))}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
