import { useState } from 'react';
import Modal from '../../components/Modal';
import { api, ApiError } from '../../lib/api';
import { useInvalidarRankingDobles } from './useRankingDobles';
import type { DesafioDobles } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';
import r from './RankingPage.module.css';

interface Props {
  desafio: DesafioDobles;
  onClose: () => void;
}

/** Cargar quién ganó el partido de dobles — se elige la PAREJA (no un
 *  jugador individual), pero el back necesita un jugadorId cualquiera de
 *  esa pareja para resolver el equipo ganador. */
export default function CargarResultadoDoblesModal({ desafio, onClose }: Props) {
  const invalidar = useInvalidarRankingDobles();
  const [ganadorId, setGanadorId] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const guardar = async () => {
    if (!ganadorId) return;
    setError(null);
    setEnviando(true);
    try {
      await api.post(`/desafios/dobles/${desafio.id}/finalizar`, { ganadorJugadorId: ganadorId });
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
      subtitulo={`${desafio.jugador1Nombre} y ${desafio.jugador2Nombre} vs. ${desafio.rival1Nombre} y ${desafio.rival2Nombre}`}
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
      <p>¿Qué pareja ganó?</p>
      <div className={r.opcionesGanador}>
        <label className={r.opcionGanador}>
          <input
            type="radio"
            name="ganador"
            checked={ganadorId === desafio.jugador1Id}
            onChange={() => setGanadorId(desafio.jugador1Id)}
          />
          <span>{desafio.jugador1Nombre} y {desafio.jugador2Nombre}</span>
        </label>
        <label className={r.opcionGanador}>
          <input
            type="radio"
            name="ganador"
            checked={ganadorId === desafio.rival1Id}
            onChange={() => setGanadorId(desafio.rival1Id)}
          />
          <span>{desafio.rival1Nombre} y {desafio.rival2Nombre}</span>
        </label>
      </div>
      {error && <div className={s.error}>{error}</div>}
    </Modal>
  );
}
