import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import { fechaCorta, horaCorta } from '../agenda/types';
import type { MiTurno } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';
import p from './PortalPages.module.css';

const MOTIVOS = ['Enfermedad', 'Trabajo', 'Viaje', 'Lesión'];

interface Props {
  turno: MiTurno;
  onClose: () => void;
  onCancelar: (turnoId: string, motivo: string) => Promise<unknown>;
}

/** El alumno avisa que no viene (mockup): motivo predefinido o texto libre.
 *  Es un AVISO: el turno sigue para el resto y la clase se cobra igual. */
export default function CancelarTurnoModal({ turno, onClose, onCancelar }: Props) {
  const [motivo, setMotivo] = useState('');
  const [libre, setLibre] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const motivoFinal = libre.trim() !== '' ? libre.trim() : motivo;

  const confirmar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onCancelar(turno.id, motivoFinal);
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo cancelar el turno.');
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Cancelar turno"
      subtitulo={`${turno.titulo} · ${fechaCorta(turno.fecha)} ${horaCorta(turno.horaInicio)} hs`}
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Volver</button>
          <button
            className={`${s.btnPrimario} ${p.btnPeligro}`}
            onClick={confirmar}
            disabled={enviando || motivoFinal === ''}
          >
            {enviando ? 'Cancelando…' : 'Cancelar turno'}
          </button>
        </>
      }
    >
      <div className={p.avisoCancelacion}>
        Tu profe va a ver el aviso. La clase se cobra igual (la recuperación
        queda a su criterio).
      </div>

      <div className={p.motivos}>
        {MOTIVOS.map((m) => (
          <button
            key={m}
            type="button"
            className={motivo === m && libre.trim() === '' ? p.motivoChipActivo : p.motivoChip}
            onClick={() => setMotivo(m)}
          >
            {m}
          </button>
        ))}
      </div>

      <label className={s.campo}>
        <span>Otro motivo (opcional)</span>
        <input
          value={libre}
          onChange={(e) => setLibre(e.target.value)}
          placeholder="Contanos brevemente…"
          maxLength={200}
        />
      </label>

      {error && <div className={s.error}>{error}</div>}
    </Modal>
  );
}
