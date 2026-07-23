import { useCallback, useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import { guardarSesion, obtenerSesion } from '../auth/sesion';
import type { Sesion } from '../auth/sesion';
import MisDatosForm from './MisDatosForm';
import RaquetasSection from './RaquetasSection';
import { comprimirImagen } from './comprimirImagen';
import { CAT_LABEL, CATEGORIAS, avatarColor, iniciales } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import type { MiPerfil } from './types';
import s from './PortalPages.module.css';

/**
 * Mi perfil (M3): foto editable, contacto + categoría editables por el alumno,
 * y mis raquetas. El resto (DNI, modalidad) lo administra el profesor.
 */
export default function PerfilPage() {
  const sesion = obtenerSesion();
  const conClub = sesion?.alumno != null;
  const [perfil, setPerfil] = useState<MiPerfil | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [editando, setEditando] = useState(false);
  const [form, setForm] = useState({ telefono: '', email: '', categoria: 'SinCategoria' as Categoria });
  const [guardando, setGuardando] = useState(false);
  const [guardado, setGuardado] = useState(false);
  const [subiendoFoto, setSubiendoFoto] = useState(false);
  const fotoInput = useRef<HTMLInputElement>(null);

  const cargar = useCallback(() => {
    if (!conClub) return;
    api.get<MiPerfil>('/portal/perfil')
      .then(setPerfil)
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando tu perfil'));
  }, [conClub]);

  useEffect(() => { cargar(); }, [cargar]);

  // Sin club: el perfil son TUS datos de cuenta (los necesita la solicitud)
  if (!conClub) {
    return <MisDatosForm sesion={sesion} onGuardado={(s2: Sesion) => guardarSesion(s2)} />;
  }
  if (error && !perfil) return <div className={s.error}>{error}</div>;
  if (!perfil) return <div className={s.vacio}>Cargando…</div>;

  const empezarEdicion = () => {
    setForm({ telefono: perfil.telefono, email: perfil.email ?? '', categoria: perfil.categoria as Categoria });
    setError(null);
    setEditando(true);
  };

  const guardar = async () => {
    setError(null);
    setGuardando(true);
    try {
      const actualizado = await api.put<MiPerfil>('/portal/perfil', {
        telefono: form.telefono.trim(),
        email: form.email.trim() || undefined,
        categoria: form.categoria,
      });
      setPerfil(actualizado);
      setEditando(false);
      setGuardado(true);
      setTimeout(() => setGuardado(false), 2500);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudieron guardar los cambios.');
    } finally {
      setGuardando(false);
    }
  };

  const elegirFoto = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = ''; // permite re-elegir el mismo archivo
    if (!file) return;
    setError(null);
    setSubiendoFoto(true);
    try {
      const dataUrl = await comprimirImagen(file);
      const actualizado = await api.put<MiPerfil>('/portal/perfil/foto', { fotoUrl: dataUrl });
      setPerfil(actualizado);
    } catch (e2) {
      setError(e2 instanceof ApiError ? e2.message : 'No se pudo subir la foto.');
    } finally {
      setSubiendoFoto(false);
    }
  };

  const quitarFoto = async () => {
    setError(null);
    setSubiendoFoto(true);
    try {
      const actualizado = await api.put<MiPerfil>('/portal/perfil/foto', { fotoUrl: null });
      setPerfil(actualizado);
    } catch (e2) {
      setError(e2 instanceof ApiError ? e2.message : 'No se pudo quitar la foto.');
    } finally {
      setSubiendoFoto(false);
    }
  };

  const av = avatarColor(perfil.nombre + perfil.apellido);
  const cat = CAT_LABEL[perfil.categoria as Categoria] ?? perfil.categoria;

  const datosFijos: [string, string][] = [
    ['DNI', perfil.dni ?? '—'],
    ['Fecha de nacimiento', perfil.fechaNacimiento
      ? new Date(perfil.fechaNacimiento).toLocaleDateString('es-AR')
      : '—'],
    ['Cuenta', sesion?.telefono ?? sesion?.email ?? '—'],
    ['Modalidad de pago', perfil.modalidad === 'PorClase' ? 'Por clase' : 'Mensual'],
    ['Club', perfil.club],
  ];

  return (
    <div className={s.perfilCol}>
      <div className={s.tarjeta}>
        <div className={s.perfilCabecera}>
          <div className={s.perfilAvatarWrap}>
            {perfil.fotoUrl ? (
              <img src={perfil.fotoUrl} alt="Mi foto" className={s.perfilFoto} />
            ) : (
              <div className={s.perfilAvatar} style={{ background: `${av}1a`, color: av }}>
                {iniciales(perfil.nombre, perfil.apellido)}
              </div>
            )}
            <button
              className={s.fotoBtn}
              title="Cambiar foto"
              disabled={subiendoFoto}
              onClick={() => fotoInput.current?.click()}
            >
              {subiendoFoto ? '…' : '📷'}
            </button>
            <input ref={fotoInput} type="file" accept="image/*" hidden onChange={(e) => void elegirFoto(e)} />
          </div>
          <div style={{ flex: 1 }}>
            <div className={s.perfilNombre}>{perfil.nombre} {perfil.apellido}</div>
            <div className={s.perfilChips}>
              {perfil.categoria !== 'SinCategoria' && (
                <span className={`${s.chip} ${s.chipVerde}`}>Categoría {cat}</span>
              )}
              <span className={`${s.chip} ${perfil.estado === 'Activo' ? s.chipVerde : s.chipGris}`}>
                {perfil.estado}
              </span>
            </div>
            {perfil.fotoUrl && (
              <button className={s.fotoQuitar} disabled={subiendoFoto} onClick={() => void quitarFoto()}>
                Quitar foto
              </button>
            )}
          </div>
          {!editando && (
            <button className={s.btnEditar} onClick={empezarEdicion}>
              {guardado ? '✓ Guardado' : 'Editar datos'}
            </button>
          )}
        </div>
      </div>

      <div className={s.tarjeta}>
        {error && <div className={s.error}>{error}</div>}

        {editando ? (
          <div className={s.perfilEdicion}>
            <label className={s.perfilCampo}>
              <span>Teléfono</span>
              <input value={form.telefono} onChange={(e) => setForm((f) => ({ ...f, telefono: e.target.value }))} />
            </label>
            <label className={s.perfilCampo}>
              <span>Email de contacto</span>
              <input type="email" value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} placeholder={sesion?.email ?? ''} />
            </label>
            <label className={s.perfilCampo}>
              <span>Categoría</span>
              <select value={form.categoria} onChange={(e) => setForm((f) => ({ ...f, categoria: e.target.value as Categoria }))}>
                {CATEGORIAS.map((c) => (
                  <option key={c} value={c}>{CAT_LABEL[c]}</option>
                ))}
              </select>
            </label>
            <div className={s.perfilAcciones}>
              <button className={s.btnEditar} onClick={() => setEditando(false)}>Cancelar</button>
              <button className={s.btnGuardar} disabled={guardando || !form.telefono.trim()} onClick={() => void guardar()}>
                {guardando ? 'Guardando…' : 'Guardar cambios'}
              </button>
            </div>
          </div>
        ) : (
          <>
            <div className={s.perfilDato}>
              <span className={s.perfilLabel}>Teléfono</span>
              <span className={s.perfilValor}>{perfil.telefono}</span>
            </div>
            <div className={s.perfilDato}>
              <span className={s.perfilLabel}>Email de contacto</span>
              <span className={s.perfilValor}>{perfil.email ?? sesion?.email ?? '—'}</span>
            </div>
          </>
        )}

        {datosFijos.map(([label, valor]) => (
          <div key={label} className={s.perfilDato}>
            <span className={s.perfilLabel}>{label}</span>
            <span className={s.perfilValor}>{valor}</span>
          </div>
        ))}
      </div>

      <p className={s.nota}>
        Podés editar tu foto, contacto y categoría, y cargar tus raquetas. El DNI y la
        modalidad de pago los administra tu profesor.
      </p>

      <RaquetasSection raquetas={perfil.raquetas} onCambio={cargar} />

      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Mi cuenta</h3>
        <div className={s.perfilDato}>
          <span className={s.perfilLabel}>Usuario de acceso</span>
          <span className={s.perfilValor}>{sesion?.telefono ?? sesion?.email ?? '—'}</span>
        </div>
        <div className={s.perfilAcciones}>
          <Link to="/cambiar-password" className={s.btnEditar}>Cambiar contraseña</Link>
        </div>
      </div>
    </div>
  );
}
