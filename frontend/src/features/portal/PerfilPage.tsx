import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import { guardarSesion, obtenerSesion } from '../auth/sesion';
import type { Sesion } from '../auth/sesion';
import MisDatosForm from './MisDatosForm';
import { CAT_LABEL, avatarColor, iniciales } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import type { MiPerfil } from './types';
import s from './PortalPages.module.css';

/**
 * Mi perfil (mockup): cabecera con chips + la ficha. El alumno edita SUS
 * datos de contacto (teléfono/email); el resto lo administra el profesor.
 */
export default function PerfilPage() {
  const sesion = obtenerSesion();
  const conClub = sesion?.alumno != null;
  const [perfil, setPerfil] = useState<MiPerfil | null>(null);
  const [error, setError] = useState<string | null>(null);

  // edición de contacto
  const [editando, setEditando] = useState(false);
  const [form, setForm] = useState({ telefono: '', email: '' });
  const [guardando, setGuardando] = useState(false);
  const [guardado, setGuardado] = useState(false);

  useEffect(() => {
    if (!conClub) return;
    api.get<MiPerfil>('/portal/perfil')
      .then(setPerfil)
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando tu perfil'));
  }, [conClub]);

  // Sin club: el perfil son TUS datos de cuenta (los necesita la solicitud)
  if (!conClub) {
    return (
      <MisDatosForm
        sesion={sesion}
        onGuardado={(s2: Sesion) => guardarSesion(s2)}
      />
    );
  }
  if (error && !perfil) return <div className={s.error}>{error}</div>;
  if (!perfil) return <div className={s.vacio}>Cargando…</div>;

  const empezarEdicion = () => {
    setForm({ telefono: perfil.telefono, email: perfil.email ?? '' });
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

  const av = avatarColor(perfil.nombre + perfil.apellido);
  const cat = CAT_LABEL[perfil.categoria as Categoria] ?? perfil.categoria;

  // Una sola fila de email: el de la ficha, o el de la cuenta si no hay
  const datosFijos: [string, string][] = [
    ['DNI', perfil.dni],
    ['Fecha de nacimiento', new Date(perfil.fechaNacimiento).toLocaleDateString('es-AR')],
    ['Cuenta', sesion?.email ?? '—'],
    ['Modalidad de pago', perfil.modalidad === 'PorClase' ? 'Por clase' : 'Mensual'],
    ['Club', perfil.club],
  ];

  return (
    <div className={s.perfilCol}>
      <div className={s.tarjeta}>
        <div className={s.perfilCabecera}>
          <div className={s.perfilAvatar} style={{ background: `${av}1a`, color: av }}>
            {iniciales(perfil.nombre, perfil.apellido)}
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
          </div>
          {!editando && (
            <button className={s.btnEditar} onClick={empezarEdicion}>
              {guardado ? '✓ Guardado' : 'Editar contacto'}
            </button>
          )}
        </div>
      </div>

      <div className={s.tarjeta}>
        {error && <div className={s.error}>{error}</div>}

        {/* Contacto: editable por el alumno */}
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
            <div className={s.perfilAcciones}>
              <button className={s.btnEditar} onClick={() => setEditando(false)}>Cancelar</button>
              <button
                className={s.btnGuardar}
                disabled={guardando || !form.telefono.trim()}
                onClick={() => void guardar()}
              >
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

        {/* El resto lo administra el profe */}
        {datosFijos.map(([label, valor]) => (
          <div key={label} className={s.perfilDato}>
            <span className={s.perfilLabel}>{label}</span>
            <span className={s.perfilValor}>{valor}</span>
          </div>
        ))}
      </div>

      <p className={s.nota}>
        Podés editar tu teléfono y email. El resto de los datos (DNI, categoría,
        modalidad) los corrige tu profesor.
      </p>

      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Mi cuenta</h3>
        <div className={s.perfilDato}>
          <span className={s.perfilLabel}>Email de acceso</span>
          <span className={s.perfilValor}>{sesion?.email}</span>
        </div>
        <div className={s.perfilAcciones}>
          <Link to="/cambiar-password" className={s.btnEditar}>Cambiar contraseña</Link>
        </div>
      </div>
    </div>
  );
}
