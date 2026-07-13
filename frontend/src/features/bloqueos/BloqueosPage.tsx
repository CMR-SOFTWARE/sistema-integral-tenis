import { useState } from 'react';
import { useBloqueos } from './useBloqueos';
import NuevoBloqueoModal from './NuevoBloqueoModal';
import ImpactoModal from './ImpactoModal';
import { DIAS, fechaCorta } from '../agenda/types';
import { franjaLegible } from './types';
import type { CreateBloqueo, Impacto } from './types';
import s from './BloqueosPage.module.css';

interface Pendiente {
  dto: CreateBloqueo;
  impacto: Impacto;
}

/** Bloqueos de agenda: franjas no disponibles (fijas o por fecha) con
 *  cancelación en cascada de los turnos pisados. */
export default function BloqueosPage() {
  const { bloqueos, cargando, error, previsualizar, crear, eliminar } = useBloqueos();
  const [modalNuevo, setModalNuevo] = useState(false);
  const [pendiente, setPendiente] = useState<Pendiente | null>(null);
  const [toast, setToast] = useState<string | null>(null);

  const avisar = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 3500);
  };

  // Paso 1 (modal alta) → calcular impacto → paso 2 (modal confirmación)
  const continuar = async (dto: CreateBloqueo) => {
    const impacto = await previsualizar(dto);
    setModalNuevo(false);
    setPendiente({ dto, impacto });
  };

  const confirmar = async () => {
    if (!pendiente) return;
    await crear(pendiente.dto);
    const n = pendiente.impacto.turnosAfectados;
    setPendiente(null);
    avisar(n > 0 ? `Bloqueo creado: se cancelaron ${n} turno${n > 1 ? 's' : ''}.` : 'Bloqueo creado.');
  };

  const borrar = async (id: string) => {
    if (!window.confirm('¿Eliminar este bloqueo? Los turnos futuros de la franja vuelven a generarse.')) return;
    await eliminar(id);
    avisar('Bloqueo eliminado.');
  };

  const diaLabel = (dia: string | null) =>
    DIAS.find((d) => d.valor === dia)?.label ?? '';

  return (
    <div>
      <div className={s.toolbar}>
        <div className={s.titulo}>
          Franjas no disponibles: los turnos que pisan se cancelan y los futuros no se generan.
        </div>
        <button className={s.btnNuevo} onClick={() => setModalNuevo(true)}>+ Nuevo bloqueo</button>
      </div>

      {error && <div className={s.error}>{error} — ¿está corriendo la API?</div>}
      {cargando && <div className={s.vacio}>Cargando…</div>}

      {!cargando && !error && bloqueos.length === 0 && (
        <div className={s.vacioCard}>
          No hay bloqueos. Creá uno para marcar lluvia, torneos, mantenimiento
          o tu franja semanal sin clases.
        </div>
      )}

      {!cargando && bloqueos.length > 0 && (
        <div className={s.lista}>
          {bloqueos.map((b) => (
            <div key={b.id} className={s.fila}>
              <span className={b.tipo === 'Fijo' ? s.chipFijo : s.chipRango}>
                {b.tipo === 'Fijo' ? 'Fijo' : 'Rango'}
              </span>
              <div className={s.cuerpo}>
                <div className={s.cuando}>
                  {b.tipo === 'Fijo'
                    ? `Todos los ${diaLabel(b.dia).toLowerCase()}`
                    : fechaCorta(b.fecha!)}
                  {' · '}
                  {franjaLegible(b.horaInicio, b.horaFin)}
                </div>
                <div className={s.detalle}>
                  {b.cancha ?? 'Todas las canchas'}
                  {b.motivo && ` · ${b.motivo}`}
                </div>
              </div>
              <button className={s.btnEliminar} onClick={() => borrar(b.id)}>Eliminar</button>
            </div>
          ))}
        </div>
      )}

      {modalNuevo && (
        <NuevoBloqueoModal onClose={() => setModalNuevo(false)} onContinuar={continuar} />
      )}
      {pendiente && (
        <ImpactoModal
          impacto={pendiente.impacto}
          onClose={() => setPendiente(null)}
          onConfirmar={confirmar}
        />
      )}
      {toast && <div className={s.toast}>{toast}</div>}
    </div>
  );
}
