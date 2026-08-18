import { useEffect, useState } from 'react';
import { usePublicidad } from './hooks';
import s from './PortalPages.module.css';

/**
 * Carrusel de banners del club (M6). Vive en el LAYOUT del portal, así aparece en todas
 * las secciones y no solo en el Inicio, que es donde estaba hasta el 15/08/2026.
 *
 * No cuesta consultas de más: `usePublicidad` es React Query con una key fija, así que
 * navegar entre secciones reusa lo cacheado. Y su `enabled` resuelve el caso del que
 * todavía no está en ningún club: los banners son POR club, así que no hay nada que
 * mostrarle y no se pide nada.
 */
export default function Publicidad() {
  const { data: banners = [] } = usePublicidad();
  const [idx, setIdx] = useState(0);

  // Rotación (si hay más de uno) cada 6s
  useEffect(() => {
    if (banners.length < 2) return;
    const t = setInterval(() => setIdx((i) => (i + 1) % banners.length), 6000);
    return () => clearInterval(t);
  }, [banners.length]);

  if (banners.length === 0) return null;

  return (
    <div className={s.bannerCard}>
      <span className={s.bannerLabel}>Publicidad</span>
      <div
        className={s.bannerTrack}
        style={{ transform: `translateX(-${(idx % banners.length) * 100}%)` }}
      >
        {banners.map((b) => {
          const img = <img src={b.imagenUrl} alt={b.nombre} className={s.bannerImg} />;
          return (
            <div key={b.id} className={s.bannerSlide}>
              {/* fondo: la misma imagen borrosa rellena los costados */}
              <div className={s.bannerBg} style={{ backgroundImage: `url("${b.imagenUrl}")` }} />
              {b.enlace
                ? <a href={b.enlace} target="_blank" rel="noreferrer noopener" className={s.bannerLink}>{img}</a>
                : img}
            </div>
          );
        })}
      </div>
    </div>
  );
}
