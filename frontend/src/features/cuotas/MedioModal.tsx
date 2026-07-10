import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import type { Medio } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  titulo: string;
  subtitulo: string;
  onClose: () => void;
  onConfirmar: (medio: Medio) => Promise<void>;
}

/** Elegir el medio de pago al confirmar (Efectivo / Transferencia / Otro). */
export default function MedioModal({ titulo, subtitulo, onClose, onConfirmar }: Props) {
  const [medio, setMedio] = useState<Medio>('Efectivo');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const confirmar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onConfirmar(medio);
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo registrar el pago.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo={titulo}
      subtitulo={subtitulo}
      onClose={onClose}
      ancho={420}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={() => void confirmar()} disabled={enviando}>
            {enviando ? 'Registrando…' : 'Confirmar pago'}
          </button>
        </>
      }
    >
      <div className={s.grid}>
        <label className={`${s.campo} ${s.span2}`}>
          <span>¿Cómo pagó?</span>
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
