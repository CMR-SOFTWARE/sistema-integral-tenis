import { useState } from 'react';
import Modal from '../../components/Modal';
import HoraSelect from '../../components/HoraSelect';
import { ApiError } from '../../lib/api';
import { useSedes } from '../agenda/hooks';
import { DIAS } from '../agenda/types';
import type { DiaSemana } from '../agenda/types';
import { MOTIVOS, MOTIVO_LABEL } from './types';
import type { CreateBloqueo, MotivoBloqueo, TipoBloqueo } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';
import b from './BloqueosPage.module.css';

interface Props {
  onClose: () => void;
  /** Devuelve el DTO armado: la página pide el impacto y muestra el modal de confirmación. */
  onContinuar: (dto: CreateBloqueo) => Promise<unknown>;
}

/** Alta de bloqueo: fijo (recurrente semanal) o por rango (fecha puntual con motivo). */
export default function NuevoBloqueoModal({ onClose, onContinuar }: Props) {
  const { sedes } = useSedes();
  const [tipo, setTipo] = useState<TipoBloqueo>('Rango');
  const [dia, setDia] = useState<DiaSemana>('Monday');
  const [fecha, setFecha] = useState('');
  const [horaInicio, setHoraInicio] = useState('');
  const [horaFin, setHoraFin] = useState('');
  const [diaCompleto, setDiaCompleto] = useState(false);
  const [canchaId, setCanchaId] = useState(''); // '' = todas
  const [motivo, setMotivo] = useState<MotivoBloqueo>('MalClima');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const incompleto =
    (!diaCompleto && (horaInicio === '' || horaFin === '')) ||
    (tipo === 'Rango' && fecha === '');

  const continuar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onContinuar({
        tipo,
        dia: tipo === 'Fijo' ? dia : undefined,
        fecha: tipo === 'Rango' ? fecha : undefined,
        // Día completo = franja 00:00–23:59 (el backend no necesita distinguirlo)
        horaInicio: diaCompleto ? '00:00' : horaInicio,
        horaFin: diaCompleto ? '23:59' : horaFin,
        canchaId: canchaId === '' ? undefined : canchaId,
        motivo: tipo === 'Rango' ? motivo : undefined,
      });
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo calcular el impacto.');
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Nuevo bloqueo"
      subtitulo="Marcá una franja como no disponible; los turnos que pise se cancelan."
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={continuar} disabled={enviando || incompleto}>
            {enviando ? 'Calculando…' : 'Ver impacto'}
          </button>
        </>
      }
    >
      <div className={s.grid}>
        <div className={`${s.span2} ${b.tipos}`}>
          <button
            type="button"
            className={tipo === 'Rango' ? b.tipoActivo : b.tipoBtn}
            onClick={() => setTipo('Rango')}
          >
            Por rango
            <small>Una fecha puntual (lluvia, torneo…)</small>
          </button>
          <button
            type="button"
            className={tipo === 'Fijo' ? b.tipoActivo : b.tipoBtn}
            onClick={() => setTipo('Fijo')}
          >
            Fijo
            <small>Se repite todas las semanas</small>
          </button>
        </div>

        {tipo === 'Rango' ? (
          <>
            <label className={s.campo}>
              <span>Fecha</span>
              <input type="date" value={fecha} onChange={(e) => setFecha(e.target.value)} />
            </label>
            <label className={s.campo}>
              <span>Motivo</span>
              <select value={motivo} onChange={(e) => setMotivo(e.target.value as MotivoBloqueo)}>
                {MOTIVOS.map((m) => (
                  <option key={m} value={m}>{MOTIVO_LABEL[m]}</option>
                ))}
              </select>
            </label>
          </>
        ) : (
          <label className={`${s.campo} ${s.span2}`}>
            <span>Día de la semana</span>
            <select value={dia} onChange={(e) => setDia(e.target.value as DiaSemana)}>
              {DIAS.map((d) => (
                <option key={d.valor} value={d.valor}>{d.label}</option>
              ))}
            </select>
          </label>
        )}

        <label className={`${s.span2} ${b.checkDiaCompleto}`}>
          <input
            type="checkbox"
            checked={diaCompleto}
            onChange={(e) => setDiaCompleto(e.target.checked)}
          />
          <span>Día completo (bloquea de 00:00 a 23:59)</span>
        </label>

        {!diaCompleto && (
          <>
            <label className={s.campo}>
              <span>Desde</span>
              <HoraSelect value={horaInicio} onChange={setHoraInicio} />
            </label>
            <label className={s.campo}>
              <span>Hasta</span>
              <HoraSelect value={horaFin} onChange={setHoraFin} />
            </label>
          </>
        )}

        <label className={`${s.campo} ${s.span2}`}>
          <span>Cancha afectada</span>
          <select value={canchaId} onChange={(e) => setCanchaId(e.target.value)}>
            <option value="">Todas las canchas</option>
            {sedes.flatMap((sede) =>
              sede.canchas.map((c) => (
                <option key={c.id} value={c.id}>{sede.nombre} — {c.nombre}</option>
              )),
            )}
          </select>
        </label>

        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
