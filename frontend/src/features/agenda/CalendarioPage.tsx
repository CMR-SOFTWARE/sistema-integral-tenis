import { useMemo, useState } from 'react';
import { obtenerSesion } from '../auth/sesion';
import { useProfesores } from '../profesores/useProfesores';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { useHorarios, useMes, useSedes, useSemana } from './hooks';
import TurnoModal from './TurnoModal';
import VistaMes from './VistaMes';
import PanelClasesSueltas from './PanelClasesSueltas';
import PanelSolicitudesHorario from './PanelSolicitudesHorario';
import NuevoHorarioModal from './NuevoHorarioModal';
import EditarHorarioModal from './EditarHorarioModal';
import { aISO, fechaCorta, horaCorta, lunesDe, rangoSemana, sumarDias } from './types';
import type { CreateHorario, Horario, Turno, UpdateHorario } from './types';
import { MESES } from '../cuotas/types';
import { useBloqueos } from '../bloqueos/useBloqueos';
import { cubreFecha, franjaLegible } from '../bloqueos/types';
import s from './CalendarioPage.module.css';

type Vista = 'semana' | 'mes';

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
  const [mesCursor, setMesCursor] = useState({ anio: ahora.getFullYear(), mes: ahora.getMonth() + 1 });

  // Solo la vista activa consulta (la otra queda deshabilitada).
  const semana = useSemana(lunes, vista === 'semana');
  const mes = useMes(mesCursor.anio, mesCursor.mes, vista === 'mes');
  const activo = vista === 'semana' ? semana : mes;

  const { bloqueos } = useBloqueos();
  const { nombreDe } = useProfesores();
  const confirmar = useConfirmar();
  const { sedes } = useSedes();
  const { horarios, crear: crearH, editar: editarH, desactivar: desactivarH, recargar: recargarH } = useHorarios();
  const [abierto, setAbierto] = useState<string | null>(null); // turnoId
  const [modalHorario, setModalHorario] = useState(false);
  const [editandoHorario, setEditandoHorario] = useState<Horario | null>(null);
  const [duplicandoHorario, setDuplicandoHorario] = useState<Horario | null>(null);
  const esOwner = obtenerSesion()?.rol === 'owner';

  const visibles = activo.turnos.filter(
    (t) => (sede === '' || t.sede === sede) && (profe === '' || t.profesorUserId === profe),
  );

  const dias = useMemo(() => Array.from({ length: 7 }, (_, i) => sumarDias(lunes, i)), [lunes]);
  const semanaActual = lunesDe(new Date()) === lunes;
  const mesActual = mesCursor.anio === ahora.getFullYear() && mesCursor.mes === ahora.getMonth() + 1;
  const turnoAbierto: Turno | null = activo.turnos.find((t) => t.id === abierto) ?? null;
  // La plantilla de la que salió el turno abierto (null = clase suelta): habilita sus acciones.
  const horarioDelTurno = turnoAbierto?.horarioId
    ? horarios.find((h) => h.id === turnoAbierto.horarioId) ?? null
    : null;

  // Para dar de alta solo se ofrecen las sedes con canchas.
  const disponibles = sedes.filter((x) => x.activo);
  const sinCanchas = disponibles.every((x) => x.canchas.length === 0);

  const cambiarMes = (delta: number) =>
    setMesCursor(({ anio, mes: m }) => {
      const total = anio * 12 + (m - 1) + delta;
      return { anio: Math.floor(total / 12), mes: (total % 12) + 1 };
    });

  const retroceder = () => (vista === 'semana' ? setLunes(sumarDias(lunes, -7)) : cambiarMes(-1));
  const avanzar = () => (vista === 'semana' ? setLunes(sumarDias(lunes, 7)) : cambiarMes(1));
  const volverAHoy = () =>
    vista === 'semana'
      ? setLunes(lunesDe(new Date()))
      : setMesCursor({ anio: ahora.getFullYear(), mes: ahora.getMonth() + 1 });
  const esHoy = vista === 'semana' ? semanaActual : mesActual;

  // Tocar un horario cambia los turnos generados → refrescamos la semana/mes también.
  const refrescar = async () => { await recargarH(); await activo.recargar(); };
  const crearHorario = async (dto: CreateHorario) => { await crearH(dto); await refrescar(); };
  const editarHorario = async (id: string, dto: UpdateHorario) => { await editarH(id, dto); await refrescar(); };
  const desactivarHorario = async (h: Horario) => {
    if (!(await confirmar({
      titulo: `Desactivar el horario de "${h.titulo}"`,
      mensaje: 'Los turnos ya generados se conservan; no se generan nuevos.',
      confirmar: 'Desactivar',
      peligro: true,
    }))) return;
    setAbierto(null);
    await desactivarH(h.id);
    await refrescar();
  };

  return (
    <div>
      <div className={s.toolbar}>
        <button className={s.nav} onClick={retroceder}>‹</button>
        <div className={s.rango}>
          {vista === 'semana' ? `Semana del ${rangoSemana(lunes)}` : `${MESES[mesCursor.mes - 1]} ${mesCursor.anio}`}
          {!esHoy && (
            <button className={s.hoy} onClick={volverAHoy}>volver a hoy</button>
          )}
        </div>
        <button className={s.nav} onClick={avanzar}>›</button>

        <div className={s.toggle}>
          <button className={vista === 'semana' ? s.toggleActivo : s.toggleBtn} onClick={() => setVista('semana')}>Semana</button>
          <button className={vista === 'mes' ? s.toggleActivo : s.toggleBtn} onClick={() => setVista('mes')}>Mes</button>
        </div>

        {esOwner && (
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
      {esOwner && <PanelClasesSueltas onCambio={() => void activo.recargar()} />}

      {activo.error && <div className={s.error}>{activo.error} — ¿está corriendo la API?</div>}
      {activo.cargando && <div className={s.vacio}>Cargando…</div>}

      {!activo.cargando && !activo.error && vista === 'semana' && (
        <div className={s.grilla}>
          {dias.map((fecha) => {
            const delDia = visibles.filter((t) => t.fecha === fecha);
            const bloqueosDia = bloqueos.filter((b) => cubreFecha(b, fecha));
            const hoyCol = fecha === aISO(new Date());
            return (
              <div key={fecha} className={`${s.columna} ${hoyCol ? s.columnaHoy : ''}`}>
                <div className={s.columnaTitulo}>{fechaCorta(fecha)}</div>
                {bloqueosDia.map((b) => (
                  <div key={b.id} className={s.franjaBloqueada}>
                    <div className={s.turnoHora}>{franjaLegible(b.horaInicio, b.horaFin)}</div>
                    <div className={s.franjaMotivo}>
                      {b.motivo ?? 'Bloqueo fijo'}{b.cancha ? ` · ${b.cancha}` : ''}
                    </div>
                  </div>
                ))}
                {delDia.length === 0 && bloqueosDia.length === 0 && <div className={s.libre}>—</div>}
                {delDia.map((t) => {
                  const cancelado = t.estado === 'Cancelado';
                  const ausentes = t.participantes.filter((p) => !p.presente).length;
                  return (
                    <button
                      key={t.id}
                      className={`${s.turno} ${cancelado ? s.turnoCancelado : ''}`}
                      onClick={() => setAbierto(t.id)}
                    >
                      <div className={s.turnoHora}>{horaCorta(t.horaInicio)}</div>
                      <div className={s.turnoTitulo}>{t.titulo}</div>
                      <div className={s.turnoDetalle}>
                        {cancelado
                          ? `Cancelado: ${t.canceladoMotivo}`
                          : `${t.cancha} · ${t.participantes.length} 👤${ausentes > 0 ? ` · ${ausentes} falta${ausentes > 1 ? 's' : ''}` : ''}`}
                      </div>
                      {!cancelado && t.profesorUserId && (
                        <div className={s.turnoProfe}>{nombreDe(t.profesorUserId)}</div>
                      )}
                    </button>
                  );
                })}
              </div>
            );
          })}
        </div>
      )}

      {!activo.cargando && !activo.error && vista === 'mes' && (
        <VistaMes
          key={`${mesCursor.anio}-${mesCursor.mes}`}
          anio={mesCursor.anio}
          mes={mesCursor.mes}
          turnos={visibles}
          bloqueos={bloqueos}
          onAbrirTurno={(t) => setAbierto(t.id)}
        />
      )}

      {!activo.cargando && !activo.error && vista === 'semana' && activo.turnos.length === 0 && (
        <div className={s.vacioCard}>
          No hay turnos esta semana. Los turnos nacen de los <b>horarios</b>: tocá
          <b> "+ Nuevo horario" </b> para armar una clase y esta pantalla la genera sola.
        </div>
      )}

      {turnoAbierto && (
        <TurnoModal
          turno={turnoAbierto}
          horario={esOwner ? horarioDelTurno : null}
          onClose={() => setAbierto(null)}
          onAsistencia={activo.marcarAsistencia}
          onCancelar={activo.cancelar}
          onEditarHorario={(h) => { setAbierto(null); setEditandoHorario(h); }}
          onDuplicarHorario={(h) => { setAbierto(null); setDuplicandoHorario(h); }}
          onDesactivarHorario={desactivarHorario}
        />
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
          sedes={sedes}
          onClose={() => setEditandoHorario(null)}
          onEditar={editarHorario}
        />
      )}
    </div>
  );
}
