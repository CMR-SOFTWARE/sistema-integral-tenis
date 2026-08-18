import { useEffect, useState } from 'react';
import Modal from '../../components/Modal';
import HoraSelect from '../../components/HoraSelect';
import { api, ApiError } from '../../lib/api';
import { hoyIso } from '../alumnos/types';
import { useProfesores } from '../profesores/useProfesores';
import { horaCorta } from './types';
import type { Sede, Turno } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';
import g from './FormHorario.module.css';

interface CanchaLibre {
  canchaId: string;
  cancha: string;
  sede: string;
}

interface Props {
  turno: Turno;
  sedes: Sede[];
  onClose: () => void;
  /** Recarga la agenda: el turno reprogramado aparece en su nuevo horario. */
  onEditada: () => void;
}

/**
 * Reprograma una clase suelta ya asignada: solo lo agendable (fecha, hora, cancha,
 * duración, profe) — espejo reducido de NuevaClaseSueltaModal, sin Alumno ni el
 * switch de cobro (eso no se toca al editar).
 *
 * El `Turno` no viaja con canchaId/sedeId (solo los nombres, para las 5 pantallas
 * que lo leen): la sede inicial se resuelve por nombre, y la cancha actual se
 * reconoce dentro de las "libres" una vez que llegan (mismo criterio que ya usa el
 * filtro por sede de CalendarioPage).
 */
export default function EditarClaseSueltaModal({ turno, sedes, onClose, onEditada }: Props) {
  const [sedeId, setSedeId] = useState(
    () => sedes.find((x) => x.nombre === turno.sede)?.id ?? sedes[0]?.id ?? '',
  );
  const [fecha, setFecha] = useState(turno.fecha);
  const [hora, setHora] = useState(horaCorta(turno.horaInicio));
  const [duracion, setDuracion] = useState(turno.duracionMinutos);
  const [canchaId, setCanchaId] = useState('');
  const [profesorId, setProfesorId] = useState(turno.profesorUserId ?? '');
  const [libres, setLibres] = useState<CanchaLibre[] | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { profes } = useProfesores();

  // Canchas libres para esta combinación, excluyendo la ocupación del propio turno
  // (si no, se marcaría a sí mismo como "ocupado" y nunca se podría guardar sin
  // cambiar de cancha/hora).
  useEffect(() => {
    if (!sedeId || !fecha) return;
    let vigente = true;
    setLibres(null);
    const params = new URLSearchParams({
      sedeId, fecha, horaInicio: hora, duracionMinutos: String(duracion), excluirTurnoId: turno.id,
    });
    api.get<CanchaLibre[]>(`/clases-sueltas/canchas-libres?${params}`)
      .then((cs) => {
        if (!vigente) return;
        setLibres(cs);
        setCanchaId((actual) => {
          if (actual && cs.some((c) => c.canchaId === actual)) return actual;
          return cs.find((c) => c.cancha === turno.cancha)?.canchaId ?? cs[0]?.canchaId ?? '';
        });
      })
      .catch(() => { if (vigente) setLibres([]); });
    return () => { vigente = false; };
  }, [sedeId, fecha, hora, duracion, turno.id, turno.cancha]);

  const valido = canchaId !== '';

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await api.put(`/clases-sueltas/turno/${turno.id}`, {
        canchaId,
        fecha,
        horaInicio: hora,
        duracionMinutos: duracion,
        profesorUserId: profesorId || undefined,
      });
      onEditada();
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo reprogramar la clase.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Editar clase suelta"
      subtitulo="Cambia la agenda: no toca al alumno ni el cobro"
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={() => void guardar()} disabled={enviando || !valido}>
            {enviando ? 'Guardando…' : 'Guardar cambios'}
          </button>
        </>
      }
    >
      <div className={g.grid}>
        <label className={s.campo}>
          <span>Profe (opcional)</span>
          <select value={profesorId} onChange={(e) => setProfesorId(e.target.value)}>
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

        {error && <div className={`${g.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
