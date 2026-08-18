import { useState } from 'react';
import Modal from '../../components/Modal';
import { api, ApiError } from '../../lib/api';
import { useInvalidarRanking } from './useRanking';
import type { Desafio } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';
import r from './RankingPage.module.css';

interface Props {
  desafio: Desafio;
  onClose: () => void;
}

/** Cargar quién ganó — nada de resultado en texto (no "6-4 6-4"), solo el ganador. */
export default function CargarResultadoModal({ desafio, onClose }: Props) {
  const invalidar = useInvalidarRanking();
  const [ganadorId, setGanadorId] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const guardar = async () => {
    if (!ganadorId) return;
    setError(null);
    setEnviando(true);
    try {
      await api.post(`/desafios/${desafio.id}/finalizar`, { ganadorJugadorId: ganadorId });
      invalidar();
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo cargar el resultado.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Cargar resultado"
      subtitulo={`${desafio.jugador1Nombre} vs. ${desafio.jugador2Nombre}`}
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} disabled={enviando || !ganadorId} onClick={() => void guardar()}>
            {enviando ? 'Guardando…' : 'Confirmar'}
          </button>
        </>
      }
    >
      <p>¿Quién ganó?</p>
      <div className={r.opcionesGanador}>
        <label className={`${r.opcionGanador}${ganadorId === desafio.jugador1Id ? ' motion-match-win' : ''}`}>
          <input
            type="radio"
            name="ganador"
            checked={ganadorId === desafio.jugador1Id}
            onChange={() => setGanadorId(desafio.jugador1Id)}
          />
          <span>{desafio.jugador1Nombre}</span>
        </label>
        <label className={`${r.opcionGanador}${ganadorId === desafio.jugador2Id ? ' motion-match-win' : ''}`}>
          <input
            type="radio"
            name="ganador"
            checked={ganadorId === desafio.jugador2Id}
            onChange={() => setGanadorId(desafio.jugador2Id)}
          />
          <span>{desafio.jugador2Nombre}</span>
        </label>
      </div>
      {error && <div className={`${s.error} motion-net`}>{error}</div>}
    </Modal>
  );
}
