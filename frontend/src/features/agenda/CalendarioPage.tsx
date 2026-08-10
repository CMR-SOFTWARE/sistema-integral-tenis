import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { obtenerSesion } from '../auth/sesion';
import { useProfesores } from '../profesores/useProfesores';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { useHorarios, useMes, useSedes, useSemana } from './hooks';
import TurnoModal from './TurnoModal';
import VistaMes from './VistaMes';
import GrillaSemana from './GrillaSemana';
import PanelClasesSueltas from './PanelClasesSueltas';
import PanelSolicitudesHorario from './PanelSolicitudesHorario';
import PanelSolicitudesCupo from './PanelSolicitudesCupo';
import NuevoHorarioModal from './NuevoHorarioModal';
import EditarHorarioModal from './EditarHorarioModal';
import FichaDesdeAgenda from './FichaDesdeAgenda';
import { aISO, fechaLarga, lunesDe, rangoSemana, sumarDias } from './types';
import type { CreateHorario, Horario, Turno, UpdateHorario } from './types';
import { MESES } from '../cuotas/types';
import { useBloqueos } from '../bloqueos/useBloqueos';
import s from './CalendarioPage.module.css';

type Vista = 'dia' | 'semana' | 'mes';

interface Props {
  /** Filtros compartidos de la Agenda: sede (nombre) y profe (userId); '' = todos. */
  sede: string;
  profe: string;
}

/**
 * Agenda de la semana: los turnos concretos (asistencia/cancelar) Y la gestión de
 * los horarios (plantillas) que los generan, en una sola vista con toggle Semana/Mes.
 * Cada clase abre el detalle con TODAS sus acciones; "Nuevo horario" arma la plantilla.
 */
export default function CalendarioPage({ sede, profe }: Props) {
  const ahora = new Date();
  const [vista, setVista] = useState<Vista>('semana');
  const [lunes, setLunes] = useState(() => lunesDe(new Date()));
  const [dia, setDia] = useState(() => aISO(new Date())); // cursor de la vista Día
  const [mesCursor, setMesCursor] = useState({ anio: ahora.getFullYear(), mes: ahora.getMonth() + 1 });

  // La vista Día se sirve de la MISMA semana que ya pide el backend: se pide la
  // semana que contiene al día y se muestra esa sola columna. Un endpoint menos.
  const lunesActivo = vista === 'dia' ? lunesDe(new Date(`${dia}T00:00:00`)) : lunes;

  // Solo la vista activa consulta (la otra queda deshabilitada).
  const semana = useSemana(lunesActivo, vista !== 'mes');
  const mes = useMes(mesCursor.anio, mesCursor.mes, vista === 'mes');
  const activo = vista === 'mes' ? mes : semana;

  const { bloqueos } = useBloqueos();
  const { profes, nombreDe } = useProfesores();
  const confirmar = useConfirmar();
  const { sedes, cargando: sedesCargando } = useSedes();
  const {
    horarios, crear: crearH, editar: editarH, desactivar: desactivarH,
    agregarAlumno, quitarAlumno, recargar: recargarH,
  } = useHorarios();
  const [abierto, setAbierto] = useState<string | null>(null); // turnoId
  const [fichaAlumnoId, setFichaAlumnoId] = useState<string | null>(null); // ficha abierta desde una clase
  const [modalHorario, setModalHorario] = useState(false);
  // Se guarda el ID y no el objeto: tocar el roster desde el modal recarga la lista,
  // y el modal tiene que ver el roster nuevo (un snapshot quedaría viejo).
  const [editandoId, setEditandoId] = useState<string | null>(null);
  const [duplicandoHorario, setDuplicandoHorario] = useState<Horario | null>(null);
  const [porProfe, setPorProfe] = useState(false); // ver la semana/mes agrupada por profesor
  const sesion = obtenerSesion();
  const esOwner = sesion?.rol === 'owner';
  const esStaff = sesion?.rol === 'staff';
  const miSedeId = sesion?.sedeId ?? null; // el club del empleado (acota sus canchas)

  const visibles = activo.turnos.filter(
    (t) => (sede === '' || t.sede === sede) && (profe === '' || t.profesorUserId === profe),
  );

  // Las clases SIN alumnos no generan turnos, pero ocupan la cancha igual: si no se
  // mostraran, quedan invisibles y no hay forma de editarlas ni darlas de baja (el
  // profe las choca recién al querer armar otra clase en esa franja).
  const clasesVacias = horarios.filter(
    (h) => h.miembrosActivos === 0
      && (sede === '' || h.sede === sede)
      && (profe === '' || h.profesorUserId === profe),
  );

  // Modo "por profe": los profes CON turnos en el período (el filtro de profe ya colapsa a uno).
  // Los turnos sin profe van a su propia sección (solo cuando no se filtró por uno).
  const gruposProfe = porProfe
    ? profes.filter((p) => (profe === '' || p.userId === profe) && visibles.some((t) => t.profesorUserId === p.userId))
    : [];
  const haySinProfe = porProfe && profe === '' && visibles.some((t) => t.profesorUserId === null);

  // En vista Día, una sola columna; en Semana, las siete.
  const dias = useMemo(
    () => (vista === 'dia' ? [dia] : Array.from({ length: 7 }, (_, i) => sumarDias(lunes, i))),
    [vista, dia, lunes],
  );

  // En el celular la semana muestra un mapa de los 7 días y ABRE uno solo (siete
  // columnas en 390 px dan 47 px cada una: no entra nada). Vive acá y no en la
  // grilla porque en modo "Por profe" hay varias apiladas y todas abren el mismo.
  const [diaElegido, setDiaElegido] = useState(() => aISO(new Date()));
  // Derivado y no un efecto: al cambiar de semana el día guardado deja de existir,
  // y se cae solo a hoy (si esta semana lo tiene) o al lunes. Nunca queda en blanco.
  const diaAbierto = dias.includes(diaElegido)
    ? diaElegido
    : dias.includes(aISO(new Date())) ? aISO(new Date()) : dias[0];
  const semanaActual = lunesDe(new Date()) === lunes;
  const mesActual = mesCursor.anio === ahora.getFullYear() && mesCursor.mes === ahora.getMonth() + 1;
  const turnoAbierto: Turno | null = activo.turnos.find((t) => t.id === abierto) ?? null;
  const editandoHorario = editandoId ? horarios.find((h) => h.id === editandoId) ?? null : null;
  // La plantilla de la que salió el turno abierto (null = clase suelta): habilita sus acciones.
  const horarioDelTurno = turnoAbierto?.horarioId
    ? horarios.find((h) => h.id === turnoAbierto.horarioId) ?? null
    : null;

  // Para dar de alta solo se ofrecen las sedes con canchas. El profe EMPLEADO ve
  // solo SU club (el dueño, todas): las canchas que puede tocar están acotadas.
  const soloMiClub = (lista: typeof sedes) =>
    esStaff && miSedeId ? lista.filter((x) => x.id === miSedeId) : lista;
  const disponibles = soloMiClub(sedes.filter((x) => x.activo));
  const sinCanchas = disponibles.every((x) => x.canchas.length === 0);

  // Deep-link desde el inicio (Accesos directos): /agenda?tab=calendario&nuevo=1
  // abre "Nuevo horario". Solo si hay canchas (si no, el modal no tendría dónde crear).
  const [params, setParams] = useSearchParams();
  useEffect(() => {
    // Esperamos a que carguen las sedes: si no, sinCanchas arranca en true.
    if (params.get('nuevo') !== '1' || sedesCargando) return;
    if ((esOwner || esStaff) && !sinCanchas) setModalHorario(true);
    const next = new URLSearchParams(params);
    next.delete('nuevo');
    setParams(next, { replace: true });
  }, [params, setParams, esOwner, esStaff, sinCanchas, sedesCargando]);

  const cambiarMes = (delta: number) =>
    setMesCursor(({ anio, mes: m }) => {
      const total = anio * 12 + (m - 1) + delta;
      return { anio: Math.floor(total / 12), mes: (total % 12) + 1 };
    });

  const mover = (pasos: number) => {
    if (vista === 'dia') setDia(sumarDias(dia, pasos));
    else if (vista === 'semana') setLunes(sumarDias(lunes, pasos * 7));
    else cambiarMes(pasos);
  };
  const retroceder = () => mover(-1);
  const avanzar = () => mover(1);
  const volverAHoy = () => {
    if (vista === 'dia') setDia(aISO(new Date()));
    else if (vista === 'semana') setLunes(lunesDe(new Date()));
    else setMesCursor({ anio: ahora.getFullYear(), mes: ahora.getMonth() + 1 });
  };
  const esHoy = vista === 'dia' ? dia === aISO(new Date()) : vista === 'semana' ? semanaActual : mesActual;

  // Tocar un horario cambia los turnos generados → refrescamos la semana/mes también.
  const refrescar = async () => { await recargarH(); await activo.recargar(); };
  const crearHorario = async (dto: CreateHorario) => { await crearH(dto); await refrescar(); };
  const editarHorario = async (id: string, dto: UpdateHorario) => { await editarH(id, dto); await refrescar(); };
  const desactivarHorario = async (h: Horario) => {
    if (!(await confirmar({
      titulo: `Desactivar el horario de "${h.titulo}"`,
      mensaje: 'Los turnos ya generados se conservan; no se generan nuevos. Se libera la franja de esa cancha.',
      confirmar: 'Desactivar',
      peligro: true,
    }))) return;
    // Se llama desde el detalle del turno Y desde la edición: se cierran los dos.
    setAbierto(null);
    setEditandoId(null);
    await desactivarH(h.id);
    await refrescar();
  };

  return (
    <div>
      <div className={s.toolbar}>
        {/* Las flechas y el período van juntos en un bloque: sueltos en la toolbar,
            el texto largo del rango los separaba al envolverse en pantallas chicas. */}
        <div className={s.navegador}>
          <button className={s.nav} onClick={retroceder} aria-label="Anterior">‹</button>
          <div className={s.rango}>
            {vista === 'dia'
              ? fechaLarga(dia)
              : vista === 'semana'
                ? `Semana del ${rangoSemana(lunes)}`
                : `${MESES[mesCursor.mes - 1]} ${mesCursor.anio}`}
            {!esHoy && (
              <button className={s.hoy} onClick={volverAHoy}>volver a hoy</button>
            )}
          </div>
          <button className={s.nav} onClick={avanzar} aria-label="Siguiente">›</button>
        </div>

        <div className={s.toggle}>
          <button className={vista === 'dia' ? s.toggleActivo : s.toggleBtn} onClick={() => setVista('dia')}>Día</button>
          <button className={vista === 'semana' ? s.toggleActivo : s.toggleBtn} onClick={() => setVista('semana')}>Semana</button>
          <button className={vista === 'mes' ? s.toggleActivo : s.toggleBtn} onClick={() => setVista('mes')}>Mes</button>
        </div>

        {esOwner && profes.length >= 2 && (
          <div className={s.toggle}>
            <button className={!porProfe ? s.toggleActivo : s.toggleBtn} onClick={() => setPorProfe(false)}>Todos</button>
            <button className={porProfe ? s.toggleActivo : s.toggleBtn} onClick={() => setPorProfe(true)}>Por profe</button>
          </div>
        )}

        {/* Dueño Y staff pueden armar horarios (el staff, solo en canchas de su club). */}
        {(esOwner || esStaff) && (
          <button
            className={s.btnNuevo}
            onClick={() => setModalHorario(true)}
            disabled={sinCanchas}
            title={sinCanchas ? 'Primero cargá una sede con canchas en Mi academia → Configuración' : undefined}
          >
            + Nuevo horario
          </button>
        )}
      </div>

      <div className={s.leyenda}>
        <span className={s.leyendaItem}><i className={s.puntoProgramado} /> Programado</span>
        <span className={s.leyendaItem}><i className={s.puntoCancelado} /> Cancelado</span>
        <span className={s.leyendaItem}><i className={s.puntoBloqueado} /> Bloqueado</span>
      </div>

      {esOwner && <PanelSolicitudesHorario onCambio={() => void refrescar()} />}
      {esOwner && <PanelSolicitudesCupo onCambio={() => void refrescar()} />}
      {esOwner && <PanelClasesSueltas onCambio={() => void activo.recargar()} />}

      {activo.error && <div className={s.error}>{activo.error}</div>}
      {activo.cargando && <div className={s.vacio}>Cargando…</div>}

      {/* Modo TODOS: una sola grilla (Día o Semana) o un solo mes. */}
      {!activo.cargando && !activo.error && !porProfe && vista !== 'mes' && (
        <GrillaSemana dias={dias} turnos={visibles} clasesVacias={clasesVacias} bloqueos={bloqueos} onAbrirTurno={setAbierto} onAbrirClaseVacia={(h) => setEditandoId(h.id)} diaAbierto={diaAbierto} onAbrirDia={setDiaElegido} nombreDe={nombreDe} />
      )}

      {!activo.cargando && !activo.error && !porProfe && vista === 'mes' && (
        <VistaMes
          key={`${mesCursor.anio}-${mesCursor.mes}`}
          anio={mesCursor.anio}
          mes={mesCursor.mes}
          turnos={visibles}
          bloqueos={bloqueos}
          onAbrirTurno={(t) => setAbierto(t.id)}
          nombreDe={nombreDe}
        />
      )}

      {/* Modo POR PROFE: la semana/mes de cada profe, apilada. */}
      {!activo.cargando && !activo.error && porProfe && (
        <div className={s.porProfe}>
          {gruposProfe.map((p) => {
            const suyos = visibles.filter((t) => t.profesorUserId === p.userId);
            return (
              <div key={p.userId} className={s.grupoProfe}>
                <div className={s.grupoProfeTitulo}>{p.nombre}{p.esDueño ? ' (vos)' : ''}</div>
                {vista !== 'mes' ? (
                  <GrillaSemana dias={dias} turnos={suyos} clasesVacias={clasesVacias.filter((h) => h.profesorUserId === p.userId)} bloqueos={bloqueos} onAbrirTurno={setAbierto} onAbrirClaseVacia={(h) => setEditandoId(h.id)} diaAbierto={diaAbierto} onAbrirDia={setDiaElegido} nombreDe={nombreDe} />
                ) : (
                  <VistaMes
                    key={`${mesCursor.anio}-${mesCursor.mes}-${p.userId}`}
                    anio={mesCursor.anio} mes={mesCursor.mes} turnos={suyos}
                    bloqueos={bloqueos} onAbrirTurno={(t) => setAbierto(t.id)}
                    nombreDe={nombreDe}
                  />
                )}
              </div>
            );
          })}
          {haySinProfe && (
            <div className={s.grupoProfe}>
              <div className={s.grupoProfeTitulo}>Sin profe</div>
              {vista !== 'mes' ? (
                <GrillaSemana dias={dias} turnos={visibles.filter((t) => !t.profesorUserId)} clasesVacias={clasesVacias.filter((h) => !h.profesorUserId)} bloqueos={bloqueos} onAbrirTurno={setAbierto} onAbrirClaseVacia={(h) => setEditandoId(h.id)} diaAbierto={diaAbierto} onAbrirDia={setDiaElegido} nombreDe={nombreDe} />
              ) : (
                <VistaMes
                  key={`${mesCursor.anio}-${mesCursor.mes}-sin`}
                  anio={mesCursor.anio} mes={mesCursor.mes} turnos={visibles.filter((t) => !t.profesorUserId)}
                  bloqueos={bloqueos} onAbrirTurno={(t) => setAbierto(t.id)}
                  nombreDe={nombreDe}
                />
              )}
            </div>
          )}
          {gruposProfe.length === 0 && !haySinProfe && (
            <div className={s.vacioCard}>
              No hay turnos de {profe === '' ? 'ningún profe' : 'este profe'} en este período.
            </div>
          )}
        </div>
      )}

      {!activo.cargando && !activo.error && !porProfe && vista !== 'mes' &&
       (vista === 'dia' ? !visibles.some((t) => t.fecha === dia) : activo.turnos.length === 0) && (
        <div className={s.vacioCard}>
          No hay turnos {vista === 'dia' ? 'este día' : 'esta semana'}. Los turnos nacen
          de los <b>horarios</b>: tocá <b>"+ Nuevo horario"</b> para armar una clase y
          esta pantalla la genera sola.
        </div>
      )}

      {turnoAbierto && (
        <TurnoModal
          turno={turnoAbierto}
          horario={esOwner || esStaff ? horarioDelTurno : null}
          onClose={() => setAbierto(null)}
          onAsistencia={activo.marcarAsistencia}
          onCancelar={activo.cancelar}
          onEditarHorario={(h) => { setAbierto(null); setEditandoId(h.id); }}
          onDuplicarHorario={(h) => { setAbierto(null); setDuplicandoHorario(h); }}
          onDesactivarHorario={esOwner ? desactivarHorario : undefined}
          // Se cierra el turno en vez de apilar dos modales
          onAbrirFicha={(alumnoId) => { setAbierto(null); setFichaAlumnoId(alumnoId); }}
        />
      )}

      {fichaAlumnoId && (
        <FichaDesdeAgenda alumnoId={fichaAlumnoId} onClose={() => setFichaAlumnoId(null)} />
      )}

      {(modalHorario || duplicandoHorario) && (
        <NuevoHorarioModal
          sedes={disponibles}
          base={duplicandoHorario ?? undefined}
          onClose={() => { setModalHorario(false); setDuplicandoHorario(null); }}
          onCrear={crearHorario}
        />
      )}

      {editandoHorario && (
        <EditarHorarioModal
          horario={editandoHorario}
          sedes={soloMiClub(sedes)}
          onClose={() => setEditandoId(null)}
          onEditar={editarHorario}
          onAgregarAlumno={agregarAlumno}
          onQuitarAlumno={quitarAlumno}
          onDesactivar={esOwner ? desactivarHorario : undefined}
        />
      )}
    </div>
  );
}
