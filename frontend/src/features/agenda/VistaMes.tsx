import { useState } from 'react';
import { aISO, DIAS, diaDelMes, diasDelMesGrid, fechaCorta, horaCorta } from './types';
import type { Turno } from './types';
import { cubreFecha, franjaLegible } from '../bloqueos/types';
import type { Bloqueo } from '../bloqueos/types';
import s from './VistaMes.module.css';

interface Props {
  anio: number;
  mes: number; // 1-12
  /** Turnos del mes, YA filtrados (sede/profe) por el calendario. */
  turnos: Turno[];
  bloqueos: Bloqueo[];
  onAbrirTurno: (turno: Turno) => void;
}

const MAX_CHIPS = 3;

/** Vista mensual estilo calendario: el mes resumido + detalle del día al hacer click. */
export default function VistaMes({ anio, mes, turnos, bloqueos, onAbrirTurno }: Props) {
  const dias = diasDelMesGrid(anio, mes);
  const hoyIso = aISO(new Date());
  // Arranca en hoy si el mes mostrado es el actual; si no, en el día 1.
  const primero = `${anio}-${String(mes).padStart(2, '0')}-01`;
  const [sel, setSel] = useState<string>(hoyIso.slice(0, 7) === primero.slice(0, 7) ? hoyIso : primero);

  const delDia = (iso: string) =>
    turnos.filter((t) => t.fecha === iso).sort((a, b) => a.horaInicio.localeCompare(b.horaInicio));
  const bloqueosDe = (iso: string) => bloqueos.filter((b) => cubreFecha(b, iso));

  const turnosSel = delDia(sel);
  const bloqueosSel = bloqueosDe(sel);

  return (
    <div>
      <div className={s.grilla}>
        {DIAS.map((d) => (
          <div key={d.valor} className={s.encabezado}>{d.corto}</div>
        ))}
        {dias.map(({ iso, enMes }) => {
          const items = delDia(iso);
          const bloqueado = bloqueosDe(iso).length > 0;
          return (
            <button
              key={iso}
              className={[
                s.celda,
                !enMes ? s.celdaFuera : '',
                iso === hoyIso ? s.celdaHoy : '',
                iso === sel ? s.celdaSel : '',
              ].join(' ')}
              onClick={() => setSel(iso)}
            >
              <div className={s.celdaNum}>
                {diaDelMes(iso)}
                {bloqueado && <span className={s.bloqueoDot} title="Hay un bloqueo" />}
              </div>
              <div className={s.chips}>
                {items.slice(0, MAX_CHIPS).map((t) => (
                  <div
                    key={t.id}
                    className={`${s.chip} ${t.estado === 'Cancelado' ? s.chipCancelado : ''}`}
                    title={`${horaCorta(t.horaInicio)} ${t.titulo}`}
                  >
                    <span className={s.chipHora}>{horaCorta(t.horaInicio)}</span> {t.titulo}
                  </div>
                ))}
                {items.length > MAX_CHIPS && (
                  <div className={s.chipMas}>+{items.length - MAX_CHIPS} más</div>
                )}
              </div>
            </button>
          );
        })}
      </div>

      {/* Detalle del día seleccionado */}
      <div className={s.detalle}>
        <div className={s.detalleTitulo}>{fechaCorta(sel)}</div>
        {turnosSel.length === 0 && bloqueosSel.length === 0 && (
          <div className={s.detalleVacio}>Sin clases este día.</div>
        )}
        {bloqueosSel.map((b) => (
          <div key={b.id} className={s.detalleBloqueo}>
            <span className={s.detalleHora}>{franjaLegible(b.horaInicio, b.horaFin)}</span>
            <span>{b.motivo ?? 'Bloqueo fijo'}{b.cancha ? ` · ${b.cancha}` : ''}</span>
          </div>
        ))}
        {turnosSel.map((t) => {
          const cancelado = t.estado === 'Cancelado';
          const ausentes = t.participantes.filter((p) => !p.presente).length;
          return (
            <button
              key={t.id}
              className={`${s.detalleTurno} ${cancelado ? s.detalleCancelado : ''}`}
              onClick={() => onAbrirTurno(t)}
            >
              <span className={s.detalleHora}>{horaCorta(t.horaInicio)}</span>
              <span className={s.detalleNombre}>{t.titulo}</span>
              <span className={s.detalleInfo}>
                {cancelado
                  ? `Cancelado: ${t.canceladoMotivo}`
                  : `${t.cancha} · ${t.participantes.length} 👤${ausentes > 0 ? ` · ${ausentes} falta${ausentes > 1 ? 's' : ''}` : ''}`}
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
