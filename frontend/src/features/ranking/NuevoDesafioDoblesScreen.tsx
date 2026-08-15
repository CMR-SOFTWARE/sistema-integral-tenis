import { useMemo, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import Avatar from '../../components/Avatar';
import SeleccionarJugadorModal from './SeleccionarJugadorModal';
import { useInvalidarRankingDobles } from './useRankingDobles';
import type { RankingFilaDobles } from './types';
import s from './RankingPage.module.css';
import p from '../portal/PortalPages.module.css';

interface Props {
  jugadores: RankingFilaDobles[];
  miJugadorId: string;
  miNombre: string;
  miApellido: string;
  onVolver: () => void;
}

type Slot = 'companero' | 'rival1' | 'rival2';

const slotTitulo: Record<Slot, string> = {
  companero: 'Elegí a tu compañero',
  rival1: 'Elegí al primer rival',
  rival2: 'Elegí al segundo rival',
};

/** Pantalla "Nuevo desafío de dobles": elegís pareja + los dos rivales por
 *  búsqueda. Cuenta cuando uno de los dos rivales acepta. */
export default function NuevoDesafioDoblesScreen({ jugadores, miJugadorId, miNombre, miApellido, onVolver }: Props) {
  const invalidar = useInvalidarRankingDobles();
  const [seleccion, setSeleccion] = useState<Record<Slot, RankingFilaDobles | null>>({
    companero: null, rival1: null, rival2: null,
  });
  const [buscando, setBuscando] = useState<Slot | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const excluidos = useMemo(
    () => [miJugadorId, ...Object.values(seleccion).filter(Boolean).map((j) => j!.jugadorId)],
    [miJugadorId, seleccion],
  );

  const elegir = (slot: Slot, jugador: RankingFilaDobles) => {
    setSeleccion((prev) => ({ ...prev, [slot]: jugador }));
    setBuscando(null);
  };

  const listo = seleccion.companero && seleccion.rival1 && seleccion.rival2;

  const enviar = async () => {
    if (!listo) return;
    setError(null);
    setEnviando(true);
    try {
      await api.post('/desafios/dobles', {
        companeroJugadorId: seleccion.companero!.jugadorId,
        rival1JugadorId: seleccion.rival1!.jugadorId,
        rival2JugadorId: seleccion.rival2!.jugadorId,
      });
      invalidar();
      onVolver();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo armar el partido.');
    } finally {
      setEnviando(false);
    }
  };

  const Pill = ({ slot }: { slot: Slot }) => {
    const j = seleccion[slot];
    if (!j) {
      return <button className={s.pillVacia} onClick={() => setBuscando(slot)}>+ {slotTitulo[slot]}</button>;
    }
    return (
      <div className={s.pillOcupada}>
        <Avatar nombre={j.nombre} apellido={j.apellido} size={32} />
        <span className={s.pillNombre}>{j.nombre} {j.apellido}</span>
        <button className={s.cambiarLink} onClick={() => setBuscando(slot)}>Cambiar</button>
      </div>
    );
  };

  return (
    <div>
      <div className={s.volverHeader}>
        <button className={s.volverLink} onClick={onVolver}>← Ranking dobles</button>
      </div>
      <h2 className={s.pantallaTitulo}>Nuevo desafío de dobles</h2>
      <p className={s.pantallaSubtitulo}>Elegí tu pareja y a los dos rivales. Cuenta cuando uno de los dos rivales acepta.</p>

      {error && <div className={p.error}>{error}</div>}

      <div className={s.grupoLabel}>Tu pareja</div>
      <Pill slot="companero" />

      <div className={s.grupoLabel}>Pareja rival</div>
      <Pill slot="rival1" />
      <div style={{ marginTop: 8 }}><Pill slot="rival2" /></div>

      {listo && (
        <p className={s.resumenDesafio}>
          {miNombre} {miApellido} / {seleccion.companero!.nombre} {seleccion.companero!.apellido}
          {' vs '}
          {seleccion.rival1!.nombre} {seleccion.rival1!.apellido} / {seleccion.rival2!.nombre} {seleccion.rival2!.apellido}
        </p>
      )}

      <button className={s.btnEnviarDesafio} disabled={!listo || enviando} onClick={() => void enviar()}>
        {enviando ? 'Enviando…' : 'Enviar desafío'}
      </button>

      {buscando && (
        <SeleccionarJugadorModal
          titulo="Buscar jugadores"
          jugadores={jugadores}
          excluir={excluidos}
          onElegir={(j) => elegir(buscando, j)}
          onClose={() => setBuscando(null)}
        />
      )}
    </div>
  );
}
