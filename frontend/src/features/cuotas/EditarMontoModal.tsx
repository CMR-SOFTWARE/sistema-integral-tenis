import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import s from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  concepto: string;
  montoActual: number;
  onClose: () => void;
  onGuardar: (monto: number) => Promise<unknown>;
}

/** Cambiar el monto de un cargo impago (ej. ajustar la cuota del mes al cobrar). */
export default function EditarMontoModal({ concepto, montoActual, onClose, onGuardar }: Props) {
  const [monto, setMonto] = useState(montoActual.toString());
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const guardar = async () => {
    const n = Number(monto);
    if (Number.isNaN(n) || n < 0) {
      setError('Poné un monto válido (un número sin puntos de miles).');
      return;
    }
    setError(null);
    setEnviando(true);
    try {
      await onGuardar(n);
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo guardar el monto.');
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Cambiar el monto"
      subtitulo={concepto}
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={guardar} disabled={enviando || monto.trim() === ''}>
            {enviando ? 'Guardando…' : 'Guardar'}
          </button>
        </>
      }
    >
      <div className={s.grid}>
        <label className={`${s.campo} ${s.span2}`}>
          <span>Monto</span>
          <input
            type="number"
            min={0}
            value={monto}
            onChange={(e) => setMonto(e.target.value)}
            onWheel={(e) => e.currentTarget.blur()}
            onKeyDown={(e) => e.key === 'Enter' && void guardar()}
            autoFocus
          />
        </label>
        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
