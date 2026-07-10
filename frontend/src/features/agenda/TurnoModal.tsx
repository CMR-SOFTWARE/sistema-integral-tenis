import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import { fechaCorta, horaCorta } from './types';
import type { Turno } from './types';
import s from './TurnoModal.module.css';
import m from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  turno: Turno;
  onClose: () => void;
  onAsistencia: (turnoId: string, alumnoId: string, presente: boolean) => Promise<void>;
  onCancelar: (turnoId: string, motivo: string) => Promise<void>;
}

/** Detalle del turno: asistencia (un tap marca la falta) y cancelación con motivo. */
export default function TurnoModal({ turno, onClose, onAsistencia, onCancelar }: Props) {
  const [cancelando, setCancelando] = useState(false);
  const [motivo, setMotivo] = useState('');
  const [error, setError] = useState<string | null>(null);
  const cancelado = turno.estado === 'Cancelado';

  const toggle = async (alumnoId: string, presente: boolean) => {
    setError(null);
    try {
      await onAsistencia(turno.id, alumnoId, presente);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo actualizar la asistencia.');
    }
  };

  const confirmarCancelacion = async () => {
    setError(null);
    try {
      await onCancelar(turno.id, motivo.trim());
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo cancelar el turno.');
    }
  };

  return (
    <Modal
      titulo={turno.titulo}
      subtitulo={`${fechaCorta(turno.fecha)} · ${horaCorta(turno.horaInicio)} (${turno.duracionMinutos}') · ${turno.sede} · ${turno.cancha}`}
      onClose={onClose}
      footer={
        cancelado ? (
          <span className={s.canceladoNota}>
            Turno cancelado — {turno.canceladoMotivo}
          </span>
        ) : cancelando ? (
          <>
            <input
              autoFocus
              className={s.motivoInput}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
              placeholder="Motivo (ej: lluvia)"
            />
            <button className={m.btnSecundario} onClick={() => setCancelando(false)}>Volver</button>
            <button
              className={s.btnPeligro}
              disabled={motivo.trim() === ''}
              onClick={() => void confirmarCancelacion()}
            >
              Confirmar cancelación
            </button>
          </>
        ) : (
          <>
            <button className={s.btnPeligroSuave} onClick={() => setCancelando(true)}>
              Cancelar turno
            </button>
            <button className={m.btnPrimario} onClick={onClose}>Listo</button>
          </>
        )
      }
    >
      <div className={s.seccion}>Asistencia ({turno.participantes.length})</div>
      <div className={s.lista}>
        {turno.participantes.map((p) => (
          <div key={p.alumnoId} className={s.fila}>
            <span className={`${s.estadoDot} ${p.presente ? s.dotPresente : s.dotAusente}`} />
            <span className={s.nombre}>
              {p.nombre} {p.apellido}
              {p.deudaVencida && <span className={s.deudaBadge}>cuota vencida</span>}
            </span>
            {!cancelado && (
              <button
                className={p.presente ? s.btnFalto : s.btnVino}
                onClick={() => void toggle(p.alumnoId, !p.presente)}
              >
                {p.presente ? 'Marcar falta' : 'Estuvo presente'}
              </button>
            )}
            {cancelado && (
              <span className={s.presenteLabel}>{p.presente ? '' : 'había faltado'}</span>
            )}
          </div>
        ))}
        {turno.participantes.length === 0 && (
          <div className={s.vacio}>Sin participantes (el grupo estaba vacío al generarse).</div>
        )}
      </div>
      <p className={s.nota}>
        La asistencia no cambia lo que se cobra: el que falta paga igual
        (la recuperación queda a tu criterio).
      </p>
      {error && <div className={s.error}>{error}</div>}
    </Modal>
  );
}
