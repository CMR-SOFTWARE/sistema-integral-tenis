import { useState } from 'react';
import { api } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { useProfesores } from '../profesores/useProfesores';
import { useGrupos } from './useGrupos';
import NuevoGrupoModal from './NuevoGrupoModal';
import AsignarAlumnoModal from './AsignarAlumnoModal';
import PanelSolicitudes from './PanelSolicitudes';
import { CAT_COLOR, CAT_LABEL } from '../alumnos/types';
import type { Grupo } from './types';
import s from './GruposPage.module.css';

export default function GruposPage() {
  const { grupos, cargando, error, crear, asignar, quitar, recargar } = useGrupos();
  const [modalNuevo, setModalNuevo] = useState(false);
  const [grupoAsignar, setGrupoAsignar] = useState<Grupo | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const confirmar = useConfirmar();
  const { profes } = useProfesores();

  const avisar = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 2600);
  };

  const reasignarProfe = async (grupoId: string, profesorUserId: string) => {
    await api.patch(`/grupos/${grupoId}/profesor`, { profesorUserId: profesorUserId || null });
    void recargar();
  };

  const quitarMiembro = async (grupo: Grupo, alumnoId: string, nombre: string) => {
    if (!(await confirmar({
      titulo: 'Quitar del grupo',
      mensaje: `¿Quitar a ${nombre} de "${grupo.nombre}"? Su historia en el grupo se conserva.`,
      confirmar: 'Quitar',
      peligro: true,
    }))) return;
    await quitar(grupo.id, alumnoId);
    avisar(`${nombre} quitado del grupo`);
  };

  return (
    <div>
      <div className={s.toolbar}>
        <div className={s.spacer} />
        <div className={s.contador}>{grupos.length} grupos</div>
        <button className={s.btnNuevo} onClick={() => setModalNuevo(true)}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.5" strokeLinecap="round">
            <path d="M12 5v14M5 12h14" />
          </svg>
          Nuevo grupo
        </button>
      </div>

      <PanelSolicitudes onCambio={() => void recargar()} />

      {error && <div className={s.error}>{error} — ¿está corriendo la API? (dotnet run)</div>}
      {cargando && !error && <div className={s.vacio}>Cargando…</div>}

      {!cargando && !error && grupos.length === 0 && (
        <div className={s.vacioCard}>
          Todavía no hay grupos. Creá el primero con "Nuevo grupo" — por ejemplo,
          "Intermedios martes" con cupo 4.
        </div>
      )}

      <div className={s.grilla}>
        {grupos.map((g) => {
          const cat = g.categoria ? CAT_COLOR[g.categoria] : null;
          const lleno = g.cupoMaximo !== null && g.miembrosActivos >= g.cupoMaximo;
          return (
            <div key={g.id} className={s.tarjeta}>
              <div className={s.tarjetaHeader}>
                <div className={s.tarjetaTitulos}>
                  {cat && g.categoria && (
                    <span className={s.chip} style={{ background: `${cat}1a`, color: cat }}>
                      {CAT_LABEL[g.categoria]}
                    </span>
                  )}
                  <span className={s.nombre}>{g.nombre}</span>
                </div>
                <span className={lleno ? s.cupoLleno : s.cupo}>
                  {g.cupoMaximo === null
                    ? `${g.miembrosActivos} integrantes`
                    : `${g.miembrosActivos}/${g.cupoMaximo}${lleno ? ' · completo' : ''}`}
                </span>
              </div>

              <div className={s.miembros}>
                {g.miembros.map((m) => (
                  <span key={m.alumnoId} className={s.miembroChip}>
                    {m.nombre} {m.apellido}
                    <button
                      className={s.quitarX}
                      title={`Quitar a ${m.nombre}`}
                      onClick={() => void quitarMiembro(g, m.alumnoId, m.nombre)}
                    >
                      <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
                        <path d="M18 6L6 18M6 6l12 12" />
                      </svg>
                    </button>
                  </span>
                ))}
                {g.miembros.length === 0 && (
                  <span className={s.sinAlumnos}>Sin alumnos asignados</span>
                )}
              </div>

              <div className={s.tarjetaPie}>
                <label className={s.profeSelect}>
                  <span>Profe</span>
                  <select
                    value={g.profesorUserId ?? ''}
                    onChange={(e) => void reasignarProfe(g.id, e.target.value)}
                  >
                    <option value="">Sin asignar</option>
                    {profes.map((p) => (
                      <option key={p.userId} value={p.userId}>{p.nombre}{p.esDueño ? ' (vos)' : ''}</option>
                    ))}
                  </select>
                </label>
                <button
                  className={s.btnAsignar}
                  disabled={lleno}
                  title={lleno ? 'El grupo está completo' : undefined}
                  onClick={() => setGrupoAsignar(g)}
                >
                  + Asignar alumno
                </button>
              </div>
            </div>
          );
        })}
      </div>

      {modalNuevo && (
        <NuevoGrupoModal
          onClose={() => setModalNuevo(false)}
          onCrear={async (dto) => {
            await crear(dto);
            avisar(`Grupo "${dto.nombre}" creado`);
          }}
        />
      )}
      {grupoAsignar && (
        <AsignarAlumnoModal
          grupo={grupoAsignar}
          onClose={() => setGrupoAsignar(null)}
          onAsignar={async (alumnoId) => {
            await asignar(grupoAsignar.id, alumnoId);
            avisar('Alumno asignado');
          }}
        />
      )}

      {toast && (
        <div className={s.toast}>
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#7bed9f" strokeWidth="2.5" strokeLinecap="round">
            <path d="M20 6L9 17l-5-5" />
          </svg>
          {toast}
        </div>
      )}
    </div>
  );
}
