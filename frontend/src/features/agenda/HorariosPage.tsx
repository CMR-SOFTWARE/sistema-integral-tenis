import { useState } from 'react';
import { api } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { useProfesores } from '../profesores/useProfesores';
import { useHorarios, useSedes } from './hooks';
import NuevoHorarioModal from './NuevoHorarioModal';
import EditarHorarioModal from './EditarHorarioModal';
import PanelSolicitudesHorario from './PanelSolicitudesHorario';
import SelectSede from './SelectSede';
import { DIAS, horaCorta } from './types';
import type { Horario } from './types';
import { CAT_COLOR, CAT_LABEL } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import s from './HorariosPage.module.css';

/** Grilla semanal de PLANTILLAS (horarios recurrentes de la temporada). */
export default function HorariosPage() {
  const { horarios, cargando, error, crear, editar, desactivar, recargar } = useHorarios();
  const { sedes } = useSedes();
  const [modal, setModal] = useState(false);
  const [editando, setEditando] = useState<Horario | null>(null);
  const [duplicando, setDuplicando] = useState<Horario | null>(null);
  const [sede, setSede] = useState(''); // '' = todas
  const confirmar = useConfirmar();
  const { profes } = useProfesores();

  const reasignarProfe = async (id: string, profesorUserId: string) => {
    await api.patch(`/horarios/${id}/profesor`, { profesorUserId: profesorUserId || null });
    void recargar();
  };

  // Para dar de alta solo se ofrecen las sedes habilitadas; el filtro de
  // arriba sí muestra todas (puede haber horarios de una sede dada de baja)
  const disponibles = sedes.filter((x) => x.activo);
  const sinCanchas = disponibles.every((x) => x.canchas.length === 0);
  const visibles = horarios.filter((h) => sede === '' || h.sede === sede);

  const baja = async (id: string, titulo: string) => {
    if (!(await confirmar({
      titulo: `Desactivar el horario de "${titulo}"`,
      mensaje: 'Los turnos ya generados se conservan; no se generan nuevos.',
      confirmar: 'Desactivar',
      peligro: true,
    }))) return;
    await desactivar(id);
  };

  return (
    <div>
      <div className={s.toolbar}>
        <div className={s.hint}>Plantillas de la temporada — el calendario genera los turnos concretos desde acá</div>
        <div className={s.spacer} />
        <SelectSede sedes={sedes} valor={sede} onChange={setSede} />
        <button
          className={s.btnNuevo}
          onClick={() => setModal(true)}
          disabled={sinCanchas}
          title={sinCanchas ? 'Primero cargá una sede con canchas en Mi academia → Configuración' : undefined}
        >
          + Nuevo horario
        </button>
      </div>

      <PanelSolicitudesHorario onCambio={() => void recargar()} />

      {error && <div className={s.error}>{error} — ¿está corriendo la API?</div>}
      {sinCanchas && !cargando && (
        <div className={s.aviso}>
          Para crear horarios primero necesitás al menos una <b>sede con canchas</b>.
          Cargalas en <b>Mi academia</b> → Configuración.
        </div>
      )}

      <div className={s.grilla}>
        {DIAS.map((d) => {
          const delDia = visibles
            .filter((h) => h.dia === d.valor)
            .sort((a, b) => a.horaInicio.localeCompare(b.horaInicio));
          return (
            <div key={d.valor} className={s.columna}>
              <div className={s.columnaTitulo}>{d.corto}</div>
              {delDia.length === 0 && <div className={s.libre}>—</div>}
              {delDia.map((h) => {
                const cat = h.categoria ? CAT_COLOR[h.categoria as Categoria] : null;
                return (
                  <div key={h.id} className={s.slot}>
                    <div className={s.slotHora}>
                      {horaCorta(h.horaInicio)}
                      <span className={s.slotDur}> · {h.duracionMinutos}'</span>
                    </div>
                    <div className={s.slotTitulo}>
                      {cat && h.categoria && (
                        <span className={s.chip} style={{ background: `${cat}1a`, color: cat }}>
                          {CAT_LABEL[h.categoria as Categoria] ?? h.categoria}
                        </span>
                      )}
                      {h.titulo}
                    </div>
                    <div className={s.slotLugar}>{h.sede} · {h.cancha}</div>
                    <select
                      className={s.slotProfe}
                      value={h.profesorUserId ?? ''}
                      onChange={(e) => void reasignarProfe(h.id, e.target.value)}
                      title="Profe a cargo"
                    >
                      <option value="">Sin profe</option>
                      {profes.map((p) => (
                        <option key={p.userId} value={p.userId}>{p.nombre}{p.esDueño ? ' (vos)' : ''}</option>
                      ))}
                    </select>
                    <button
                      className={s.slotDuplicar}
                      title="Duplicar horario"
                      onClick={() => setDuplicando(h)}
                    >
                      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                        <rect x="9" y="9" width="11" height="11" rx="2" />
                        <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
                      </svg>
                    </button>
                    <button
                      className={s.slotEditar}
                      title="Editar horario"
                      onClick={() => setEditando(h)}
                    >
                      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                        <path d="M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z" />
                      </svg>
                    </button>
                    <button
                      className={s.slotBaja}
                      title="Desactivar horario"
                      onClick={() => void baja(h.id, h.titulo)}
                    >
                      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
                        <path d="M18 6L6 18M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                );
              })}
            </div>
          );
        })}
      </div>

      {(modal || duplicando) && (
        <NuevoHorarioModal
          sedes={disponibles}
          base={duplicando ?? undefined}
          onClose={() => { setModal(false); setDuplicando(null); }}
          onCrear={crear}
        />
      )}

      {editando && (
        <EditarHorarioModal
          horario={editando}
          sedes={sedes}
          onClose={() => setEditando(null)}
          onEditar={editar}
        />
      )}
    </div>
  );
}
