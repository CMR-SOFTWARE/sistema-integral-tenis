import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import type { Sesion } from '../auth/sesion';
import { CATEGORIAS, CAT_LABEL } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import s from './PortalPages.module.css';

interface Props {
  sesion: Sesion | null;
  onGuardado: (s: Sesion) => void;
}

/**
 * Perfil del jugador SIN club: sus datos de cuenta (DNI, teléfono, fecha de
 * nacimiento, categoría). La solicitud a un profe los necesita para armar
 * la ficha — acá se completan o corrigen.
 */
export default function MisDatosForm({ sesion, onGuardado }: Props) {
  const [f, setF] = useState({
    dni: sesion?.dni ?? '',
    telefono: sesion?.telefono ?? '',
    fechaNacimiento: sesion?.fechaNacimiento?.slice(0, 10) ?? '',
    categoria: sesion?.categoria ?? 'SinCategoria',
  });
  const set = (k: keyof typeof f, v: string) => setF((x) => ({ ...x, [k]: v }));
  const [guardando, setGuardando] = useState(false);
  const [guardado, setGuardado] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const incompleto = !f.dni.trim() || !f.telefono.trim() || f.fechaNacimiento === '';

  const guardar = async () => {
    setError(null);
    setGuardando(true);
    try {
      const s2 = await api.post<Sesion>('/auth/mis-datos', {
        dni: f.dni.trim(),
        telefono: f.telefono.trim(),
        fechaNacimiento: f.fechaNacimiento,
        categoria: f.categoria,
      });
      onGuardado(s2);
      setGuardado(true);
      setTimeout(() => setGuardado(false), 2500);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudieron guardar tus datos.');
    } finally {
      setGuardando(false);
    }
  };

  return (
    <div className={s.perfilCol}>
      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Mis datos</h3>
        <p className={s.sinClubTexto}>
          Cuando le mandes una solicitud a tu profesor, tu ficha se arma con
          estos datos.
        </p>
        <div className={s.perfilEdicion}>
          <label className={s.perfilCampo}>
            <span>DNI</span>
            <input value={f.dni} onChange={(e) => set('dni', e.target.value)} placeholder="30111222" />
          </label>
          <label className={s.perfilCampo}>
            <span>Teléfono</span>
            <input value={f.telefono} onChange={(e) => set('telefono', e.target.value)} placeholder="+54 9 11..." />
          </label>
          <label className={s.perfilCampo}>
            <span>Fecha de nacimiento</span>
            <input type="date" value={f.fechaNacimiento} onChange={(e) => set('fechaNacimiento', e.target.value)} />
          </label>
          <label className={s.perfilCampo}>
            <span>Categoría</span>
            <select value={f.categoria} onChange={(e) => set('categoria', e.target.value)}>
              <option value="SinCategoria">No sé todavía</option>
              {CATEGORIAS.filter((c) => c !== 'SinCategoria').map((c: Categoria) => (
                <option key={c} value={c}>{CAT_LABEL[c]}</option>
              ))}
            </select>
          </label>
        </div>
        {error && <div className={s.error}>{error}</div>}
        <div className={s.perfilAcciones}>
          <button className={s.btnGuardar} disabled={guardando || incompleto} onClick={() => void guardar()}>
            {guardando ? 'Guardando…' : guardado ? '¡Guardado!' : 'Guardar mis datos'}
          </button>
        </div>
      </div>

      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Mi cuenta</h3>
        <div className={s.perfilDato}>
          <span className={s.perfilLabel}>Email</span>
          <span className={s.perfilValor}>{sesion?.email}</span>
        </div>
        <div className={s.perfilAcciones}>
          <Link to="/cambiar-password" className={s.btnEditar}>Cambiar contraseña</Link>
        </div>
      </div>
    </div>
  );
}
