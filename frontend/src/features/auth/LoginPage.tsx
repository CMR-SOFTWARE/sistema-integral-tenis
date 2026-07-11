import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import { guardarSesion } from './sesion';
import type { Sesion } from './sesion';
import s from './LoginPage.module.css';

type Vista = 'login' | 'registro' | 'vincular';

/**
 * Entrada real (JWT): login con email + contraseña, registro GRATIS de
 * jugador (ADR-0007) y reclamo de ficha si un club ya lo tenía cargado.
 * El destino depende de las membresías: profesor → gestión; alumno → portal.
 */
export default function LoginPage() {
  const navigate = useNavigate();
  const [vista, setVista] = useState<Vista>('login');
  const [sesion, setSesion] = useState<Sesion | null>(null); // para la vista vincular
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  // login
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  // registro
  const [reg, setReg] = useState({
    nombre: '', apellido: '', email: '', dni: '', telefono: '', password: '',
  });
  const setR = (k: keyof typeof reg, v: string) => setReg((f) => ({ ...f, [k]: v }));

  // vincular: corrección de datos para volver a buscar coincidencias
  const [datos, setDatos] = useState({ dni: '', telefono: '' });

  /** Después de login/registro/reclamo: guardar y decidir el destino. */
  const entrar = (s: Sesion) => {
    guardarSesion(s);
    if (s.esProfesor) navigate('/dashboard');
    else if (s.alumno) navigate('/portal');
    else {
      setSesion(s);
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

  const registrar = () =>
    conError(async () => {
      entrar(await api.post<Sesion>('/auth/registro', {
        nombre: reg.nombre.trim(),
        apellido: reg.apellido.trim(),
        email: reg.email.trim(),
        password: reg.password,
        dni: reg.dni.trim() || undefined,
        telefono: reg.telefono.trim() || undefined,
      }));
    }, 'No se pudo crear la cuenta.');

  const reclamar = (alumnoId: string) =>
    conError(async () => {
      entrar(await api.post<Sesion>('/auth/reclamar', { alumnoId }));
    }, 'No se pudo vincular la ficha.');

  const actualizarDatos = () =>
    conError(async () => {
      entrar(await api.post<Sesion>('/auth/mis-datos', {
        dni: datos.dni.trim() || undefined,
        telefono: datos.telefono.trim() || undefined,
      }));
    }, 'No se pudieron actualizar tus datos.');

  return (
    <div className={s.pantalla}>
      {/* ── Panel izquierdo: marca ── */}
      <div className={s.panelMarca}>
        <div className={s.grilla} />
        <div className={s.pelota} />
        <div className={s.aro} />

        <div className={s.marca}>
          <div className={s.marcaLogo}>C</div>
          <div className={s.marcaNombre}>CourtSet</div>
        </div>

        <div className={s.heroTexto}>
          <div className={s.eyebrow}>Gestión deportiva</div>
          <h1 className={s.titulo}>Tu cancha,<br />bajo control.</h1>
          <p className={s.bajada}>
            Alumnos, turnos, grupos por categoría, cuotas y disponibilidad.
            Todo en un solo lugar, rápido y claro.
          </p>
        </div>

        <div className={s.stats}>
          <div><div className={s.statNum}>12</div><div className={s.statLabel}>Alumnos</div></div>
          <div className={s.statSep} />
          <div><div className={s.statNum}>7</div><div className={s.statLabel}>Categorías</div></div>
          <div className={s.statSep} />
          <div><div className={s.statNum}>$228k</div><div className={s.statLabel}>Este mes</div></div>
        </div>
      </div>

      {/* ── Panel derecho ── */}
      <div className={s.panelLogin}>
        <div className={s.caja}>
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

              <button className={s.linkCambio} onClick={() => { setError(null); setVista('registro'); }}>
                ¿Jugás o entrenás? <b>Creá tu cuenta gratis</b>
              </button>

              <div className={s.pie}>Demo profesor: profe@clubdemo.com · profe1234</div>
            </>
          )}

          {vista === 'registro' && (
            <>
              <h2 className={s.cajaTitulo}>Creá tu cuenta</h2>
              <p className={s.cajaBajada}>
                Es gratis. Con tu DNI o teléfono te vinculamos a tu club si ya te tienen cargado.
              </p>

              <form onSubmit={(e) => { e.preventDefault(); void registrar(); }}>
                <div className={s.dosCol}>
                  <label className={s.campo}>
                    <span>Nombre</span>
                    <input value={reg.nombre} onChange={(e) => setR('nombre', e.target.value)} autoFocus />
                  </label>
                  <label className={s.campo}>
                    <span>Apellido</span>
                    <input value={reg.apellido} onChange={(e) => setR('apellido', e.target.value)} />
                  </label>
                </div>
                <label className={s.campo}>
                  <span>Email</span>
                  <input type="email" value={reg.email} onChange={(e) => setR('email', e.target.value)} placeholder="tu@email.com" />
                </label>
                <div className={s.dosCol}>
                  <label className={s.campo}>
                    <span>DNI</span>
                    <input value={reg.dni} onChange={(e) => setR('dni', e.target.value)} placeholder="30111222" />
                  </label>
                  <label className={s.campo}>
                    <span>Teléfono</span>
                    <input value={reg.telefono} onChange={(e) => setR('telefono', e.target.value)} placeholder="+54 9 11..." />
                  </label>
                </div>
                <label className={s.campo}>
                  <span>Contraseña (mínimo 8 caracteres)</span>
                  <input type="password" value={reg.password} onChange={(e) => setR('password', e.target.value)} />
                </label>
                {error && <div className={s.error}>{error}</div>}
                <button
                  type="submit"
                  className={s.btnEntrar}
                  disabled={enviando || !reg.nombre.trim() || !reg.apellido.trim() || !reg.email.trim() || reg.password.length < 8}
                >
                  {enviando ? 'Creando…' : 'Crear cuenta gratis'}
                </button>
              </form>

              <button className={s.linkCambio} onClick={() => { setError(null); setVista('login'); }}>
                Ya tengo cuenta — <b>iniciar sesión</b>
              </button>
            </>
          )}

          {vista === 'vincular' && sesion && (
            <>
              <h2 className={s.cajaTitulo}>¡Hola, {sesion.nombre}!</h2>
              <p className={s.cajaBajada}>
                Tu cuenta ya está creada. Para ver tus clases y tu cuota hay que
                vincularla con tu ficha del club.
              </p>
              {error && <div className={s.error}>{error}</div>}

              {sesion.fichasPorReclamar.length > 0 ? (
                sesion.fichasPorReclamar.map((f) => (
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
                ))
              ) : (
                <p className={s.cajaBajada}>
                  No encontramos ninguna ficha que coincida. Pedile a tu profesor que te
                  cargue con el mismo DNI o teléfono de tu cuenta, o corregí tus datos acá abajo.
                </p>
              )}

              {/* Corregir datos y volver a buscar (p. ej. si cargaste mal el DNI) */}
              <div className={s.vincularDatos}>
                <p className={s.vincularTexto}>
                  {sesion.fichasPorReclamar.length > 0
                    ? '¿La coincidencia no sos vos? Corregí tus datos y buscamos de nuevo:'
                    : 'Corregir mis datos:'}
                </p>
                <div className={s.dosCol}>
                  <label className={s.campo}>
                    <span>DNI</span>
                    <input value={datos.dni} onChange={(e) => setDatos((d) => ({ ...d, dni: e.target.value }))} placeholder="30111222" />
                  </label>
                  <label className={s.campo}>
                    <span>Teléfono</span>
                    <input value={datos.telefono} onChange={(e) => setDatos((d) => ({ ...d, telefono: e.target.value }))} placeholder="+54 9 11..." />
                  </label>
                </div>
                <button
                  className={s.btnEntrar}
                  disabled={enviando || (!datos.dni.trim() && !datos.telefono.trim())}
                  onClick={() => void actualizarDatos()}
                >
                  {enviando ? 'Buscando…' : 'Actualizar y buscar de nuevo'}
                </button>
              </div>

              <button className={s.linkCambio} onClick={() => { setError(null); setVista('login'); }}>
                Volver al inicio de sesión
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
