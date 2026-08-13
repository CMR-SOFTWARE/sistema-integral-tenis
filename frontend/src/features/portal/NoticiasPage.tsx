import { obtenerSesion } from '../auth/sesion';
import SinClub from './SinClub';
import { fechaCorta } from '../agenda/types';
import { useNoticias } from './hooks';
import s from './PortalPages.module.css';

/**
 * Noticias del club: todo lo que publicó el profe y sigue vigente. Las importantes
 * también aparecen en el Inicio; acá están todas, que es lo que el alumno mira cuando
 * quiere ponerse al día.
 */
export default function NoticiasPage() {
  const conClub = obtenerSesion()?.alumno != null;
  const query = useNoticias();

  if (!conClub) {
    return <SinClub mensaje="Cuando estés en un club, acá vas a ver sus noticias y avisos." />;
  }
  if (query.error) {
    return <div className={s.error}>{query.error.message || 'Error cargando las noticias'}</div>;
  }
  if (query.isLoading) return <div className={s.vacio}>Cargando…</div>;

  const noticias = query.data ?? [];

  return (
    <div>
      {noticias.length === 0 && (
        <div className={s.tarjeta}>
          <div className={s.vacio}>Tu club todavía no publicó ninguna noticia.</div>
        </div>
      )}

      {/* Vienen ordenadas del back: las importantes primero, después por fecha. */}
      <div className={s.noticiasLista}>
        {noticias.map((n) => (
          <div key={n.id} className={n.importante ? s.noticiaDestacada : s.noticia}>
            <div className={s.noticiaCabecera}>
              <div className={s.noticiaTitulo}>{n.titulo}</div>
              {n.importante && <span className={s.noticiaChip}>Importante</span>}
            </div>
            <div className={s.noticiaMensaje}>{n.mensaje}</div>
            <div className={s.noticiaFecha}>{fechaCorta(n.creadoEl.slice(0, 10))}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
