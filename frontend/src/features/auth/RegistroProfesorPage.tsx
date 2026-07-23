import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import AuthShell from './AuthShell';
import InputPassword from '../../components/InputPassword';
import { entrarConSesion } from './entrar';
import type { Sesion } from './sesion';
import s from './LoginPage.module.css';

/** Registro de PROFESOR: identidad + su club (nace pendiente de pago → checkout). */
export default function RegistroProfesorPage() {
  const navigate = useNavigate();
  const [f, setF] = useState({
    nombre: '', apellido: '', email: '', dni: '', telefono: '',
    nombreClub: '', password: '',
  });
  const set = (k: keyof typeof f, v: string) => setF((x) => ({ ...x, [k]: v }));
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const incompleto =
    !f.nombre.trim() || !f.apellido.trim() || !f.telefono.trim() ||
    !f.nombreClub.trim() || f.password.length < 8;

  const registrar = async () => {
    if (f.email.trim() && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.email.trim())) {
      setError('El email no tiene un formato válido.');
      return;
    }
    setError(null);
    setEnviando(true);
    try {
      const sesion = await api.post<Sesion>('/auth/registro-profesor', {
        nombre: f.nombre.trim(),
        apellido: f.apellido.trim(),
        telefono: f.telefono.trim(),
        email: f.email.trim() || undefined,
        password: f.password,
        dni: f.dni.trim() || undefined,
        nombreClub: f.nombreClub.trim(),
      });
      entrarConSesion(sesion, navigate); // → /checkout (nace PendientePago)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear la cuenta.');
      setEnviando(false);
    }
  };

  return (
    <AuthShell>
      <h2 className={s.cajaTitulo}>Creá tu cuenta de profesor</h2>
      <p className={s.cajaBajada}>
        Tu club queda reservado y se activa al confirmar la suscripción.
      </p>

      <form onSubmit={(e) => { e.preventDefault(); void registrar(); }}>
        <div className={s.dosCol}>
          <label className={s.campo}>
            <span>Nombre</span>
            <input value={f.nombre} onChange={(e) => set('nombre', e.target.value)} autoFocus />
          </label>
          <label className={s.campo}>
            <span>Apellido</span>
            <input value={f.apellido} onChange={(e) => set('apellido', e.target.value)} />
          </label>
        </div>
        <div className={s.dosCol}>
          <label className={s.campo}>
            <span>Celular (tu usuario para entrar)</span>
            <input value={f.telefono} onChange={(e) => set('telefono', e.target.value)} placeholder="11 5555 1234" />
          </label>
          <label className={s.campo}>
            <span>DNI (opcional)</span>
            <input value={f.dni} onChange={(e) => set('dni', e.target.value)} placeholder="30111222" />
          </label>
        </div>
        <label className={s.campo}>
          <span>Email (opcional)</span>
          <input type="email" value={f.email} onChange={(e) => set('email', e.target.value)} placeholder="tu@email.com" />
        </label>
        <label className={s.campo}>
          <span>Nombre de tu club o academia</span>
          <input value={f.nombreClub} onChange={(e) => set('nombreClub', e.target.value)} placeholder="Academia Río Cuarto" />
        </label>
        <label className={s.campo}>
          <span>Contraseña (mínimo 8 caracteres)</span>
          <InputPassword value={f.password} onChange={(v) => set('password', v)} />
        </label>

        {error && <div className={s.error}>{error}</div>}
        <button type="submit" className={s.btnEntrar} disabled={enviando || incompleto}>
          {enviando ? 'Creando…' : 'Continuar al pago'}
        </button>
      </form>

      <Link to="/registro" className={s.linkCambio}>← Elegir otro tipo de cuenta</Link>
    </AuthShell>
  );
}
