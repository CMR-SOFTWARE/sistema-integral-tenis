import { useState } from 'react';
import { api, ApiError } from '../../lib/api';
import Avatar from '../../components/Avatar';
import SeleccionarJugadorModal from './SeleccionarJugadorModal';
import { useInvalidarRanking } from './useRanking';
import type { RankingFila } from './types';
import s from './RankingPage.module.css';
import p from '../portal/PortalPages.module.css';

interface Props {
  jugadores: RankingFila[];
  miJugadorId: string;
  onVolver: () => void;
}

/** Pantalla "Nuevo desafío" (singles): elegís el rival por búsqueda y lo
 *  confirmás — el juego arranca de verdad recién cuando el rival acepta. */
export default function NuevoDesafioScreen({ jugadores, miJugadorId, onVolver }: Props) {
  const invalidar = useInvalidarRanking();
  const [rival, setRival] = useState<RankingFila | null>(null);
  const [buscando, setBuscando] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const enviar = async () => {
    if (!rival) return;
    setError(null);
    setEnviando(true);
    try {
      await api.post('/desafios', { rivalJugadorId: rival.jugadorId });
      invalidar();
      onVolver();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo mandar el desafío.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <div>
      <div className={s.volverHeader}>
        <button className={s.volverLink} onClick={onVolver}>← Ranking</button>
      </div>
      <h2 className={s.pantallaTitulo}>Nuevo desafío</h2>
      <p className={s.pantallaSubtitulo}>Elegí el rival. El juego cuenta cuando él acepta.</p>

      {error && <div className={p.error}>{error}</div>}

      {rival ? (
        <div className={s.pillOcupada}>
          <Avatar nombre={rival.nombre} apellido={rival.apellido} size={32} />
          <span className={s.pillNombre}>{rival.nombre} {rival.apellido}</span>
          <button className={s.cambiarLink} onClick={() => setBuscando(true)}>Cambiar</button>
        </div>
      ) : (
        <button className={s.pillVacia} onClick={() => setBuscando(true)}>+ Elegí tu rival</button>
      )}

      <button className={s.btnEnviarDesafio} disabled={!rival || enviando} onClick={() => void enviar()}>
        {enviando ? 'Enviando…' : 'Enviar desafío'}
      </button>

      {buscando && (
        <SeleccionarJugadorModal
          titulo="¿A quién desafiás?"
          jugadores={jugadores}
          excluir={[miJugadorId]}
          onElegir={(j) => { setRival(j); setBuscando(false); }}
          onClose={() => setBuscando(false)}
        />
      )}
    </div>
  );
}
