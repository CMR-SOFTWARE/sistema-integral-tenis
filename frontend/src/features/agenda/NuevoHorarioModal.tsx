import { useMemo, useState } from 'react';
import Modal from '../../components/Modal';
import HoraSelect from '../../components/HoraSelect';
import { ApiError } from '../../lib/api';
import CategoriaOptions from '../alumnos/CategoriaOptions';
import type { Categoria } from '../alumnos/types';
import SelectorAlumnos from './SelectorAlumnos';
import { DIAS, horaCorta } from './types';
import type { CreateHorario, DiaSemana, Horario, Sede } from './types';
import { useProfesores } from '../profesores/useProfesores';
import { obtenerSesion } from '../auth/sesion';
import s from '../alumnos/NuevoAlumnoModal.module.css';
import g from './FormHorario.module.css';

interface Props {
  sedes: Sede[];
  onClose: () => void;
  onCrear: (dto: CreateHorario) => Promise<void>;
  /** Al duplicar: horario del que se copian los datos (roster incluido). */
  base?: Horario;
  /** Abierto desde la ficha de un alumno: arranca con ESE alumno tildado. */
  alumnoFijo?: { id: string; nombre: string; apellido: string; profesorUserId?: string | null };
}

/**
 * Alta de una clase fija: profe → club → cancha, cómo se llama, cuántos entran y
 * quiénes vienen, más día/hora/duración. El profe va PRIMERO porque elegirlo
 * completa el club y el valor hora: es el dato del que cuelga el resto.
 */
export default function NuevoHorarioModal({ sedes, onClose, onCrear, base, alumnoFijo }: Props) {
  // Al duplicar, los estados arrancan con los datos del horario base.
  const sedeInicial = base
    ? sedes.find((x) => x.canchas.some((c) => c.id === base.canchaId))?.id ?? sedes[0]?.id ?? ''
    : sedes[0]?.id ?? '';
  const [sedeId, setSedeId] = useState(sedeInicial);
  const [canchaId, setCanchaId] = useState(base?.canchaId ?? '');
  const [nombre, setNombre] = useState(base?.nombre ?? '');
  const [cupo, setCupo] = useState(base?.cupoMaximo?.toString() ?? '');
  // 'SinCategoria' (herencia de los grupos migrados) es lo mismo que sin elegir.
  const [categoria, setCategoria] = useState<Categoria | ''>(
    base?.categoria && base.categoria !== 'SinCategoria' ? base.categoria : '',
  );
  const [alumnoIds, setAlumnoIds] = useState<string[]>(
    alumnoFijo ? [alumnoFijo.id] : base?.miembros.map((m) => m.alumnoId) ?? [],
  );
  const [dia, setDia] = useState<DiaSemana>(base?.dia ?? 'Monday');
  const [hora, setHora] = useState(base ? horaCorta(base.horaInicio) : '18:00');
  const [duracion, setDuracion] = useState(base?.duracionMinutos ?? 60);
  const [profesorId, setProfesorId] = useState(alumnoFijo?.profesorUserId ?? base?.profesorUserId ?? '');
  const [valorHora, setValorHora] = useState(base?.valorHoraProfe?.toString() ?? '');
  const { profes } = useProfesores();
  // El profe empleado da SUS clases: el horario queda a su nombre (lo asigna el back).
  const esStaff = obtenerSesion()?.rol === 'staff';
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const canchas = useMemo(
    () => sedes.find((x) => x.id === sedeId)?.canchas ?? [],
    [sedes, sedeId],
  );

  const cupoNum = cupo === '' ? null : Number(cupo);
  // Bajar el cupo por debajo de los ya tildados lo rechaza el back: lo avisamos antes.
  const cupoCorto = cupoNum !== null && alumnoIds.length > cupoNum;
  const valido = canchaId !== '' && !cupoCorto;

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onCrear({
        canchaId,
        nombre: nombre.trim() || undefined,
        cupoMaximo: cupoNum ?? undefined,
        categoria: categoria || undefined,
        alumnoIds,
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
      <div className={g.grid}>
        {/* El staff no elige profe: la clase queda a su nombre (la asigna el back). */}
        {esStaff ? (
          <label className={s.campo}>
            <span>Profe</span>
            <input type="text" value="Vos (queda a tu nombre)" disabled />
          </label>
        ) : (
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
                // Y su CLUB: la sede se pone sola en la del profe (podés cambiarla).
                if (p?.sedeId && sedes.some((x) => x.id === p.sedeId)) {
                  setSedeId(p.sedeId);
                  setCanchaId('');
                }
              }}
            >
              <option value="">Sin asignar</option>
              {profes.map((p) => (
                <option key={p.userId} value={p.userId}>{p.nombre}{p.esDueño ? ' (vos)' : ''}</option>
              ))}
            </select>
          </label>
        )}
        {!esStaff && profesorId && (
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

        <label className={s.campo}>
          <span>Club</span>
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
          <span>Nombre (opcional)</span>
          <input
            type="text"
            maxLength={80}
            value={nombre}
            onChange={(e) => setNombre(e.target.value)}
            placeholder="Vacío: se arma solo con los alumnos"
          />
        </label>
        <label className={s.campo}>
          <span>Cupo (opcional)</span>
          <input
            type="number"
            min={1}
            max={50}
            value={cupo}
            onChange={(e) => setCupo(e.target.value)}
            onWheel={(e) => e.currentTarget.blur()}
            placeholder="Vacío: sin límite"
          />
        </label>
        <label className={`${s.campo} ${g.span2}`}>
          <span>Categoría sugerida (opcional)</span>
          <select value={categoria} onChange={(e) => setCategoria(e.target.value as Categoria | '')}>
            <option value="">Sin categoría (cualquiera puede entrar)</option>
            <CategoriaOptions />
          </select>
        </label>

        <div className={`${s.campo} ${g.span2}`}>
          {/* Desde la ficha el alumno ya viene tildado, pero se pueden sumar más. */}
          <span>Alumnos{alumnoFijo ? ` · ${alumnoFijo.nombre} ${alumnoFijo.apellido} ya está tildado` : ''}</span>
          <SelectorAlumnos elegidos={alumnoIds} onCambiar={setAlumnoIds} cupo={cupoNum} />
        </div>

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

        {cupoCorto && (
          <div className={`${g.span2} ${s.error}`}>
            Elegiste {alumnoIds.length} alumnos pero el cupo es de {cupoNum}.
          </div>
        )}
        {error && <div className={`${g.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
