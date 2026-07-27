import { useMemo, useState } from 'react';
import Modal from '../../components/Modal';
import HoraSelect from '../../components/HoraSelect';
import { ApiError } from '../../lib/api';
import { DIAS, horaCorta } from './types';
import type { DiaSemana, Horario, Sede, UpdateHorario } from './types';
import { useProfesores } from '../profesores/useProfesores';
import s from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  horario: Horario;
  sedes: Sede[];
  onClose: () => void;
  onEditar: (id: string, dto: UpdateHorario) => Promise<void>;
}

/**
 * Edición de un horario: cancha, día, hora, duración, profe y su valor hora. El
 * grupo/alumno queda fijo (para cambiarlo se borra y se recrea). Cambiar el día/hora
 * reprograma los turnos futuros (lo maneja el backend).
 */
export default function EditarHorarioModal({ horario, sedes, onClose, onEditar }: Props) {
  const sedeInicial = sedes.find((x) => x.canchas.some((c) => c.id === horario.canchaId))?.id
    ?? sedes[0]?.id ?? '';
  const [sedeId, setSedeId] = useState(sedeInicial);
  const [canchaId, setCanchaId] = useState(horario.canchaId);
  const [dia, setDia] = useState<DiaSemana>(horario.dia);
  const [hora, setHora] = useState(horaCorta(horario.horaInicio));
  const [duracion, setDuracion] = useState(horario.duracionMinutos);
  const [profesorId, setProfesorId] = useState(horario.profesorUserId ?? '');
  const [valorHora, setValorHora] = useState(horario.valorHoraProfe?.toString() ?? '');
  const { profes } = useProfesores();
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const canchas = useMemo(
    () => sedes.find((x) => x.id === sedeId)?.canchas ?? [],
    [sedes, sedeId],
  );

  const valido = canchaId !== '';

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onEditar(horario.id, {
        canchaId,
        profesorUserId: profesorId || undefined,
        valorHoraProfe: profesorId && valorHora ? Number(valorHora) : undefined,
        dia,
        horaInicio: hora, // "18:00" — TimeOnly lo parsea
        duracionMinutos: duracion,
      });
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo guardar el horario.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Editar horario"
      subtitulo={horario.titulo}
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
          <span>Profe</span>
          <select
            value={profesorId}
            onChange={(e) => {
              const id = e.target.value;
              setProfesorId(id);
              // Al cambiar de profe, pre-cargamos su valor hora base (lo podés pisar).
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
              placeholder="Vacío = valor base del profe"
            />
          </label>
        )}

        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
