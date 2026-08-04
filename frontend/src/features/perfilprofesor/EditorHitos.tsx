import { useState } from 'react';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { ApiError } from '../../lib/api';
import { TOPES, type HitoTrayectoria } from './types';
import { useEditarPerfil } from './usePerfilProfesor';
import s from './MiPerfil.module.css';

interface Props {
  hitos: HitoTrayectoria[];
}

const VACIO = { anio: '', titulo: '', detalle: '' };

/**
 * La trayectoria del profe como lista de hitos. Se reordena con flechas (no con
 * drag & drop): sin librerías nuevas y funciona igual de bien en el celular.
 */
export default function EditorHitos({ hitos }: Props) {
  const { agregarHito, editarHito, eliminarHito, reordenarHitos } = useEditarPerfil();
  const confirmar = useConfirmar();

  const [form, setForm] = useState(VACIO);
  const [editando, setEditando] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const lleno = hitos.length >= TOPES.hitos;
  const puedeGuardar = form.anio.trim() !== '' && form.titulo.trim() !== '' && !guardando;

  const limpiar = () => { setForm(VACIO); setEditando(null); };

  const guardar = async () => {
    setError(null);
    setGuardando(true);
    try {
      const datos = {
        anio: Number(form.anio),
        titulo: form.titulo.trim(),
        detalle: form.detalle.trim() || null,
      };
      if (editando) await editarHito(editando, datos);
      else await agregarHito(datos);
      limpiar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo guardar el hito.');
    } finally {
      setGuardando(false);
    }
  };

  const empezarAEditar = (h: HitoTrayectoria) => {
    setEditando(h.id);
    setForm({ anio: String(h.anio), titulo: h.titulo, detalle: h.detalle ?? '' });
  };

  const borrar = async (h: HitoTrayectoria) => {
    const ok = await confirmar({
      titulo: 'Borrar este hito',
      mensaje: `Se va a borrar "${h.titulo}" de tu trayectoria.`,
      confirmar: 'Borrar',
      peligro: true,
    });
    if (!ok) return;
    try {
      await eliminarHito(h.id);
      if (editando === h.id) limpiar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo borrar el hito.');
    }
  };

  const mover = async (desde: number, hacia: number) => {
    if (hacia < 0 || hacia >= hitos.length) return;
    const ids = hitos.map((h) => h.id);
    [ids[desde], ids[hacia]] = [ids[hacia], ids[desde]];
    try {
      await reordenarHitos(ids);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo reordenar.');
    }
  };

  return (
    <section className={s.tarjeta}>
      <h3 className={s.tarjetaTitulo}>Mi trayectoria</h3>
      <p className={s.ayuda}>
        Cada hito es un momento de tu carrera: el año, qué fue y —si querés— un detalle.
        Se muestran como una línea de tiempo, en el orden que les des acá.
      </p>

      {error && <div className={s.error}>{error}</div>}

      {hitos.length > 0 && (
        <div className={s.filas}>
          {hitos.map((h, i) => (
            <div key={h.id} className={s.fila}>
              <span className={s.filaAnio}>{h.anio}</span>
              <div className={s.filaTexto}>
                <div className={s.filaTitulo}>{h.titulo}</div>
                {h.detalle && <div className={s.filaDetalle}>{h.detalle}</div>}
              </div>
              <div className={s.filaBotones}>
                <button className={s.btnIcono} onClick={() => void mover(i, i - 1)} disabled={i === 0} aria-label="Subir">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M18 15l-6-6-6 6" />
                  </svg>
                </button>
                <button className={s.btnIcono} onClick={() => void mover(i, i + 1)} disabled={i === hitos.length - 1} aria-label="Bajar">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M6 9l6 6 6-6" />
                  </svg>
                </button>
                <button className={s.btnIcono} onClick={() => empezarAEditar(h)} aria-label="Editar">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z" />
                  </svg>
                </button>
                <button className={s.btnIcono} onClick={() => void borrar(h)} aria-label="Borrar">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
                  </svg>
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {lleno && !editando ? (
        <p className={s.ayuda} style={{ marginTop: 14, marginBottom: 0 }}>
          Llegaste a {TOPES.hitos} hitos. Borrá alguno si querés sumar otro.
        </p>
      ) : (
        <div className={s.campos} style={{ marginTop: hitos.length > 0 ? 16 : 0 }}>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
            <label className={s.campo} style={{ width: 110 }}>
              <span>Año</span>
              <input
                type="number"
                value={form.anio}
                onChange={(e) => setForm({ ...form, anio: e.target.value })}
                placeholder="2015"
              />
            </label>
            <label className={s.campo} style={{ flex: 1, minWidth: 200 }}>
              <span>Qué pasó</span>
              <input
                type="text"
                value={form.titulo}
                maxLength={TOPES.tituloHito}
                onChange={(e) => setForm({ ...form, titulo: e.target.value })}
                placeholder="Profesor Nacional de Tenis"
              />
            </label>
          </div>
          <label className={s.campo}>
            <span>Detalle (opcional)</span>
            <input
              type="text"
              value={form.detalle}
              maxLength={TOPES.detalleHito}
              onChange={(e) => setForm({ ...form, detalle: e.target.value })}
              placeholder="Recibido en la Asociación Argentina de Tenis"
            />
          </label>
          <div className={s.acciones}>
            <button className={s.btnPrimario} onClick={() => void guardar()} disabled={!puedeGuardar}>
              {guardando ? 'Guardando…' : editando ? 'Guardar cambios' : 'Agregar hito'}
            </button>
            {editando && (
              <button className={s.btnSuave} onClick={limpiar} disabled={guardando}>Cancelar</button>
            )}
          </div>
        </div>
      )}
    </section>
  );
}
