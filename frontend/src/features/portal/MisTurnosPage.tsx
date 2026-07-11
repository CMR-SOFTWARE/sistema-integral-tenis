import { useEffect, useState } from 'react';
import { api } from '../../lib/api';
import { CAT_COLOR, CAT_LABEL } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import { fechaCorta, horaCorta, DIAS } from '../agenda/types';
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

/** Mis turnos (mockup): próximos con compañeros, y el historial reciente. */
export default function MisTurnosPage() {
  const [turnos, setTurnos] = useState<MisTurnos | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<MisTurnos>('/portal/mis-turnos')
      .then(setTurnos)
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando tus turnos'));
  }, []);

  if (error) return <div className={s.error}>{error}</div>;
  if (!turnos) return <div className={s.vacio}>Cargando tus clases…</div>;

  const estadoHistorial = (t: MiTurno) =>
    t.estado === 'Cancelado'
      ? <span className={`${s.chip} ${s.chipRojo}`} title={t.canceladoMotivo ?? ''}>Cancelada</span>
      : t.presente
        ? <span className={`${s.chip} ${s.chipVerde}`}>Asistió</span>
        : <span className={`${s.chip} ${s.chipGris}`}>Faltó</span>;

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
            {t.estado === 'Cancelado'
              ? <span className={`${s.chip} ${s.chipRojo}`} title={t.canceladoMotivo ?? ''}>Cancelada</span>
              : <span className={`${s.chip} ${s.chipVerde}`}>Confirmada</span>}
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
    </div>
  );
}
