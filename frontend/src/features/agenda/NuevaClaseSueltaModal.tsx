import { useEffect, useMemo, useState } from 'react';
import Modal from '../../components/Modal';
import HoraSelect from '../../components/HoraSelect';
import { api, ApiError } from '../../lib/api';
import { formatoPlata, hoyIso } from '../alumnos/types';
import SelectorAlumnos from './SelectorAlumnos';
import type { Sede } from './types';
import { useProfesores } from '../profesores/useProfesores';
import s from '../alumnos/NuevoAlumnoModal.module.css';
import g from './FormHorario.module.css';

interface CanchaLibre {
  canchaId: string;
  cancha: string;
  sede: string;
}

interface Props {
  sedes: Sede[];
  onClose: () => void;
  /** Recarga la agenda: el turno suelto aparece en el día elegido. */
  onCreada: () => void;
}

/**
 * Clase suelta que ASIGNA el profe: una clase individual en una fecha puntual, para el
 * que viene a probar o pide un día extra. Es el espejo de "Nuevo horario", con dos
 * diferencias que importan: no se repite, y **puede no cobrarse** (clase de prueba).
 *
 * Las canchas se piden al backend recién cuando hay club + fecha + hora + duración, y se
 * ofrecen SOLO las libres: elegir una ocupada para que te la rechacen es un viaje al
 * pedo cuando el back ya sabe cuáles están.
 */
export default function NuevaClaseSueltaModal({ sedes, onClose, onCreada }: Props) {
  const [valorClaseIndividual, setValorClaseIndividual] = useState<number | null>(null);
  const [alumnoIds, setAlumnoIds] = useState<string[]>([]);
  const [sedeId, setSedeId] = useState(sedes[0]?.id ?? '');
  const [fecha, setFecha] = useState(hoyIso());
  const [hora, setHora] = useState('18:00');
  const [duracion, setDuracion] = useState(60);
  const [canchaId, setCanchaId] = useState('');
  const [profesorId, setProfesorId] = useState('');
  const [generaCargo, setGeneraCargo] = useState(true);
  const [libres, setLibres] = useState<CanchaLibre[] | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { profes } = useProfesores();

  // El precio de la clase individual (Configuración) es lo que se le cobra. Se pide acá
  // y no desde el Calendario: es un dato de este modal y nadie más lo necesita.
  useEffect(() => {
    api.get<{ valorClaseIndividual: number | null }>('/configuracion/precios')
      .then((p) => setValorClaseIndividual(p.valorClaseIndividual))
      .catch(() => setValorClaseIndividual(null));
  }, []);

  // Canchas libres para esta combinación. Se vuelve a pedir ante cualquier cambio que
  // mueva la ocupación, y mientras tanto la elegida se limpia (podría no seguir libre).
  useEffect(() => {
    if (!sedeId || !fecha) return;
    let vigente = true;
    setLibres(null);
    setCanchaId('');
    const params = new URLSearchParams({
      sedeId, fecha, horaInicio: hora, duracionMinutos: String(duracion),
    });
    api.get<CanchaLibre[]>(`/clases-sueltas/canchas-libres?${params}`)
      .then((cs) => {
        if (!vigente) return;
        setLibres(cs);
        if (cs.length > 0) setCanchaId(cs[0].canchaId);
      })
      .catch(() => { if (vigente) setLibres([]); });
    return () => { vigente = false; };
  }, [sedeId, fecha, hora, duracion]);

  const monto = useMemo(
    () => (valorClaseIndividual == null ? null : Math.round(valorClaseIndividual * duracion / 60)),
    [valorClaseIndividual, duracion],
  );

  const alumnoId = alumnoIds[0];
  const valido = !!alumnoId && canchaId !== '' && (!generaCargo || monto != null);

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await api.post('/clases-sueltas', {
        alumnoId,
        canchaId,
        fecha,
        horaInicio: hora,
        duracionMinutos: duracion,
        profesorUserId: profesorId || undefined,
        generaCargo,
      });
      onCreada();
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo asignar la clase.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Nueva clase suelta"
      subtitulo="Una clase en una fecha puntual: no se repite"
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={() => void guardar()} disabled={enviando || !valido}>
            {enviando ? 'Asignando…' : 'Asignar clase'}
          </button>
        </>
      }
    >
      <div className={g.grid}>
        {/* Solo el dueño llega acá (el endpoint es Owner), así que siempre elige profe.
            Sin profe la clase no le cuenta a nadie para el sueldo. */}
        <label className={s.campo}>
          <span>Profe (opcional)</span>
          <select
            value={profesorId}
            onChange={(e) => {
              const id = e.target.value;
              setProfesorId(id);
              // Como en Nuevo horario: elegir el profe pone su club.
              const p = profes.find((x) => x.userId === id);
              if (p?.sedeId && sedes.some((x) => x.id === p.sedeId)) setSedeId(p.sedeId);
            }}
          >
            <option value="">Sin asignar</option>
            {profes.map((p) => (
              <option key={p.userId} value={p.userId}>{p.nombre}{p.esDueño ? ' (vos)' : ''}</option>
            ))}
          </select>
        </label>

        <label className={s.campo}>
          <span>Club</span>
          <select value={sedeId} onChange={(e) => setSedeId(e.target.value)}>
            {sedes.map((x) => <option key={x.id} value={x.id}>{x.nombre}</option>)}
          </select>
        </label>

        <label className={s.campo}>
          <span>Fecha</span>
          <input type="date" value={fecha} min={hoyIso()} onChange={(e) => setFecha(e.target.value)} />
        </label>
        <label className={s.campo}>
          <span>Hora de inicio</span>
          <HoraSelect value={hora} onChange={setHora} />
        </label>
        <label className={s.campo}>
          <span>Duración (minutos)</span>
          <select value={duracion} onChange={(e) => setDuracion(Number(e.target.value))}>
            {[30, 45, 60, 90, 120].map((m) => <option key={m} value={m}>{m}'</option>)}
          </select>
        </label>
        <label className={s.campo}>
          <span>Cancha</span>
          <select value={canchaId} onChange={(e) => setCanchaId(e.target.value)} disabled={!libres?.length}>
            {libres === null && <option>Buscando canchas libres…</option>}
            {libres?.length === 0 && <option>No hay canchas libres a esa hora</option>}
            {libres?.map((c) => <option key={c.canchaId} value={c.canchaId}>{c.cancha}</option>)}
          </select>
        </label>

        <div className={`${s.campo} ${g.span2}`}>
          <span>Alumno</span>
          {/* cupo 1: es una clase individual. El selector ya ofrece a los de la lista
              de espera, que es justo de donde sale el que viene a probar. */}
          <SelectorAlumnos elegidos={alumnoIds} onCambiar={setAlumnoIds} cupo={1} />
        </div>

        <div className={`${s.campo} ${g.span2}`}>
          <label className={g.switchCargo}>
            <input
              type="checkbox"
              checked={generaCargo}
              onChange={(e) => setGeneraCargo(e.target.checked)}
            />
            <span>
              {generaCargo ? (
                <>
                  <b>Le cobra la clase</b>
                  {monto != null
                    ? <> — {formatoPlata(monto)} se suma a su cuenta</>
                    : <> — falta configurar el precio de la clase individual</>}
                </>
              ) : (
                <><b>Clase de prueba</b> — no se le cobra nada</>
              )}
            </span>
          </label>
        </div>

        {generaCargo && monto == null && (
          <div className={`${g.span2} ${s.error}`}>
            Configurá el valor de la clase individual en Mi academia → Configuración, o
            destildá el cobro para darla como clase de prueba.
          </div>
        )}
        {error && <div className={`${g.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
