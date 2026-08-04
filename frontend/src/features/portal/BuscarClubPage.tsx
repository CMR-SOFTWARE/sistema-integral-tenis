import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import ProfesoresClub from '../perfilprofesor/ProfesoresClub';
import { datosCompletos, guardarSesion, obtenerSesion } from '../auth/sesion';
import type { Sesion } from '../auth/sesion';
import s from './PortalPages.module.css';

interface ProfesorPublico {
  tenantId: string;
  club: string;
  profesor: string;
}

/**
 * Mi club: buscá a tu profe por nombre o club y unite. Al unirte entrás a su
 * LISTA DE ESPERA (todavía no sos alumno); cuando tu profe te asigne una clase
 * (un grupo o un horario) el portal se habilita completo.
 */
export default function BuscarClubPage() {
  const [sesion, setSesion] = useState<Sesion | null>(obtenerSesion());
  const [buscar, setBuscar] = useState('');
  const [profes, setProfes] = useState<ProfesorPublico[]>([]);
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState<string | null>(null); // tenantId en curso
  const [error, setError] = useState<string | null>(null);
  const [mirando, setMirando] = useState<ProfesorPublico | null>(null); // club cuyo equipo estoy viendo

  // Refresca la sesión: si el profe te asignó una clase (o recién te uniste), la
  // ficha aparece y la vista cambia sin re-loguear.
  const refrescarSesion = useCallback(async () => {
    try {
      const s2 = await api.get<Sesion>('/auth/yo');
      guardarSesion(s2);
      setSesion(s2);
    } catch { /* sin red: seguimos con la sesión guardada */ }
  }, []);

  useEffect(() => {
    void refrescarSesion();
  }, [refrescarSesion]);

  // Buscador con debounce simple
  useEffect(() => {
    const timer = setTimeout(() => {
      const q = buscar.trim() === '' ? '' : `?buscar=${encodeURIComponent(buscar.trim())}`;
      api.get<ProfesorPublico[]>(`/auth/profesores${q}`).then(setProfes).catch(() => setProfes([]));
    }, 300);
    return () => clearTimeout(timer);
  }, [buscar]);

  const unirme = async (p: ProfesorPublico) => {
    setError(null);
    setEnviando(p.tenantId);
    try {
      await api.post('/portal/solicitudes', {
        tenantId: p.tenantId,
        mensaje: mensaje.trim() || undefined,
      });
      setMensaje('');
      await refrescarSesion(); // ahora tengo ficha (en espera) → cambia la vista
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo unir al club.');
    } finally {
      setEnviando(null);
    }
  };

  // Ya estás vinculado a un club (en lista de espera o alumno pleno)
  if (sesion?.alumno) {
    const ficha = sesion.alumno;
    return (
      <div className={s.perfilCol}>
        <div className={s.tarjeta}>
          <h3 className={s.tarjetaTitulo}>Tu club</h3>
          {ficha.enEspera ? (
            <p className={s.sinClubTexto}>
              Estás en la <b>lista de espera</b> de <b>{ficha.club}</b>. Cuando tu
              profe te asigne un horario o un grupo, tu portal se habilita completo.
            </p>
          ) : (
            <p className={s.sinClubTexto}>
              Estás en <b>{ficha.club}</b> como {ficha.nombre} {ficha.apellido}.
            </p>
          )}
        </div>

        {/* Las cartas de presentación de los profes del club. La ficha vieja
            (guardada antes de esta versión) no trae tenantId: se completa sola
            al refrescar la sesión, arriba. */}
        {ficha.tenantId && (
          <ProfesoresClub tenantId={ficha.tenantId} titulo={`Los profes de ${ficha.club}`} />
        )}
      </div>
    );
  }

  return (
    <div className={s.perfilCol}>
      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Unirte a un club</h3>
        {!datosCompletos(sesion) ? (
          <>
            <p className={s.sinClubTexto}>
              Antes de unirte necesitamos tu DNI, teléfono y fecha de nacimiento
              (con ellos se arma tu ficha en el club).
            </p>
            <Link to="/portal/perfil" className={s.btnPrimario}>Completar mis datos</Link>
          </>
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
                    {/* Conocer al profe ANTES de mandar la solicitud: es para lo
                        que existe la carta de presentación. */}
                    <button
                      className={s.btnSecundario}
                      onClick={() => setMirando(p)}
                    >
                      Conocerlos
                    </button>
                    <button
                      className={s.btnGuardar}
                      disabled={enviando !== null}
                      onClick={() => void unirme(p)}
                    >
                      {enviando === p.tenantId ? 'Uniéndote…' : 'Unirme'}
                    </button>
                  </div>
                ))}
              </div>
            )}
          </>
        )}
      </div>

      {mirando && (
        <ProfesoresClub tenantId={mirando.tenantId} titulo={`Los profes de ${mirando.club}`} />
      )}
    </div>
  );
}
