import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import AuthShell from './AuthShell';
import { entrarConSesion } from './entrar';
import type { Sesion } from './sesion';
import s from './LoginPage.module.css';

type Vista = 'login' | 'vincular';

/**
 * Entrada real (JWT): login con email + contraseña. El registro segmentado
 * vive en /registro. Si un club ya te tenía cargado (match DNI/tel), acá se
 * ofrece el reclamo de la ficha (transitorio: lo reemplazan las solicitudes).
 */
export default function LoginPage() {
  const navigate = useNavigate();
  const [vista, setVista] = useState<Vista>('login');
  const [sesion, setSesion] = useState<Sesion | null>(null); // para la vista vincular
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  /** Guardar y navegar; si hay fichas por reclamar, mostrar la vista vincular. */
  const entrar = (s2: Sesion) => {
    if (!entrarConSesion(s2, navigate)) {
      setSesion(s2);
      setVista('vincular');
    }
  };

  const conError = async (accion: () => Promise<void>, fallback: string) => {
    setError(null);
    setEnviando(true);
    try {
      await accion();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : fallback);
    } finally {
      setEnviando(false);
    }
  };

  const login = () =>
    conError(async () => {
      entrar(await api.post<Sesion>('/auth/login', { email, password }));
    }, 'No se pudo iniciar sesión.');

  const reclamar = (alumnoId: string) =>
    conError(async () => {
      entrar(await api.post<Sesion>('/auth/reclamar', { alumnoId }));
    }, 'No se pudo vincular la ficha.');

  return (
    <AuthShell>
      {vista === 'login' && (
        <>
          <h2 className={s.cajaTitulo}>Ingresá a tu cuenta</h2>
          <p className={s.cajaBajada}>Profesores y alumnos entran por acá.</p>

          <form onSubmit={(e) => { e.preventDefault(); void login(); }}>
            <label className={s.campo}>
              <span>Email</span>
              <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="tu@email.com" autoFocus />
            </label>
            <label className={s.campo}>
              <span>Contraseña</span>
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" />
            </label>
            {error && <div className={s.error}>{error}</div>}
            <button type="submit" className={s.btnEntrar} disabled={enviando || !email || !password}>
              {enviando ? 'Entrando…' : 'Entrar'}
            </button>
          </form>

          <Link to="/registro" className={s.linkCambio}>
            ¿No tenés cuenta? <b>Creá una gratis</b>
          </Link>

          <div className={s.pie}>Demo profesor: profe@clubdemo.com · profe1234</div>
        </>
      )}

      {vista === 'vincular' && sesion && (
        <>
          <h2 className={s.cajaTitulo}>¡Hola, {sesion.nombre}!</h2>
          <p className={s.cajaBajada}>
            Tu profesor ya te tiene cargado: vinculá tu cuenta con tu ficha
            del club para ver tus clases y tu cuota.
          </p>
          {error && <div className={s.error}>{error}</div>}

          {sesion.fichasPorReclamar.map((f) => (
            <button
              key={f.alumnoId}
              className={s.tarjetaRol}
              disabled={enviando}
              onClick={() => void reclamar(f.alumnoId)}
            >
              <div className={`${s.rolIcono} ${s.rolIconoAlumno}`}>
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#0e6b3c" strokeWidth="2" strokeLinecap="round">
                  <path d="M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0zM4 21v-1a6 6 0 0 1 12 0v1" />
                </svg>
              </div>
              <div className={s.rolTexto}>
                <div className={s.rolTitulo}>{f.nombre} {f.apellido}</div>
                <div className={s.rolDetalle}>{f.club}</div>
              </div>
              <span className={s.proximamente}>Es mi ficha</span>
            </button>
          ))}

          <button className={s.linkCambio} onClick={() => navigate('/portal')}>
            Ninguna es mía — <b>entrar sin vincular</b>
          </button>
        </>
      )}
    </AuthShell>
  );
}
