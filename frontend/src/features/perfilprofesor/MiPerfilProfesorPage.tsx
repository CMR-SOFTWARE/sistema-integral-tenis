import { useEffect, useState } from 'react';
import { ApiError } from '../../lib/api';
import EditorGaleria from './EditorGaleria';
import EditorHitos from './EditorHitos';
import PerfilVista, { PerfilVistaCargando } from './PerfilVista';
import SubirImagen from './SubirImagen';
import { TOPES } from './types';
import { useEditarPerfil, useMiPerfil } from './usePerfilProfesor';
import s from './MiPerfil.module.css';

/**
 * "Mi perfil" del profe: su carta de presentación. La editan tanto el dueño como
 * los profes empleados —cada uno la suya—, y es lo que ven los alumnos del club
 * (y quien esté buscando profe) desde Mi club.
 */
export default function MiPerfilProfesorPage() {
  const { data: perfil, isLoading } = useMiPerfil();
  const acciones = useEditarPerfil();

  const [form, setForm] = useState({ titular: '', subtitulo: '', bio: '' });
  const [especialidades, setEspecialidades] = useState<string[]>([]);
  const [nuevaEspecialidad, setNuevaEspecialidad] = useState('');
  const [guardando, setGuardando] = useState(false);
  const [guardado, setGuardado] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [vistaPrevia, setVistaPrevia] = useState(false);

  // Los campos de texto son estado local (se escriben y se guardan de una vez);
  // el resto (fotos, hitos) se guarda solo, así que sale directo del servidor.
  useEffect(() => {
    if (!perfil) return;
    setForm({
      titular: perfil.titular ?? '',
      subtitulo: perfil.subtitulo ?? '',
      bio: perfil.bio ?? '',
    });
    setEspecialidades(perfil.especialidades);
  }, [perfil]);

  if (isLoading || !perfil) return <PerfilVistaCargando />;

  const avisar = (e: unknown, porDefecto: string) =>
    setError(e instanceof ApiError ? e.message : porDefecto);

  const guardarTextos = async (cambios?: { publicado?: boolean }) => {
    setError(null);
    setGuardando(true);
    try {
      await acciones.guardar({
        titular: form.titular.trim() || null,
        subtitulo: form.subtitulo.trim() || null,
        bio: form.bio.trim() || null,
        especialidades,
        publicado: cambios?.publicado ?? perfil.publicado,
      });
      setGuardado(true);
      setTimeout(() => setGuardado(false), 2500);
    } catch (e) {
      avisar(e, 'No se pudo guardar tu perfil.');
    } finally {
      setGuardando(false);
    }
  };

  const agregarEspecialidad = () => {
    const texto = nuevaEspecialidad.trim();
    if (!texto || especialidades.length >= TOPES.especialidades) return;
    if (especialidades.some((e) => e.toLowerCase() === texto.toLowerCase())) {
      setNuevaEspecialidad('');
      return;
    }
    setEspecialidades([...especialidades, texto]);
    setNuevaEspecialidad('');
  };

  return (
    <div className={s.pagina}>
      <div className={s.barra}>
        <div className={s.barraTexto}>
          <div className={s.barraTitulo}>
            {perfil.publicado ? 'Tu perfil está publicado' : 'Tu perfil está oculto'}
          </div>
          <div className={s.barraAyuda}>
            {perfil.publicado
              ? `Tus alumnos de ${perfil.club} lo ven en "Mi club", y también quien esté buscando profe.`
              : 'Armalo tranquilo: nadie lo ve hasta que lo publiques.'}
          </div>
        </div>

        <label className={s.switch}>
          <input
            type="checkbox"
            checked={perfil.publicado}
            disabled={guardando}
            onChange={(e) => void guardarTextos({ publicado: e.target.checked })}
          />
          <span className={s.pista} />
          Publicado
        </label>

        <button className={s.btnSuave} onClick={() => setVistaPrevia((v) => !v)}>
          {vistaPrevia ? 'Volver a editar' : 'Ver cómo queda'}
        </button>
      </div>

      {error && <div className={s.error}>{error}</div>}

      {vistaPrevia ? (
        <PerfilVista
          perfil={{ ...perfil, titular: form.titular || null, subtitulo: form.subtitulo || null, bio: form.bio || null, especialidades }}
          mensajeVacio="Todavía no cargaste nada. Contá quién sos, sumá tu trayectoria y algunas fotos: así te van a conocer antes de escribirte."
        />
      ) : (
        <>
          {/* ── Imágenes ── */}
          <section className={s.tarjeta}>
            <h3 className={s.tarjetaTitulo}>Portada y foto</h3>
            <p className={s.ayuda}>
              La portada es la imagen ancha del encabezado; la foto, tu retrato. Se
              achican solas antes de subirse, no hace falta que las prepares.
            </p>
            <div className={s.imagenes}>
              <div className={s.bloqueImagen}>
                <div className={s.previaPortada}>
                  {perfil.portadaUrl && <img src={perfil.portadaUrl} alt="Portada" />}
                </div>
                <div className={s.acciones}>
                  <SubirImagen
                    etiqueta={perfil.portadaUrl ? 'Cambiar portada' : 'Subir portada'}
                    onError={setError}
                    onElegir={acciones.subirPortada}
                  />
                  {perfil.portadaUrl && (
                    <button
                      className={s.btnPeligro}
                      onClick={() => acciones.quitarPortada().catch((e) => avisar(e, 'No se pudo quitar la portada.'))}
                    >
                      Quitar
                    </button>
                  )}
                </div>
              </div>

              <div className={s.bloqueImagen}>
                <div className={s.previaAvatar}>
                  {perfil.avatarUrl
                    ? <img src={perfil.avatarUrl} alt="Foto de perfil" />
                    : <span style={{ fontSize: 12, color: 'var(--color-text-faint)' }}>Sin foto</span>}
                </div>
                <div className={s.acciones}>
                  <SubirImagen
                    etiqueta={perfil.avatarUrl ? 'Cambiar foto' : 'Subir foto'}
                    onError={setError}
                    onElegir={acciones.subirAvatar}
                  />
                  {perfil.avatarUrl && (
                    <button
                      className={s.btnPeligro}
                      onClick={() => acciones.quitarAvatar().catch((e) => avisar(e, 'No se pudo quitar la foto.'))}
                    >
                      Quitar
                    </button>
                  )}
                </div>
              </div>
            </div>
          </section>

          {/* ── Textos ── */}
          <section className={s.tarjeta}>
            <h3 className={s.tarjetaTitulo}>Tu presentación</h3>
            <div className={s.campos}>
              <label className={s.campo}>
                <span>Cómo te presentás</span>
                <input
                  type="text"
                  value={form.titular}
                  maxLength={TOPES.titular}
                  onChange={(e) => setForm({ ...form, titular: e.target.value })}
                  placeholder="Profesor Nacional de Tenis"
                />
              </label>

              <label className={s.campo}>
                <span>Una frase corta (opcional)</span>
                <input
                  type="text"
                  value={form.subtitulo}
                  maxLength={TOPES.subtitulo}
                  onChange={(e) => setForm({ ...form, subtitulo: e.target.value })}
                  placeholder="Formando jugadores en Río Cuarto desde 2010"
                />
              </label>

              <div className={s.campo}>
                <span>Especialidades</span>
                <div className={s.chipsEdit}>
                  {especialidades.map((e) => (
                    <span key={e} className={s.chipEdit}>
                      {e}
                      <button
                        className={s.chipQuitar}
                        onClick={() => setEspecialidades(especialidades.filter((x) => x !== e))}
                        aria-label={`Quitar ${e}`}
                      >
                        ×
                      </button>
                    </span>
                  ))}
                  {especialidades.length < TOPES.especialidades && (
                    <input
                      type="text"
                      value={nuevaEspecialidad}
                      maxLength={30}
                      style={{ width: 190 }}
                      placeholder="Alto rendimiento…"
                      onChange={(e) => setNuevaEspecialidad(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') { e.preventDefault(); agregarEspecialidad(); }
                      }}
                      onBlur={agregarEspecialidad}
                    />
                  )}
                </div>
              </div>

              <label className={s.campo}>
                <span>Quién soy</span>
                <textarea
                  value={form.bio}
                  maxLength={TOPES.bio}
                  onChange={(e) => setForm({ ...form, bio: e.target.value })}
                  placeholder="Doy clases hace 15 años. Me formé como Profesor Nacional y desde entonces…"
                />
                <div className={`${s.contador} ${form.bio.length > TOPES.bio - 100 ? s.contadorLleno : ''}`}>
                  {form.bio.length}/{TOPES.bio}
                </div>
              </label>
            </div>

            <div className={s.pieAcciones}>
              <button className={s.btnPrimario} onClick={() => void guardarTextos()} disabled={guardando}>
                {guardando ? 'Guardando…' : 'Guardar'}
              </button>
              {guardado && <span className={s.guardado}>¡Guardado!</span>}
            </div>
          </section>

          <EditorHitos hitos={perfil.hitos} />
          <EditorGaleria fotos={perfil.fotos} />
        </>
      )}
    </div>
  );
}
