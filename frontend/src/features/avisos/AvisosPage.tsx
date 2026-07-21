import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { fechaCorta } from '../agenda/types';
import s from './AvisosPage.module.css';

/** Espejo de AvisoDto. */
interface Aviso {
  id: string;
  titulo: string;
  mensaje: string;
  venceEl: string | null;
  activo: boolean;
  creadoEl: string;
}

const VACIO = { titulo: '', mensaje: '', venceEl: '' };

/** Hoy en formato ISO (para el min del input date). */
const hoyISO = () => new Date().toISOString().slice(0, 10);

/**
 * Avisos generales del club: el profe deja un mensaje y lo ven TODOS sus alumnos
 * en el Inicio del portal. Puede ponerle vencimiento (se oculta solo) y
 * apagarlo/borrarlo. Distinto de las notas privadas por alumno (van en la ficha).
 */
export default function AvisosPage() {
  const [avisos, setAvisos] = useState<Aviso[]>([]);
  const [cargando, setCargando] = useState(true);
  const [alta, setAlta] = useState(false);
  const [form, setForm] = useState(VACIO);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const confirmar = useConfirmar();

  const cargar = useCallback(() => {
    setCargando(true);
    api.get<Aviso[]>('/avisos')
      .then(setAvisos)
      .catch(() => setAvisos([]))
      .finally(() => setCargando(false));
  }, []);

  useEffect(() => { cargar(); }, [cargar]);

  const crear = async () => {
    if (form.titulo.trim() === '' || form.mensaje.trim() === '') return;
    setGuardando(true);
    setError(null);
    try {
      await api.post('/avisos', {
        titulo: form.titulo.trim(),
        mensaje: form.mensaje.trim(),
        venceEl: form.venceEl || null,
      });
      setForm(VACIO);
      setAlta(false);
      cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear el aviso.');
    } finally {
      setGuardando(false);
    }
  };

  const cambiarActivo = async (a: Aviso) => {
    await api.patch(`/avisos/${a.id}/activo`, { activo: !a.activo });
    cargar();
  };

  const borrar = async (a: Aviso) => {
    if (!(await confirmar({
      titulo: 'Borrar aviso',
      mensaje: `¿Borrar el aviso "${a.titulo}"?`,
      confirmar: 'Borrar',
      peligro: true,
    }))) return;
    await api.delete(`/avisos/${a.id}`);
    cargar();
  };

  const vencido = (a: Aviso) => a.venceEl != null && a.venceEl < hoyISO();

  return (
    <div>
      <div className={s.toolbar}>
        <div className={s.titulo}>
          Avisos que ven todos tus alumnos en el Inicio de su portal. Poné un vencimiento
          y se ocultan solos.
        </div>
        {!alta && <button className={s.btnNuevo} onClick={() => setAlta(true)}>+ Nuevo aviso</button>}
      </div>

      {error && <div className={s.error}>{error}</div>}

      {alta && (
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
            placeholder="Mensaje para tus alumnos…"
            maxLength={1000}
            rows={3}
          />
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
              <button className={s.btnGris} onClick={() => { setAlta(false); setForm(VACIO); setError(null); }}>
                Cancelar
              </button>
              <button
                className={s.btnPrimario}
                disabled={guardando || form.titulo.trim() === '' || form.mensaje.trim() === ''}
                onClick={() => void crear()}
              >
                {guardando ? 'Publicando…' : 'Publicar aviso'}
              </button>
            </div>
          </div>
        </div>
      )}

      {cargando && <div className={s.vacio}>Cargando…</div>}

      {!cargando && avisos.length === 0 && !alta && (
        <div className={s.vacioCard}>
          Todavía no publicaste ningún aviso. Creá uno para avisarles algo a todos tus alumnos.
        </div>
      )}

      {!cargando && avisos.length > 0 && (
        <div className={s.lista}>
          {avisos.map((a) => (
            <div key={a.id} className={a.activo && !vencido(a) ? s.fila : s.filaApagada}>
              <div className={s.cuerpo}>
                <div className={s.filaTitulo}>
                  {a.titulo}
                  {!a.activo && <span className={s.badgeApagado}>Apagado</span>}
                  {a.activo && vencido(a) && <span className={s.badgeVencido}>Vencido</span>}
                </div>
                <div className={s.mensaje}>{a.mensaje}</div>
                <div className={s.meta}>
                  {a.venceEl
                    ? `Vence el ${fechaCorta(a.venceEl)}`
                    : 'Sin vencimiento'}
                </div>
              </div>
              <div className={s.acciones}>
                <button className={s.btnMini} onClick={() => void cambiarActivo(a)}>
                  {a.activo ? 'Apagar' : 'Encender'}
                </button>
                <button className={s.btnMiniRojo} onClick={() => void borrar(a)}>Borrar</button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
