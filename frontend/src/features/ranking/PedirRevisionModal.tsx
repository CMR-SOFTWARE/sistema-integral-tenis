import { useState } from 'react';
import Modal from '../../components/Modal';
import { api, ApiError } from '../../lib/api';
import s from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  juegoPendienteId?: string;
  juegoDoblesPendienteId?: string;
  subtitulo: string;
  onClose: () => void;
}

/** Pedido de revisión sobre un partido ya finalizado — es un ticket para que
 *  lo vea un admin de plataforma, no corrige el resultado solo. */
export default function PedirRevisionModal({ juegoPendienteId, juegoDoblesPendienteId, subtitulo, onClose }: Props) {
  const [comentario, setComentario] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [enviado, setEnviado] = useState(false);

  const enviar = async () => {
    if (comentario.trim().length < 10) {
      setError('Contanos qué pasó con al menos 10 caracteres.');
      return;
    }
    setError(null);
    setEnviando(true);
    try {
      await api.post('/revisiones', { juegoPendienteId, juegoDoblesPendienteId, comentario: comentario.trim() });
      setEnviado(true);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo enviar el pedido.');
    } finally {
      setEnviando(false);
    }
  };

  if (enviado) {
    return (
      <Modal titulo="Pedido enviado" subtitulo={subtitulo} onClose={onClose} footer={
        <button className={s.btnPrimario} onClick={onClose}>Listo</button>
      }>
        <p>Un admin de la plataforma lo va a revisar. Te avisamos cuando responda.</p>
      </Modal>
    );
  }

  return (
    <Modal
      titulo="Pedir revisión"
      subtitulo={subtitulo}
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} disabled={enviando} onClick={() => void enviar()}>
            {enviando ? 'Enviando…' : 'Enviar pedido'}
          </button>
        </>
      }
    >
      <p>Contanos qué te parece que está mal con el resultado cargado.</p>
      <textarea
        rows={4}
        style={{ width: '100%', boxSizing: 'border-box', padding: '9px 11px', borderRadius: 'var(--radius-md)', border: '1px solid var(--color-border)', fontFamily: 'inherit', fontSize: 14 }}
        value={comentario}
        onChange={(e) => setComentario(e.target.value)}
        placeholder="Ej: el ganador que cargaron no es el que jugó..."
      />
      {error && <div className={s.error}>{error}</div>}
    </Modal>
  );
}
