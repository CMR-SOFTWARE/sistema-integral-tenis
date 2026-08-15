import { obtenerSesion } from '../auth/sesion';
import MiTarjetaRanking from './MiTarjetaRanking';
import type { MiPerfilRanking } from './types';
import s from './RankingPage.module.css';

interface Props {
  perfil: MiPerfilRanking;
  onVolver: () => void;
}

/** Pantalla "Mi perfil" del ranking: mis stats. El historial de partidos
 *  vive en "Mis juegos" (mismo lugar donde se cargan los resultados). */
export default function MiPerfilRankingScreen({ perfil, onVolver }: Props) {
  const sesion = obtenerSesion();

  return (
    <div>
      <div className={s.volverHeader}>
        <button className={s.volverLink} onClick={onVolver}>← Ranking</button>
      </div>
      <h2 className={s.pantallaTitulo}>Mi perfil</h2>

      <MiTarjetaRanking
        nombre={sesion?.nombre ?? ''}
        apellido={sesion?.apellido ?? ''}
        rango={perfil.rango ?? '—'}
        cf={perfil.cf ?? 0}
        posicion={perfil.posicion}
        puntos={perfil.puntos}
        mejorPuesto={perfil.mejorPuestoHistorico}
        grande
      />
    </div>
  );
}
