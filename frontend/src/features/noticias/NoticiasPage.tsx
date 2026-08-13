import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { fechaCorta } from '../agenda/types';
import s from './NoticiasPage.module.css';

/** Espejo de NoticiaDto. */
interface Noticia {
  id: string;
  titulo: string;
  mensaje: string;
  importante: boolean;
  venceEl: string | null;
  activo: boolean;
  creadoEl: string;
}

const VACIO = { titulo: '', mensaje: '', importante: false, venceEl: '' };
type Form = typeof VACIO;

/** Hoy en formato ISO (para el min del input date). */
const hoyISO = () => new Date().toISOString().slice(0, 10);

/**
 * Noticias del club: el director publica algo y lo ven TODOS sus alumnos. Las
 * **importantes** suben al Inicio del portal, bien visibles; las demás viven en la
 * sección Noticias del alumno. Puede ponerles vencimiento (se ocultan solas),
 * editarlas, apagarlas y borrarlas. Distinto de las notas privadas por alumno,
 * que van en la ficha.
 */
export default function NoticiasPage() {
  const [noticias, setNoticias] = useState<Noticia[]>([]);
  const [cargando, setCargando] = useState(true);
  const [form, setForm] = useState<Form | null>(null); // null = el formulario está cerrado
  const [editandoId, setEditandoId] = useState<string | null>(null); // null = alta
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const confirmar = useConfirmar();

  const cargar = useCallback(() => {
    setCargando(true);
    api.get<Noticia[]>('/noticias')
      .then(setNoticias)
      .catch(() => setNoticias([]))
      .finally(() => setCargando(false));
  }, []);

  useEffect(() => { cargar(); }, [cargar]);

  const cerrarForm = () => { setForm(null); setEditandoId(null); setError(null); };

  const abrirAlta = () => { setForm(VACIO); setEditandoId(null); setError(null); };

  const abrirEdicion = (n: Noticia) => {
    setForm({
      titulo: n.titulo,
      mensaje: n.mensaje,
      importante: n.importante,
      venceEl: n.venceEl ?? '',
    });
    setEditandoId(n.id);
    setError(null);
  };

  /** El mismo formulario publica y corrige: cambia el verbo y el endpoint. */
  const guardar = async () => {
    if (form === null || form.titulo.trim() === '' || form.mensaje.trim() === '') return;
    setGuardando(true);
    setError(null);
    const cuerpo = {
      titulo: form.titulo.trim(),
      mensaje: form.mensaje.trim(),
      importante: form.importante,
      venceEl: form.venceEl || null,
    };
    try {
      if (editandoId) await api.put(`/noticias/${editandoId}`, cuerpo);
      else await api.post('/noticias', cuerpo);
      cerrarForm();
      cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo guardar la noticia.');
    } finally {
      setGuardando(false);
    }
  };

  const cambiarActivo = async (n: Noticia) => {
    await api.patch(`/noticias/${n.id}/activo`, { activo: !n.activo });
    cargar();
  };

  const borrar = async (n: Noticia) => {
    if (!(await confirmar({
      titulo: 'Borrar noticia',
      mensaje: `¿Borrar "${n.titulo}"? Esto no se puede deshacer.`,
      confirmar: 'Borrar',
      peligro: true,
    }))) return;
    await api.delete(`/noticias/${n.id}`);
    cargar();
  };

  const vencida = (n: Noticia) => n.venceEl != null && n.venceEl < hoyISO();
  const incompleto = form === null || form.titulo.trim() === '' || form.mensaje.trim() === '';

  return (
    <div>
      <div className={s.toolbar}>
        <div className={s.titulo}>
          Lo que publiques acá lo ven todos tus alumnos. Marcá una como <b>importante</b> y
          les aparece primero en el Inicio; el resto queda en su sección Noticias.
        </div>
        {form === null && (
          <button className={s.btnNuevo} onClick={abrirAlta}>+ Nueva noticia</button>
        )}
      </div>

      {error && <div className={s.error}>{error}</div>}

      {form !== null && (
        <div className={s.altaCard}>
          <input
            className={s.input}
            value={form.titulo}
            onChange={(e) => setForm({ ...form, titulo: e.target.value })}
            placeholder="Título (ej: Sin clases el viernes)"
            maxLength={100}
          />
          <textarea
            className={s.textarea}
            value={form.mensaje}
            onChange={(e) => setForm({ ...form, mensaje: e.target.value })}
            placeholder="Contale a tus alumnos de qué se trata…"
            maxLength={1000}
            rows={3}
          />
          <label className={s.importante}>
            <input
              type="checkbox"
              checked={form.importante}
              onChange={(e) => setForm({ ...form, importante: e.target.checked })}
            />
            <span>
              <b>Importante</b> — aparece primero en el Inicio de tus alumnos
            </span>
          </label>
          <div className={s.altaPie}>
            <label className={s.vence}>
              Vence (opcional)
              <input
                type="date"
                className={s.inputFecha}
                value={form.venceEl}
                min={hoyISO()}
                onChange={(e) => setForm({ ...form, venceEl: e.target.value })}
              />
            </label>
            <div className={s.altaAcciones}>
              <button className={s.btnGris} onClick={cerrarForm}>Cancelar</button>
              <button className={s.btnPrimario} disabled={guardando || incompleto} onClick={() => void guardar()}>
                {guardando
                  ? 'Guardando…'
                  : editandoId ? 'Guardar cambios' : 'Publicar noticia'}
              </button>
            </div>
          </div>
        </div>
      )}

      {cargando && <div className={s.vacio}>Cargando…</div>}

      {!cargando && noticias.length === 0 && form === null && (
        <div className={s.vacioCard}>
          Todavía no publicaste ninguna noticia. Creá una para contarle algo a todos tus alumnos.
        </div>
      )}

      {!cargando && noticias.length > 0 && (
        <div className={s.lista}>
          {noticias.map((n) => (
            <div key={n.id} className={n.activo && !vencida(n) ? s.fila : s.filaApagada}>
              <div className={s.cuerpo}>
                <div className={s.filaTitulo}>
                  {n.titulo}
                  {n.importante && <span className={s.badgeImportante}>Importante</span>}
                  {!n.activo && <span className={s.badgeApagado}>Apagada</span>}
                  {n.activo && vencida(n) && <span className={s.badgeVencido}>Vencida</span>}
                </div>
                <div className={s.mensaje}>{n.mensaje}</div>
                <div className={s.meta}>
                  {n.venceEl ? `Vence el ${fechaCorta(n.venceEl)}` : 'Sin vencimiento'}
                </div>
              </div>
              <div className={s.acciones}>
                <button className={s.btnMini} onClick={() => abrirEdicion(n)}>Editar</button>
                <button className={s.btnMini} onClick={() => void cambiarActivo(n)}>
                  {n.activo ? 'Apagar' : 'Encender'}
                </button>
                <button className={s.btnMiniRojo} onClick={() => void borrar(n)}>Borrar</button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
