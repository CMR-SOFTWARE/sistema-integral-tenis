import { useEffect, useMemo, useState } from 'react';
import Modal from '../../components/Modal';
import HoraSelect from '../../components/HoraSelect';
import { api, ApiError } from '../../lib/api';
import type { Alumno } from '../alumnos/types';
import type { Grupo } from '../grupos/types';
import { DIAS, horaCorta } from './types';
import type { CreateHorario, DiaSemana, Horario, Sede } from './types';
import { useProfesores } from '../profesores/useProfesores';
import s from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  sedes: Sede[];
  onClose: () => void;
  onCrear: (dto: CreateHorario) => Promise<void>;
  /** Al duplicar: horario del que se copian los datos (roster incluido). */
  base?: Horario;
}

/** Alta de horario recurrente: cancha + (grupo XOR alumno) + día/hora/duración. */
export default function NuevoHorarioModal({ sedes, onClose, onCrear, base }: Props) {
  // Al duplicar, los estados arrancan con los datos del horario base.
  const sedeInicial = base
    ? sedes.find((x) => x.canchas.some((c) => c.id === base.canchaId))?.id ?? sedes[0]?.id ?? ''
    : sedes[0]?.id ?? '';
  const [sedeId, setSedeId] = useState(sedeInicial);
  const [canchaId, setCanchaId] = useState(base?.canchaId ?? '');
  const [tipo, setTipo] = useState<'grupo' | 'individual'>(base?.esIndividual ? 'individual' : 'grupo');
  const [grupoId, setGrupoId] = useState(base && !base.esIndividual ? base.grupoId ?? '' : '');
  const [alumnoId, setAlumnoId] = useState(base?.esIndividual ? base.alumnoId ?? '' : '');
  const [dia, setDia] = useState<DiaSemana>(base?.dia ?? 'Monday');
  const [hora, setHora] = useState(base ? horaCorta(base.horaInicio) : '18:00');
  const [duracion, setDuracion] = useState(base?.duracionMinutos ?? 60);
  const [profesorId, setProfesorId] = useState(base?.profesorUserId ?? '');
  const [valorHora, setValorHora] = useState(base?.valorHoraProfe?.toString() ?? '');
  const { profes } = useProfesores();
  const [grupos, setGrupos] = useState<Grupo[]>([]);
  const [alumnos, setAlumnos] = useState<Alumno[]>([]);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Opciones para el roster: grupos y alumnos activos
  useEffect(() => {
    void api.get<Grupo[]>('/grupos').then(setGrupos);
    void api.get<Alumno[]>('/alumnos?estado=Activo').then(setAlumnos);
  }, []);

  const canchas = useMemo(
    () => sedes.find((x) => x.id === sedeId)?.canchas ?? [],
    [sedes, sedeId],
  );

  const valido =
    canchaId !== '' && (tipo === 'grupo' ? grupoId !== '' : alumnoId !== '');

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onCrear({
        canchaId,
        grupoId: tipo === 'grupo' ? grupoId : undefined,
        alumnoId: tipo === 'individual' ? alumnoId : undefined,
        profesorUserId: profesorId || undefined,
        valorHoraProfe: profesorId && valorHora ? Number(valorHora) : undefined,
        dia,
        horaInicio: hora, // "18:00" — TimeOnly lo parsea
        duracionMinutos: duracion,
      });
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear el horario.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo={base ? 'Duplicar horario' : 'Nuevo horario'}
      subtitulo={base ? 'Copiá la clase y cambiale el día/hora' : 'Plantilla semanal: se repite toda la temporada'}
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={() => void guardar()} disabled={enviando || !valido}>
            {enviando ? 'Creando…' : 'Crear horario'}
          </button>
        </>
      }
    >
      <div className={s.grid}>
        <label className={s.campo}>
          <span>Sede</span>
          <select value={sedeId} onChange={(e) => { setSedeId(e.target.value); setCanchaId(''); }}>
            {sedes.map((x) => <option key={x.id} value={x.id}>{x.nombre}</option>)}
          </select>
        </label>
        <label className={s.campo}>
          <span>Cancha</span>
          <select value={canchaId} onChange={(e) => setCanchaId(e.target.value)}>
            <option value="">Elegí una cancha…</option>
            {canchas.map((c) => <option key={c.id} value={c.id}>{c.nombre}</option>)}
          </select>
        </label>

        <label className={s.campo}>
          <span>Tipo de clase</span>
          <select value={tipo} onChange={(e) => setTipo(e.target.value as 'grupo' | 'individual')}>
            <option value="grupo">Grupal (grupo fijo)</option>
            <option value="individual">Individual (un alumno)</option>
          </select>
        </label>
        {tipo === 'grupo' ? (
          <label className={s.campo}>
            <span>Grupo</span>
            <select value={grupoId} onChange={(e) => setGrupoId(e.target.value)}>
              <option value="">Elegí un grupo…</option>
              {grupos.map((g) => (
                <option key={g.id} value={g.id}>{g.nombre} ({g.miembrosActivos})</option>
              ))}
            </select>
          </label>
        ) : (
          <label className={s.campo}>
            <span>Alumno</span>
            <select value={alumnoId} onChange={(e) => setAlumnoId(e.target.value)}>
              <option value="">Elegí un alumno…</option>
              {alumnos.map((a) => (
                <option key={a.id} value={a.id}>{a.apellido}, {a.nombre}</option>
              ))}
            </select>
          </label>
        )}

        <label className={s.campo}>
          <span>Día</span>
          <select value={dia} onChange={(e) => setDia(e.target.value as DiaSemana)}>
            {DIAS.map((d) => <option key={d.valor} value={d.valor}>{d.label}</option>)}
          </select>
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
          <span>Profe (opcional)</span>
          <select
            value={profesorId}
            onChange={(e) => {
              const id = e.target.value;
              setProfesorId(id);
              // Al elegir un profe, pre-cargamos su valor hora base (lo podés
              // pisar para esta clase, ej. menores que se pagan menos).
              const p = profes.find((x) => x.userId === id);
              setValorHora(p?.valorHora != null ? String(p.valorHora) : '');
            }}
          >
            <option value="">Sin asignar</option>
            {profes.map((p) => (
              <option key={p.userId} value={p.userId}>{p.nombre}{p.esDueño ? ' (vos)' : ''}</option>
            ))}
          </select>
        </label>
        {profesorId && (
          <label className={s.campo}>
            <span>Valor hora del profe</span>
            <input
              type="number"
              min={0}
              value={valorHora}
              onChange={(e) => setValorHora(e.target.value)}
              onWheel={(e) => e.currentTarget.blur()}
              placeholder="Ej. 8000 (menores: menos)"
            />
          </label>
        )}

        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
