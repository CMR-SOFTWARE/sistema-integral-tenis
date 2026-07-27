import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import type { Medio } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  nombre: string;
  /** Monto pre-cargado (lo calculado); el head pro lo puede ajustar. */
  montoSugerido: number;
  onClose: () => void;
  onPagar: (monto: number, medio: Medio) => Promise<void>;
}

/** Registrar el pago del sueldo: monto ajustable + cómo se pagó. */
export default function PagarSueldoModal({ nombre, montoSugerido, onClose, onPagar }: Props) {
  const [monto, setMonto] = useState(String(montoSugerido));
  const [medio, setMedio] = useState<Medio>('Transferencia');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const confirmar = async () => {
    setError(null);
    const n = Number(monto);
    if (!Number.isFinite(n) || n < 0) {
      setError('Poné un monto válido.');
      return;
    }
    setEnviando(true);
    try {
      await onPagar(n, medio);
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo registrar el pago.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Pagar sueldo"
      subtitulo={nombre}
      onClose={onClose}
      ancho={420}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={() => void confirmar()} disabled={enviando}>
            {enviando ? 'Registrando…' : 'Registrar pago'}
          </button>
        </>
      }
    >
      <div className={s.grid}>
        <label className={s.campo}>
          <span>Monto</span>
          <input
            type="number"
            min={0}
            value={monto}
            onChange={(e) => setMonto(e.target.value)}
            onWheel={(e) => e.currentTarget.blur()}
          />
        </label>
        <label className={s.campo}>
          <span>¿Cómo le pagaste?</span>
          <select value={medio} onChange={(e) => setMedio(e.target.value as Medio)}>
            <option value="Efectivo">Efectivo</option>
            <option value="Transferencia">Transferencia</option>
            <option value="Otro">Otro</option>
          </select>
        </label>
        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
