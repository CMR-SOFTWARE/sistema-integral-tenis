import { Link } from 'react-router-dom';
import s from './PortalPages.module.css';

/** Estado vacío del portal para el jugador que todavía no está en ningún club. */
export default function SinClub({ mensaje }: { mensaje?: string }) {
  return (
    <div className={s.sinClub}>
      <div className={s.sinClubIcono}>🎾</div>
      <h3 className={s.sinClubTitulo}>Todavía no estás en ningún club</h3>
      <p className={s.sinClubTexto}>
        {mensaje ?? 'Cuando te vincules con tu profesor, acá vas a ver tus clases, tu cuota y tu ficha.'}
      </p>
      <Link to="/portal/club" className={s.btnPrimario}>Buscar mi club</Link>
    </div>
  );
}
