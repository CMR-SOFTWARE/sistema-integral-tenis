import Avatar from '../../components/Avatar';
import ScoreBox from '../../components/motion/ScoreBox';
import s from './RankingPage.module.css';
import p from '../portal/PortalPages.module.css';

interface Props {
  nombre: string;
  apellido: string;
  rango: string;
  cf: number;
  posicion: number | null;
  puntos: number;
  mejorPuesto: number | null;
  /** Versión grande: la usa la pantalla "Mi perfil". La compacta va en la lista. */
  grande?: boolean;
}

/** Tarjeta "mi ranking": avatar + rango/CF + grilla de posición, puntos y
 *  mejor puesto histórico. La misma para singles y dobles — recibe los datos
 *  por props en vez de acoplarse a un tipo de perfil en particular. */
export default function MiTarjetaRanking({ nombre, apellido, rango, cf, posicion, puntos, mejorPuesto, grande }: Props) {
  const perfilClase = grande ? `${s.tarjetaPerfil} ${s.tarjetaPerfilGrande} motion-perfil` : s.tarjetaPerfil;
  return (
    <div className={p.tarjeta}>
      <div className={perfilClase}>
        <div className={s.tarjetaPerfilCabecera}>
          <Avatar nombre={nombre} apellido={apellido} size={grande ? 56 : 44} />
          <div>
            <div className={s.tarjetaPerfilNombre}>{nombre} {apellido}</div>
            <div className={s.tarjetaPerfilSub}>Rango {rango} · CF {cf}</div>
          </div>
        </div>
        <div className={s.statsGrid}>
          <div>
            <div className={s.statValor}>{posicion ? `#${posicion}` : '—'}</div>
            <div className={s.statLabel}>Posición</div>
          </div>
          <div>
            <div className={s.statValor}><ScoreBox valor={puntos} nivel="game" /></div>
            <div className={s.statLabel}>Puntos</div>
          </div>
          <div>
            <div className={s.statValor}>{mejorPuesto ? `#${mejorPuesto}` : '—'}</div>
            <div className={s.statLabel}>Mejor puesto</div>
          </div>
        </div>
      </div>
    </div>
  );
}
