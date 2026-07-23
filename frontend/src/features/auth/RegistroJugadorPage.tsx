import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import { CATEGORIAS, CAT_LABEL } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import AuthShell from './AuthShell';
import InputPassword from '../../components/InputPassword';
import { entrarConSesion } from './entrar';
import type { Sesion } from './sesion';
import s from './LoginPage.module.css';

/** Registro GRATIS de jugador: datos personales completos + categoría. */
export default function RegistroJugadorPage() {
  const navigate = useNavigate();
  const [f, setF] = useState({
    nombre: '', apellido: '', email: '', dni: '', telefono: '',
    fechaNacimiento: '', categoria: 'SinCategoria', password: '',
  });
  const set = (k: keyof typeof f, v: string) => setF((x) => ({ ...x, [k]: v }));
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const validar = (): string | null => {
    if (f.email.trim() && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.email.trim()))
      return 'El email no tiene un formato válido.';
    if (f.dni.trim() && !/^\d{7,9}$/.test(f.dni.trim()))
      return 'El DNI debe tener solo números (7 a 9 dígitos), sin puntos.';
    if (!/^\+?[0-9 -]{8,20}$/.test(f.telefono.trim()))
      return 'El celular debe tener entre 8 y 20 dígitos (podés incluir el +54).';
    if (f.password.length < 8) return 'La contraseña necesita al menos 8 caracteres.';
    return null;
  };

  const incompleto =
    !f.nombre.trim() || !f.apellido.trim() || !f.telefono.trim() || f.password.length < 8;

  const registrar = async () => {
    const invalido = validar();
    if (invalido) {
      setError(invalido);
      return;
    }
    setError(null);
    setEnviando(true);
    try {
      const sesion = await api.post<Sesion>('/auth/registro', {
        nombre: f.nombre.trim(),
        apellido: f.apellido.trim(),
        telefono: f.telefono.trim(),
        email: f.email.trim() || undefined,
        password: f.password,
        dni: f.dni.trim() || undefined,
        fechaNacimiento: f.fechaNacimiento || undefined,
        categoria: f.categoria,
      });
      entrarConSesion(sesion, navigate); // → portal (elige su profe desde Mi club)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear la cuenta.');
      setEnviando(false);
    }
  };

  return (
    <AuthShell>
      <h2 className={s.cajaTitulo}>Creá tu cuenta de jugador</h2>
      <p className={s.cajaBajada}>
        Es gratis. Después vas a poder vincularte con tu profesor desde el portal.
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
        <div className={s.dosCol}>
          <label className={s.campo}>
            <span>Fecha de nacimiento (opcional)</span>
            <input type="date" value={f.fechaNacimiento} onChange={(e) => set('fechaNacimiento', e.target.value)} />
          </label>
          <label className={s.campo}>
            <span>Categoría</span>
            <select value={f.categoria} onChange={(e) => set('categoria', e.target.value)}>
              <option value="SinCategoria">No sé todavía</option>
              {CATEGORIAS.filter((c) => c !== 'SinCategoria').map((c: Categoria) => (
                <option key={c} value={c}>{CAT_LABEL[c]}</option>
              ))}
            </select>
          </label>
        </div>
        <label className={s.campo}>
          <span>Contraseña (mínimo 8 caracteres)</span>
          <InputPassword value={f.password} onChange={(v) => set('password', v)} />
        </label>

        {error && <div className={s.error}>{error}</div>}
        <button type="submit" className={s.btnEntrar} disabled={enviando || incompleto}>
          {enviando ? 'Creando…' : 'Crear cuenta gratis'}
        </button>
      </form>

      <Link to="/registro" className={s.linkCambio}>← Elegir otro tipo de cuenta</Link>
    </AuthShell>
  );
}
