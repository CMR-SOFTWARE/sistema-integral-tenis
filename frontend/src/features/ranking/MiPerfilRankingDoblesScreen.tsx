import { obtenerSesion } from '../auth/sesion';
import MiTarjetaRanking from './MiTarjetaRanking';
import type { MiPerfilDobles } from './types';
import s from './RankingPage.module.css';

interface Props {
  perfil: MiPerfilDobles;
  onVolver: () => void;
}

/** Igual que MiPerfilRankingScreen pero de dobles. El historial vive en
 *  "Mis juegos de dobles", no acá. */
export default function MiPerfilRankingDoblesScreen({ perfil, onVolver }: Props) {
  const sesion = obtenerSesion();

  return (
    <div>
      <div className={s.volverHeader}>
        <button className={s.volverLink} onClick={onVolver}>← Ranking dobles</button>
      </div>
      <h2 className={s.pantallaTitulo}>Mi perfil de dobles</h2>

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
