import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import AuthShell from './AuthShell';
import InputPassword from '../../components/InputPassword';
import { entrarConSesion } from './entrar';
import type { Sesion } from './sesion';
import s from './LoginPage.module.css';

/**
 * Entrada real (JWT): login con email + contraseña. El registro segmentado
 * vive en /registro. La vinculación con un club es por SOLICITUD (portal →
 * Mi club) o porque el profe te dio de alta con usuario y temporal.
 */
export default function LoginPage() {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const login = async () => {
    setError(null);
    setEnviando(true);
    try {
      entrarConSesion(await api.post<Sesion>('/auth/login', { email, password }), navigate);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo iniciar sesión.');
      setEnviando(false);
    }
  };

  return (
    <AuthShell>
      <h2 className={s.cajaTitulo}>Ingresá a tu cuenta</h2>
      <p className={s.cajaBajada}>Profesores y alumnos entran por acá.</p>

      <form onSubmit={(e) => { e.preventDefault(); void login(); }}>
        <label className={s.campo}>
          <span>Email</span>
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="tu@email.com" autoFocus />
        </label>
        <label className={s.campo}>
          <span>Contraseña</span>
          <InputPassword value={password} onChange={setPassword} />
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
    </AuthShell>
  );
}
