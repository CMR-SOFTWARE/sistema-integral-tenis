import { useCallback, useEffect, useMemo, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import Modal from '../../components/Modal';
import { obtenerSesion } from '../auth/sesion';
import SinClub from './SinClub';
import { formatoPlata, CAT_LABEL } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import { aISO, fechaCorta, horaCorta, lunesDe, rangoSemana, sumarDias } from '../agenda/types';
import type { GrupoDisponible, SolicitudGrupo } from './types';
import s from './ReservarPage.module.css';

/** DiaSemana (inglés, como lo manda el back) → índice de columna Lun=0..Dom=6. */
const DIA_IDX: Record<string, number> = {
  Monday: 0, Tuesday: 1, Wednesday: 2, Thursday: 3, Friday: 4, Saturday: 5, Sunday: 6,
};

const ESTADO_SOL: Record<SolicitudGrupo['estado'], { label: string; cls: string }> = {
  Pendiente: { label: 'Esperando al profe', cls: s.chipAzul },
  Aceptada: { label: 'Aceptada ✓', cls: s.chipVerde },
  Rechazada: { label: 'Rechazada', cls: s.chipRojo },
};

/** Un slot del calendario: un horario de un grupo al que me puedo sumar. */
interface Slot {
  grupoId: string;
  grupoNombre: string;
  categoria: string | null;
  diaIdx: number;
  horaInicio: string;
  duracionMinutos: number;
  cancha: string;
  sede: string;
  precioEstimado: number | null;
  miembros: number;
  cupo: number | null;
  pendiente: boolean;
}

/** Reservar: calendario semanal con los grupos con cupo (M5a). */
export default function ReservarPage() {
  const conClub = obtenerSesion()?.alumno != null;
  const [lunes, setLunes] = useState(() => lunesDe(new Date()));
  const [grupos, setGrupos] = useState<GrupoDisponible[]>([]);
  const [solicitudes, setSolicitudes] = useState<SolicitudGrupo[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [confirmar, setConfirmar] = useState<Slot | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  const cargar = useCallback(() => {
    if (!conClub) return;
    setCargando(true);
    setError(null);
    Promise.all([
      api.get<GrupoDisponible[]>('/portal/grupos-disponibles'),
      api.get<SolicitudGrupo[]>('/portal/solicitudes-grupo'),
    ])
      .then(([g, sol]) => { setGrupos(g); setSolicitudes(sol); })
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando los grupos'))
      .finally(() => setCargando(false));
  }, [conClub]);

  useEffect(() => { cargar(); }, [cargar]);

  // Grupo (recurrente) → un slot por cada uno de sus horarios
  const slots = useMemo<Slot[]>(() =>
    grupos.flatMap((g) =>
      g.horarios.map((h) => ({
        grupoId: g.grupoId,
        grupoNombre: g.nombre,
        categoria: g.categoria,
        diaIdx: DIA_IDX[h.dia] ?? 0,
        horaInicio: h.horaInicio,
        duracionMinutos: h.duracionMinutos,
        cancha: h.cancha,
        sede: h.sede,
        precioEstimado: h.precioEstimado,
        miembros: g.miembrosActivos,
        cupo: g.cupoMaximo,
        pendiente: g.solicitudPendiente,
      })),
    ), [grupos]);

  const dias = useMemo(() => Array.from({ length: 7 }, (_, i) => sumarDias(lunes, i)), [lunes]);
  const esHoy = useMemo(() => lunesDe(new Date()) === lunes, [lunes]);

  if (!conClub) return <SinClub mensaje="Cuando estés en un club vas a poder reservar tus clases acá." />;

  const pedir = async () => {
    if (!confirmar) return;
    setEnviando(true);
    setError(null);
    try {
      await api.post('/portal/solicitudes-grupo', { grupoId: confirmar.grupoId });
      setToast(`Pediste sumarte a ${confirmar.grupoNombre}. Tu profe lo va a aprobar.`);
      setTimeout(() => setToast(null), 3500);
      setConfirmar(null);
      cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo enviar la solicitud.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <div>
      <div className={s.toolbar}>
        <button className={s.nav} onClick={() => setLunes(sumarDias(lunes, -7))}>‹</button>
        <div className={s.rango}>
          Semana del {rangoSemana(lunes)}
          {!esHoy && (
            <button className={s.hoy} onClick={() => setLunes(lunesDe(new Date()))}>volver a hoy</button>
          )}
        </div>
        <button className={s.nav} onClick={() => setLunes(sumarDias(lunes, 7))}>›</button>
      </div>

      {error && <div className={s.error}>{error}</div>}
      {toast && <div className={s.toast}>{toast}</div>}
      {cargando && <div className={s.vacio}>Buscando grupos…</div>}

      {!cargando && slots.length === 0 && (
        <div className={s.vacioCard}>No hay grupos con lugar para tu categoría por ahora.</div>
      )}

      {!cargando && slots.length > 0 && (
        <div className={s.grilla}>
          {dias.map((fecha, i) => {
            const delDia = slots
              .filter((sl) => sl.diaIdx === i)
              .sort((a, b) => a.horaInicio.localeCompare(b.horaInicio));
            const hoyCol = fecha === aISO(new Date());
            return (
              <div key={fecha} className={`${s.columna} ${hoyCol ? s.columnaHoy : ''}`}>
                <div className={s.columnaTitulo}>{fechaCorta(fecha)}</div>
                {delDia.length === 0 && <div className={s.libre}>—</div>}
                {delDia.map((sl, j) => (
                  <button
                    key={`${sl.grupoId}-${j}`}
                    className={`${s.slot} ${sl.pendiente ? s.slotPendiente : ''}`}
                    disabled={sl.pendiente}
                    onClick={() => setConfirmar(sl)}
                  >
                    <div className={s.slotHora}>{horaCorta(sl.horaInicio)}</div>
                    <div className={s.slotTitulo}>{sl.grupoNombre}</div>
                    <div className={s.slotDetalle}>
                      {sl.miembros}{sl.cupo ? `/${sl.cupo}` : ''} · {sl.cancha || 'cancha'}
                    </div>
                    {sl.precioEstimado != null && (
                      <div className={s.slotPrecio}>≈ {formatoPlata(sl.precioEstimado)}/clase</div>
                    )}
                    {sl.pendiente && <div className={s.slotBadge}>Pedido ✓</div>}
                  </button>
                ))}
              </div>
            );
          })}
        </div>
      )}

      {/* ── Mis solicitudes ── */}
      {solicitudes.length > 0 && (
        <>
          <h2 className={s.seccion}>Mis solicitudes</h2>
          <div className={s.tarjeta}>
            {solicitudes.map((sol) => {
              const e = ESTADO_SOL[sol.estado];
              return (
                <div key={sol.id} className={s.solFila}>
                  <span className={s.solNombre}>{sol.grupoNombre}</span>
                  <span className={`${s.chip} ${e.cls}`}>{e.label}</span>
                </div>
              );
            })}
          </div>
        </>
      )}

      {/* ── Próximamente ── */}
      <h2 className={s.seccion}>Otras opciones</h2>
      <div className={s.tarjeta}>
        <div className={s.prox}>
          <span><b>Clase individual fija</b> — un horario propio, todas las semanas.</span>
          <span className={`${s.chip} ${s.chipGris}`}>Próximamente</span>
        </div>
        <div className={s.prox}>
          <span><b>Clase suelta</b> — una clase para probar, la pagás en el momento.</span>
          <span className={`${s.chip} ${s.chipGris}`}>Próximamente</span>
        </div>
      </div>

      {confirmar && (
        <Modal
          titulo="Pedir sumarme al grupo"
          subtitulo={confirmar.grupoNombre}
          onClose={() => setConfirmar(null)}
          ancho={420}
          footer={
            <>
              <button className={s.btnCancelar} onClick={() => setConfirmar(null)}>Cancelar</button>
              <button className={s.btnConfirmar} onClick={() => void pedir()} disabled={enviando}>
                {enviando ? 'Enviando…' : 'Pedir sumarme'}
              </button>
            </>
          }
        >
          <div className={s.resumen}>
            <div className={s.resumenFila}>
              <span>Día y hora</span>
              <b>{fechaCorta(sumarDias(lunes, confirmar.diaIdx))} · {horaCorta(confirmar.horaInicio)} ({confirmar.duracionMinutos}')</b>
            </div>
            <div className={s.resumenFila}>
              <span>Cancha</span>
              <b>{confirmar.cancha || '—'}{confirmar.sede ? ` · ${confirmar.sede}` : ''}</b>
            </div>
            {confirmar.categoria && confirmar.categoria !== 'SinCategoria' && (
              <div className={s.resumenFila}>
                <span>Categoría</span>
                <b>{CAT_LABEL[confirmar.categoria as Categoria] ?? confirmar.categoria}</b>
              </div>
            )}
            {confirmar.precioEstimado != null && (
              <div className={s.resumenFila}>
                <span>Precio estimado</span>
                <b>≈ {formatoPlata(confirmar.precioEstimado)}/clase</b>
              </div>
            )}
          </div>
          <p className={s.nota}>El profe tiene que aprobar tu pedido antes de sumarte al grupo.</p>
        </Modal>
      )}
    </div>
  );
}
