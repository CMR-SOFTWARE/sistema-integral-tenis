import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import { guardarSesion, obtenerSesion } from '../auth/sesion';
import type { Sesion } from '../auth/sesion';
import s from './PortalPages.module.css';

/**
 * Mi club. Hoy: si tu profesor ya te cargó (coincidencia por DNI/teléfono),
 * te vinculás con un click. El buscador de profesores con solicitud de
 * ingreso llega en la próxima versión y reemplaza esta pantalla.
 */
export default function BuscarClubPage() {
  const navigate = useNavigate();
  const [sesion, setSesion] = useState<Sesion | null>(obtenerSesion());
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const reclamar = async (alumnoId: string) => {
    setError(null);
    setEnviando(true);
    try {
      const s2 = await api.post<Sesion>('/auth/reclamar', { alumnoId });
      guardarSesion(s2);
      setSesion(s2);
      if (s2.alumno) navigate('/portal');
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo vincular la ficha.');
    } finally {
      setEnviando(false);
    }
  };

  if (!sesion) return null;

  if (sesion.alumno) {
    return (
      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Tu club</h3>
        <p className={s.sinClubTexto}>
          Estás vinculado a <b>{sesion.alumno.club}</b> como {sesion.alumno.nombre} {sesion.alumno.apellido}.
        </p>
      </div>
    );
  }

  return (
    <div className={s.perfilCol}>
      {sesion.fichasPorReclamar.length > 0 && (
        <div className={s.tarjeta}>
          <h3 className={s.tarjetaTitulo}>Tu profesor ya te tiene cargado</h3>
          {error && <div className={s.error}>{error}</div>}
          <div className={s.horariosLista}>
            {sesion.fichasPorReclamar.map((f) => (
              <div key={f.alumnoId} className={s.horarioFila}>
                <div className={s.horarioInfo}>
                  <div className={s.horarioTitulo}>{f.nombre} {f.apellido}</div>
                  <div className={s.horarioDetalle}>{f.club}</div>
                </div>
                <button
                  className={s.btnGuardar}
                  disabled={enviando}
                  onClick={() => void reclamar(f.alumnoId)}
                >
                  Es mi ficha
                </button>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className={s.sinClub}>
        <div className={s.sinClubIcono}>🔎</div>
        <h3 className={s.sinClubTitulo}>Buscar a tu profesor</h3>
        <p className={s.sinClubTexto}>
          Muy pronto vas a poder buscar a tu profesor por nombre o club y
          mandarle una solicitud para tomar clases. Mientras tanto, pedile que
          te cargue en su sistema con tu DNI o teléfono y la vinculación te va
          a aparecer acá arriba.
        </p>
      </div>
    </div>
  );
}
