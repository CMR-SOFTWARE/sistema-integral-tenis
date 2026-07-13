import { useEffect, useMemo, useState } from 'react';
import Modal from '../../components/Modal';
import HoraSelect from '../../components/HoraSelect';
import { api, ApiError } from '../../lib/api';
import type { Alumno } from '../alumnos/types';
import type { Grupo } from '../grupos/types';
import { DIAS } from './types';
import type { CreateHorario, DiaSemana, Sede } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  sedes: Sede[];
  onClose: () => void;
  onCrear: (dto: CreateHorario) => Promise<void>;
}

/** Alta de horario recurrente: cancha + (grupo XOR alumno) + día/hora/duración. */
export default function NuevoHorarioModal({ sedes, onClose, onCrear }: Props) {
  const [sedeId, setSedeId] = useState(sedes[0]?.id ?? '');
  const [canchaId, setCanchaId] = useState('');
  const [tipo, setTipo] = useState<'grupo' | 'individual'>('grupo');
  const [grupoId, setGrupoId] = useState('');
  const [alumnoId, setAlumnoId] = useState('');
  const [dia, setDia] = useState<DiaSemana>('Monday');
  const [hora, setHora] = useState('18:00');
  const [duracion, setDuracion] = useState(60);
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
      titulo="Nuevo horario"
      subtitulo="Plantilla semanal: se repite toda la temporada"
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

        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
