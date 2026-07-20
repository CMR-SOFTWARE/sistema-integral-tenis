import { useState } from 'react';
import { useHorarios, useSedes } from './hooks';
import NuevoHorarioModal from './NuevoHorarioModal';
import PanelSolicitudesHorario from './PanelSolicitudesHorario';
import SelectSede from './SelectSede';
import { DIAS, horaCorta } from './types';
import { CAT_COLOR, CAT_LABEL } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import s from './HorariosPage.module.css';

/** Grilla semanal de PLANTILLAS (horarios recurrentes de la temporada). */
export default function HorariosPage() {
  const { horarios, cargando, error, crear, desactivar, recargar } = useHorarios();
  const { sedes } = useSedes();
  const [modal, setModal] = useState(false);
  const [sede, setSede] = useState(''); // '' = todas

  // Para dar de alta solo se ofrecen las sedes habilitadas; el filtro de
  // arriba sí muestra todas (puede haber horarios de una sede dada de baja)
  const disponibles = sedes.filter((x) => x.activo);
  const sinCanchas = disponibles.every((x) => x.canchas.length === 0);
  const visibles = horarios.filter((h) => sede === '' || h.sede === sede);

  const baja = async (id: string, titulo: string) => {
    if (!window.confirm(
      `¿Desactivar el horario de "${titulo}"? Los turnos ya generados se conservan; no se generan nuevos.`,
    )) return;
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
          title={sinCanchas ? 'Primero cargá una sede con canchas en Configuración' : undefined}
        >
          + Nuevo horario
        </button>
      </div>

      <PanelSolicitudesHorario onCambio={() => void recargar()} />

      {error && <div className={s.error}>{error} — ¿está corriendo la API?</div>}
      {sinCanchas && !cargando && (
        <div className={s.aviso}>
          Para crear horarios primero necesitás al menos una <b>sede con canchas</b>.
          Cargalas en <b>Configuración</b> (sidebar).
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

      {modal && (
        <NuevoHorarioModal
          sedes={disponibles}
          onClose={() => setModal(false)}
          onCrear={crear}
        />
      )}
    </div>
  );
}
