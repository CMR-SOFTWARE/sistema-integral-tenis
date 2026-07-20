import { useCallback, useEffect, useMemo, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import Modal from '../../components/Modal';
import HoraSelect from '../../components/HoraSelect';
import { obtenerSesion } from '../auth/sesion';
import SinClub from './SinClub';
import { formatoPlata, CAT_LABEL } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import { aISO, fechaCorta, horaCorta, lunesDe, rangoSemana, sumarDias } from '../agenda/types';
import type { Disponibilidad, GrupoDisponible, SedeReserva, SolicitudGrupo, SolicitudHorario } from './types';
import s from './ReservarPage.module.css';

const DIA_IDX: Record<string, number> = {
  Monday: 0, Tuesday: 1, Wednesday: 2, Thursday: 3, Friday: 4, Saturday: 5, Sunday: 6,
};
const DIAS_ES = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'];
const DIAS_OPCIONES = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

const ESTADO_SOL: Record<string, { label: string; cls: string }> = {
  Pendiente: { label: 'Esperando al profe', cls: s.chipAzul },
  Aceptada: { label: 'Aceptada ✓', cls: s.chipVerde },
  Rechazada: { label: 'Rechazada', cls: s.chipRojo },
};

const hhmm = (t: string) => t.slice(0, 5);

interface SlotGrupo {
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

/** Reservar: calendario semanal con grupos con cupo + pedir clase individual (M5a/M5b). */
export default function ReservarPage() {
  const conClub = obtenerSesion()?.alumno != null;
  const [lunes, setLunes] = useState(() => lunesDe(new Date()));
  const [grupos, setGrupos] = useState<GrupoDisponible[]>([]);
  const [solGrupo, setSolGrupo] = useState<SolicitudGrupo[]>([]);
  const [solHorario, setSolHorario] = useState<SolicitudHorario[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [confirmar, setConfirmar] = useState<SlotGrupo | null>(null);
  const [pidiendoIndividual, setPidiendoIndividual] = useState(false);
  const [sedes, setSedes] = useState<SedeReserva[]>([]);
  const [formInd, setFormInd] = useState({ sedeId: '', dia: 'Monday', hora: '18:00', duracion: 60 });
  const [disp, setDisp] = useState<Disponibilidad | null>(null);
  const [chequeando, setChequeando] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  const cargar = useCallback(() => {
    if (!conClub) return;
    setCargando(true);
    setError(null);
    Promise.all([
      api.get<GrupoDisponible[]>('/portal/grupos-disponibles'),
      api.get<SolicitudGrupo[]>('/portal/solicitudes-grupo'),
      api.get<SolicitudHorario[]>('/portal/solicitudes-horario'),
    ])
      .then(([g, sg, sh]) => { setGrupos(g); setSolGrupo(sg); setSolHorario(sh); })
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando la agenda'))
      .finally(() => setCargando(false));
  }, [conClub]);

  useEffect(() => { cargar(); }, [cargar]);

  // Las sedes del club (para elegir dónde); preselecciona la primera
  useEffect(() => {
    if (!conClub) return;
    api.get<SedeReserva[]>('/portal/sedes')
      .then((ss) => {
        setSedes(ss);
        if (ss.length > 0) setFormInd((f) => ({ ...f, sedeId: f.sedeId || ss[0].id }));
      })
      .catch(() => setSedes([]));
  }, [conClub]);

  // Chequeo en vivo de disponibilidad mientras se arma la clase individual
  useEffect(() => {
    if (!pidiendoIndividual || !formInd.sedeId) return;
    setChequeando(true); setDisp(null);
    const t = setTimeout(() => {
      const q = `sede=${formInd.sedeId}&dia=${formInd.dia}&hora=${formInd.hora}&duracion=${formInd.duracion}`;
      api.get<Disponibilidad>(`/portal/hay-lugar?${q}`)
        .then(setDisp)
        .catch(() => setDisp(null))
        .finally(() => setChequeando(false));
    }, 300);
    return () => clearTimeout(t);
  }, [pidiendoIndividual, formInd]);

  const sedeNombre = sedes.find((x) => x.id === formInd.sedeId)?.nombre ?? '';

  const slots = useMemo<SlotGrupo[]>(() =>
    grupos.flatMap((g) =>
      g.horarios.map((h) => ({
        grupoId: g.grupoId, grupoNombre: g.nombre, categoria: g.categoria,
        diaIdx: DIA_IDX[h.dia] ?? 0, horaInicio: h.horaInicio, duracionMinutos: h.duracionMinutos,
        cancha: h.cancha, sede: h.sede, precioEstimado: h.precioEstimado,
        miembros: g.miembrosActivos, cupo: g.cupoMaximo, pendiente: g.solicitudPendiente,
      })),
    ), [grupos]);

  const dias = useMemo(() => Array.from({ length: 7 }, (_, i) => sumarDias(lunes, i)), [lunes]);
  const esHoy = useMemo(() => lunesDe(new Date()) === lunes, [lunes]);

  if (!conClub) return <SinClub mensaje="Cuando estés en un club vas a poder reservar tus clases acá." />;

  const pedirGrupo = async () => {
    if (!confirmar) return;
    setEnviando(true); setError(null);
    try {
      await api.post('/portal/solicitudes-grupo', { grupoId: confirmar.grupoId });
      setToast(`Pediste sumarte a ${confirmar.grupoNombre}. Tu profe lo va a aprobar.`);
      setTimeout(() => setToast(null), 3500);
      setConfirmar(null); cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo enviar la solicitud.');
    } finally { setEnviando(false); }
  };

  const pedirIndividual = async () => {
    setEnviando(true); setError(null);
    try {
      await api.post('/portal/solicitudes-horario', {
        sedeId: formInd.sedeId,
        dia: formInd.dia,
        horaInicio: `${formInd.hora}:00`,
        duracionMinutos: formInd.duracion,
      });
      setToast('Pediste tu clase individual. Tu profe la va a aprobar y asignar cancha.');
      setTimeout(() => setToast(null), 3500);
      setPidiendoIndividual(false); cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo enviar el pedido.');
    } finally { setEnviando(false); }
  };

  return (
    <div>
      <div className={s.toolbar}>
        <button className={s.nav} onClick={() => setLunes(sumarDias(lunes, -7))}>‹</button>
        <div className={s.rango}>
          Semana del {rangoSemana(lunes)}
          {!esHoy && <button className={s.hoy} onClick={() => setLunes(lunesDe(new Date()))}>volver a hoy</button>}
        </div>
        <button className={s.nav} onClick={() => setLunes(sumarDias(lunes, 7))}>›</button>
        <div style={{ flex: 1 }} />
        <button className={s.btnIndividual} onClick={() => { setError(null); setPidiendoIndividual(true); }}>
          + Pedir clase individual
        </button>
      </div>

      {error && <div className={s.error}>{error}</div>}
      {toast && <div className={s.toast}>{toast}</div>}
      {cargando && <div className={s.vacio}>Cargando la agenda…</div>}

      {!cargando && (
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
                    key={`g-${j}`}
                    className={`${s.slot} ${sl.pendiente ? s.slotPendiente : ''}`}
                    disabled={sl.pendiente}
                    onClick={() => { setError(null); setConfirmar(sl); }}
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

      {/* ── Mis solicitudes (grupo + individual) ── */}
      {(solGrupo.length > 0 || solHorario.length > 0) && (
        <>
          <h2 className={s.seccion}>Mis solicitudes</h2>
          <div className={s.tarjeta}>
            {solGrupo.map((sol) => {
              const e = ESTADO_SOL[sol.estado];
              return (
                <div key={`g${sol.id}`} className={s.solFila}>
                  <span className={s.solNombre}>{sol.grupoNombre}</span>
                  <span className={`${s.chip} ${e.cls}`}>{e.label}</span>
                </div>
              );
            })}
            {solHorario.map((sol) => {
              const e = ESTADO_SOL[sol.estado];
              const idx = DIA_IDX[sol.dia] ?? 0;
              return (
                <div key={`h${sol.id}`} className={s.solFila}>
                  <span className={s.solNombre}>
                    Clase individual · {sol.sede} · {DIAS_ES[idx]} {hhmm(sol.horaInicio)}
                    {sol.cancha ? ` · ${sol.cancha}` : ''}
                  </span>
                  <span className={`${s.chip} ${e.cls}`}>{e.label}</span>
                </div>
              );
            })}
          </div>
        </>
      )}

      {/* ── Próximamente: clase suelta ── */}
      <h2 className={s.seccion}>Otras opciones</h2>
      <div className={s.tarjeta}>
        <div className={s.prox}>
          <span><b>Clase suelta</b> — una clase para probar, la pagás en el momento.</span>
          <span className={`${s.chip} ${s.chipGris}`}>Próximamente</span>
        </div>
      </div>

      {/* ── Modal: sumarme a un grupo ── */}
      {confirmar && (
        <Modal
          titulo="Pedir sumarme al grupo"
          subtitulo={confirmar.grupoNombre}
          onClose={() => { setConfirmar(null); setError(null); }}
          ancho={420}
          footer={
            <>
              <button className={s.btnCancelar} onClick={() => { setConfirmar(null); setError(null); }}>Cancelar</button>
              <button className={s.btnConfirmar} onClick={() => void pedirGrupo()} disabled={enviando}>
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
          {error && <div className={s.errorModal}>{error}</div>}
          <p className={s.nota}>El profe tiene que aprobar tu pedido antes de sumarte al grupo.</p>
        </Modal>
      )}

      {/* ── Modal: pedir clase individual ── */}
      {pidiendoIndividual && (
        <Modal
          titulo="Pedir clase individual fija"
          subtitulo="Un horario propio, todas las semanas"
          onClose={() => { setPidiendoIndividual(false); setError(null); }}
          ancho={420}
          footer={
            <>
              <button className={s.btnCancelar} onClick={() => { setPidiendoIndividual(false); setError(null); }}>Cancelar</button>
              <button
                className={s.btnConfirmar}
                onClick={() => void pedirIndividual()}
                disabled={enviando || chequeando || (disp !== null && !disp.hayLugar)}
              >
                {enviando ? 'Enviando…' : 'Pedir clase'}
              </button>
            </>
          }
        >
          <div className={s.formInd}>
            {sedes.length > 1 && (
              <label className={s.campo}>
                <span>Sede</span>
                <select value={formInd.sedeId} onChange={(e) => setFormInd((f) => ({ ...f, sedeId: e.target.value }))}>
                  {sedes.map((se) => <option key={se.id} value={se.id}>{se.nombre}</option>)}
                </select>
              </label>
            )}
            <label className={s.campo}>
              <span>Día</span>
              <select value={formInd.dia} onChange={(e) => setFormInd((f) => ({ ...f, dia: e.target.value }))}>
                {DIAS_OPCIONES.map((d, i) => <option key={d} value={d}>{DIAS_ES[i]}</option>)}
              </select>
            </label>
            <label className={s.campo}>
              <span>Hora</span>
              <HoraSelect value={formInd.hora} onChange={(h) => setFormInd((f) => ({ ...f, hora: h }))} />
            </label>
            <label className={s.campo}>
              <span>Duración</span>
              <select value={formInd.duracion} onChange={(e) => setFormInd((f) => ({ ...f, duracion: Number(e.target.value) }))}>
                <option value={30}>30 minutos</option>
                <option value={60}>1 hora</option>
                <option value={90}>1 hora 30</option>
                <option value={120}>2 horas</option>
              </select>
            </label>
          </div>

          {chequeando && <div className={s.dispChequeando}>Chequeando disponibilidad…</div>}
          {!chequeando && disp?.hayLugar && (
            <div className={s.dispOk}>✓ Hay lugar{sedeNombre ? ` en ${sedeNombre}` : ''} a esa hora</div>
          )}
          {!chequeando && disp !== null && !disp.hayLugar && (
            <div className={s.dispNo}>✗ No hay lugar{sedeNombre ? ` en ${sedeNombre}` : ''} a esa hora — probá otro horario</div>
          )}

          {error && <div className={s.errorModal}>{error}</div>}
          <p className={s.nota}>El profe revisa la cancha, la asigna y aprueba tu horario.</p>
        </Modal>
      )}
    </div>
  );
}
