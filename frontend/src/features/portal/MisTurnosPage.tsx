import { useCallback, useEffect, useState } from 'react';
import { api } from '../../lib/api';
import { CAT_COLOR, CAT_LABEL } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import { fechaCorta, horaCorta, DIAS } from '../agenda/types';
import CancelarTurnoModal from './CancelarTurnoModal';
import type { MisTurnos, MiTurno } from './types';
import s from './PortalPages.module.css';

function diaCorto(iso: string): string {
  const d = new Date(`${iso}T00:00:00`);
  return DIAS[(d.getDay() + 6) % 7].corto.toUpperCase();
}

function ChipCategoria({ categoria }: { categoria: string | null }) {
  if (!categoria) return null;
  const color = CAT_COLOR[categoria as Categoria] ?? '#6b7770';
  return (
    <span className={s.chipCat} style={{ background: `${color}1a`, color }}>
      {CAT_LABEL[categoria as Categoria] ?? categoria}
    </span>
  );
}

/** Mis turnos (mockup): próximos con compañeros (con aviso de cancelación),
 *  y el historial reciente. */
export default function MisTurnosPage() {
  const [turnos, setTurnos] = useState<MisTurnos | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [aCancelar, setACancelar] = useState<MiTurno | null>(null);

  const cargar = useCallback(() => {
    api.get<MisTurnos>('/portal/mis-turnos')
      .then(setTurnos)
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando tus turnos'));
  }, []);

  useEffect(() => {
    cargar();
  }, [cargar]);

  const cancelar = async (turnoId: string, motivo: string) => {
    await api.post(`/portal/mis-turnos/${turnoId}/cancelar`, { motivo });
    cargar();
  };

  if (error) return <div className={s.error}>{error}</div>;
  if (!turnos) return <div className={s.vacio}>Cargando tus clases…</div>;

  const estadoHistorial = (t: MiTurno) =>
    t.estado === 'Cancelado'
      ? <span className={`${s.chip} ${s.chipRojo}`} title={t.canceladoMotivo ?? ''}>Cancelada</span>
      : t.canceladoPorMi
        ? <span className={`${s.chip} ${s.chipAmbar}`}>Avisaste que no ibas</span>
        : t.presente
          ? <span className={`${s.chip} ${s.chipVerde}`}>Asistió</span>
          : <span className={`${s.chip} ${s.chipGris}`}>Faltó</span>;

  const estadoProximo = (t: MiTurno) =>
    t.estado === 'Cancelado'
      ? <span className={`${s.chip} ${s.chipRojo}`} title={t.canceladoMotivo ?? ''}>Cancelada</span>
      : t.canceladoPorMi
        ? <span className={`${s.chip} ${s.chipAmbar}`}>Cancelado por vos</span>
        : <span className={`${s.chip} ${s.chipVerde}`}>Confirmada</span>;

  return (
    <div>
      <h2 className={s.seccion}>Próximos turnos</h2>
      {turnos.proximos.length === 0 && (
        <div className={s.tarjeta}><div className={s.vacio}>No tenés clases programadas por ahora.</div></div>
      )}
      <div className={s.lista}>
        {turnos.proximos.map((t) => (
          <div key={t.id} className={`${s.turno} ${t.estado === 'Cancelado' ? s.turnoCancelado : ''}`}>
            <div className={s.horarioDia}>
              <div className={s.horarioDiaNombre}>{diaCorto(t.fecha)}</div>
              <div className={s.turnoHoraGrande}>{horaCorta(t.horaInicio)}</div>
            </div>
            <div className={s.turnoInfo}>
              <div className={s.turnoTituloFila}>
                <ChipCategoria categoria={t.categoria} />
                <span className={s.turnoTitulo}>{t.titulo}</span>
              </div>
              <div className={s.turnoDetalle}>
                {fechaCorta(t.fecha)} · {t.sede} · {t.cancha}
                {t.companeros.length > 0 && <> · con {t.companeros.map((c) => c.split(' ')[0]).join(', ')}</>}
              </div>
            </div>
            {estadoProximo(t)}
            {t.puedoCancelar && (
              <button className={s.btnCancelarTurno} onClick={() => setACancelar(t)}>
                Cancelar
              </button>
            )}
          </div>
        ))}
      </div>

      <h2 className={s.seccion}>Historial</h2>
      {turnos.historial.length === 0 && (
        <div className={s.tarjeta}><div className={s.vacio}>Todavía no hay clases pasadas para mostrar.</div></div>
      )}
      {turnos.historial.length > 0 && (
        <div className={s.tarjeta}>
          {turnos.historial.map((t) => (
            <div key={t.id} className={s.historialFila}>
              <ChipCategoria categoria={t.categoria} />
              <span className={s.historialFecha}>
                {fechaCorta(t.fecha)} · {horaCorta(t.horaInicio)} hs
              </span>
              <span className={s.historialTitulo}>{t.titulo}</span>
              {estadoHistorial(t)}
            </div>
          ))}
        </div>
      )}

      {aCancelar && (
        <CancelarTurnoModal
          turno={aCancelar}
          onClose={() => setACancelar(null)}
          onCancelar={cancelar}
        />
      )}
    </div>
  );
}
