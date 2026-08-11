import { useMemo, useState } from 'react';
import Modal from '../../components/Modal';
import HoraSelect from '../../components/HoraSelect';
import { ApiError } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import CategoriaOptions from '../alumnos/CategoriaOptions';
import type { Categoria } from '../alumnos/types';
import SelectorAlumnos from './SelectorAlumnos';
import { DIAS, horaCorta } from './types';
import type { DiaSemana, Horario, Sede, UpdateHorario } from './types';
import { useProfesores } from '../profesores/useProfesores';
import s from '../alumnos/NuevoAlumnoModal.module.css';
import g from './FormHorario.module.css';
import r from './EditarHorarioModal.module.css';

interface Props {
  horario: Horario;
  sedes: Sede[];
  onClose: () => void;
  onEditar: (id: string, dto: UpdateHorario) => Promise<void>;
  /** El roster va por su cuenta: el back reconcilia los turnos al instante. */
  onAgregarAlumnos: (horarioId: string, alumnoIds: string[]) => Promise<void>;
  onQuitarAlumno: (horarioId: string, alumnoId: string) => Promise<void>;
  /** Dar de baja la clase; solo el dueño (al staff no se le pasa). Ya confirma él. */
  onDesactivar?: (h: Horario) => Promise<void>;
}

/**
 * Edición de una clase: cancha, día, hora, duración, profe, nombre, cupo y categoría
 * (todo eso se guarda con el botón) más el roster, que se toca acá mismo pero se
 * aplica al instante: sumar o sacar a alguien reprograma sus turnos futuros y cambia
 * el divisor de la cuota, así que no puede quedar esperando un "Guardar".
 */
export default function EditarHorarioModal({
  horario, sedes, onClose, onEditar, onAgregarAlumnos, onQuitarAlumno, onDesactivar,
}: Props) {
  const sedeInicial = sedes.find((x) => x.canchas.some((c) => c.id === horario.canchaId))?.id
    ?? sedes[0]?.id ?? '';
  const [sedeId, setSedeId] = useState(sedeInicial);
  const [canchaId, setCanchaId] = useState(horario.canchaId);
  const [nombre, setNombre] = useState(horario.nombre ?? '');
  const [cupo, setCupo] = useState(horario.cupoMaximo?.toString() ?? '');
  // 'SinCategoria' (herencia de los grupos migrados) es lo mismo que sin elegir.
  const [categoria, setCategoria] = useState<Categoria | ''>(
    horario.categoria && horario.categoria !== 'SinCategoria' ? horario.categoria : '',
  );
  const [dia, setDia] = useState<DiaSemana>(horario.dia);
  const [hora, setHora] = useState(horaCorta(horario.horaInicio));
  const [duracion, setDuracion] = useState(horario.duracionMinutos);
  const [profesorId, setProfesorId] = useState(horario.profesorUserId ?? '');
  const [valorHora, setValorHora] = useState(horario.valorHoraProfe?.toString() ?? '');
  const { profes } = useProfesores();
  const confirmar = useConfirmar();
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Roster
  const [sumando, setSumando] = useState(false);
  const [aSumar, setASumar] = useState<string[]>([]);
  const [tocandoRoster, setTocandoRoster] = useState(false);

  const canchas = useMemo(
    () => sedes.find((x) => x.id === sedeId)?.canchas ?? [],
    [sedes, sedeId],
  );

  const cupoNum = cupo === '' ? null : Number(cupo);
  // El back rechaza un cupo por debajo de los que ya vienen: lo avisamos antes.
  const cupoCorto = cupoNum !== null && horario.miembrosActivos > cupoNum;
  const valido = canchaId !== '' && !cupoCorto;

  // Los lugares se cuentan contra el cupo DEL FORMULARIO, no el guardado: agrandar
  // una clase particular (cupo 1 → 4) y sumarle los tres nuevos es UN solo gesto, y
  // el guardado del cupo viaja con el alta (ver sumarTildados).
  const lugaresLibres = cupoNum === null
    ? null
    : Math.max(0, cupoNum - horario.miembrosActivos);
  const completa = lugaresLibres === 0;
  // Los tildados tienen que seguir entrando: si bajás el cupo DESPUÉS de tildarlos,
  // el back los rechazaría de a uno con un error confuso.
  const entranLosTildados = lugaresLibres === null || aSumar.length <= lugaresLibres;

  const dtoActual = (): UpdateHorario => ({
    canchaId,
    nombre: nombre.trim() || undefined,
    cupoMaximo: cupoNum ?? undefined,
    categoria: categoria || undefined,
    profesorUserId: profesorId || undefined,
    valorHoraProfe: profesorId && valorHora ? Number(valorHora) : undefined,
    dia,
    horaInicio: hora, // "18:00" — TimeOnly lo parsea
    duracionMinutos: duracion,
  });

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onEditar(horario.id, dtoActual());
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo guardar el horario.');
    } finally {
      setEnviando(false);
    }
  };

  const sumarTildados = async () => {
    setError(null);
    setTocandoRoster(true);
    try {
      // El formulario se guarda ANTES de sumar porque el back valida el alta contra
      // el cupo PERSISTIDO: sin esto, agrandar la clase y sumar gente eran dos
      // pasadas con un "Guardar cambios" en el medio. Por eso el botón dice
      // "Guardar y sumar": nombra las dos cosas que hace.
      await onEditar(horario.id, dtoActual());
      await onAgregarAlumnos(horario.id, aSumar);
      setASumar([]);
      setSumando(false);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo sumar al alumno.');
    } finally {
      setTocandoRoster(false);
    }
  };

  const quitar = async (alumnoId: string, quien: string) => {
    if (!(await confirmar({
      titulo: 'Sacar de la clase',
      mensaje: `¿Sacar a ${quien} de "${horario.titulo}"? Sale de los turnos futuros y su historia se conserva.`,
      confirmar: 'Sacar',
      peligro: true,
    }))) return;
    setError(null);
    setTocandoRoster(true);
    try {
      await onQuitarAlumno(horario.id, alumnoId);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo sacar al alumno.');
    } finally {
      setTocandoRoster(false);
    }
  };

  return (
    <Modal
      titulo="Editar horario"
      subtitulo={horario.titulo}
      onClose={onClose}
      footer={
        <>
          {/* Dar de baja vive acá y no solo en el detalle del turno: una clase sin
              alumnos no genera turnos, y por ese camino no había forma de llegarle. */}
          {onDesactivar && (
            <button
              className={r.btnBaja}
              disabled={enviando || tocandoRoster}
              onClick={() => void onDesactivar(horario)}
            >
              Desactivar
            </button>
          )}
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={() => void guardar()} disabled={enviando || !valido}>
            {enviando ? 'Guardando…' : 'Guardar cambios'}
          </button>
        </>
      }
    >
      <div className={g.grid}>
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

        {/* El roster va último: en el celular, lo que más se edita es el día y la
            hora, y no conviene tener que pasar por la lista de alumnos para llegar. */}
        <div className={`${g.span2} ${r.roster}`}>
          <div className={r.rosterHeader}>
            <span className={r.rosterTitulo}>Alumnos que vienen</span>
            {/* El cupo que se muestra es el del FORMULARIO: si acabás de escribir 4,
                la clase ya se lee como "1/4" y podés sumar los que entran. */}
            <span className={r.rosterCupo}>
              {horario.miembrosActivos}
              {cupoNum !== null ? `/${cupoNum}${completa ? ' · completa' : ''}` : ''}
            </span>
            <button
              className={r.btnSumar}
              disabled={tocandoRoster || completa}
              title={completa ? 'La clase está completa: subí el cupo para sumar a alguien más' : undefined}
              onClick={() => { setSumando((x) => !x); setASumar([]); }}
            >
              {sumando ? 'Listo' : '+ Sumar alumnos'}
            </button>
          </div>

          <div className={r.miembros}>
            {horario.miembros.map((m) => (
              <span key={m.alumnoId} className={r.miembroChip}>
                {m.nombre} {m.apellido}
                <button
                  className={r.quitarX}
                  disabled={tocandoRoster}
                  title={`Sacar a ${m.nombre}`}
                  onClick={() => void quitar(m.alumnoId, `${m.nombre} ${m.apellido}`)}
                >
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
                    <path d="M18 6L6 18M6 6l12 12" />
                  </svg>
                </button>
              </span>
            ))}
            {horario.miembros.length === 0 && (
              <span className={r.sinAlumnos}>Sin alumnos: la clase no genera turnos.</span>
            )}
          </div>

          {sumando && (
            <>
              <SelectorAlumnos
                elegidos={aSumar}
                onCambiar={setASumar}
                cupo={lugaresLibres}
                excluir={horario.miembros.map((m) => m.alumnoId)}
              />
              <button
                className={r.btnSumar}
                disabled={tocandoRoster || aSumar.length === 0 || !valido || !entranLosTildados}
                onClick={() => void sumarTildados()}
              >
                {tocandoRoster ? 'Guardando y sumando…' : `Guardar y sumar ${aSumar.length}`}
              </button>
            </>
          )}

          <p className={r.nota}>
            Sumar o sacar gente se aplica al momento y reordena los turnos futuros.
            Al sumar se guardan también los cambios del formulario (entre ellos el cupo,
            que es contra el que el sistema valida quién entra).
          </p>
        </div>

        {cupoCorto && (
          <div className={`${g.span2} ${s.error}`}>
            Ya vienen {horario.miembrosActivos} alumnos: el cupo no puede ser menor.
          </div>
        )}
        {error && <div className={`${g.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
