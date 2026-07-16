import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import AuthShell from './AuthShell';
import InputPassword from '../../components/InputPassword';
import { entrarConSesion } from './entrar';
import { obtenerSesion } from './sesion';
import type { Sesion } from './sesion';
import s from './LoginPage.module.css';

/** Cambio de contraseña VOLUNTARIO (desde el perfil). */
export default function CambiarPasswordPage() {
  const navigate = useNavigate();
  const sesion = obtenerSesion();
  const [actual, setActual] = useState('');
  const [nueva, setNueva] = useState('');
  const [confirmar, setConfirmar] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const cambiar = async () => {
    if (nueva.length < 8) {
      setError('La contraseña nueva necesita al menos 8 caracteres.');
      return;
    }
    if (nueva !== confirmar) {
      setError('Las contraseñas nuevas no coinciden.');
      return;
    }
    setError(null);
    setEnviando(true);
    try {
      const s2 = await api.post<Sesion>('/auth/cambiar-password', {
        passwordActual: actual,
        passwordNueva: nueva,
      });
      entrarConSesion(s2, navigate); // flag apagado → a su casa (portal/dashboard)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo cambiar la contraseña.');
      setEnviando(false);
    }
  };

  const volver = () => navigate(-1);

  return (
    <AuthShell>
      <h2 className={s.cajaTitulo}>Cambiar contraseña</h2>
      <p className={s.cajaBajada}>
        {sesion?.nombre}, elegí tu nueva contraseña. Si tu profe te creó la
        cuenta, la actual es tu número de teléfono (solo dígitos).
      </p>

      <form onSubmit={(e) => { e.preventDefault(); void cambiar(); }}>
        <label className={s.campo}>
          <span>Contraseña actual</span>
          <InputPassword value={actual} onChange={setActual} autoFocus />
        </label>
        <label className={s.campo}>
          <span>Contraseña nueva (mínimo 8 caracteres)</span>
          <InputPassword value={nueva} onChange={setNueva} />
        </label>
        <label className={s.campo}>
          <span>Repetir contraseña nueva</span>
          <InputPassword value={confirmar} onChange={setConfirmar} />
        </label>
        {error && <div className={s.error}>{error}</div>}
        <button
          type="submit"
          className={s.btnEntrar}
          disabled={enviando || !actual || nueva.length < 8 || !confirmar}
        >
          {enviando ? 'Guardando…' : 'Cambiar contraseña y entrar'}
        </button>
      </form>

      <button className={s.linkCambio} onClick={volver}>Volver sin cambiar</button>
    </AuthShell>
  );
}
