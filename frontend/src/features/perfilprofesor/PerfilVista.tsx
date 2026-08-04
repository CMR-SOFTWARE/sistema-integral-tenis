import { useEffect, useRef, useState, type ReactNode } from 'react';
import Avatar from '../../components/Avatar';
import type { FotoPerfil, HitoTrayectoria } from './types';
import s from './PerfilVista.module.css';

// Tienen que coincidir con grid-auto-rows y gap de .galeria
const ALTO_FILA = 8;
const SEPARACION = 13;

/**
 * Una celda del mosaico: mide el alto real de su contenido y se reserva las filas
 * de la grilla que necesita. Así cada foto se muestra entera, con su proporción,
 * sin recortes y sin huecos entre una y otra.
 */
function CeldaMosaico({ children }: { children: ReactNode }) {
  const contenido = useRef<HTMLDivElement>(null);
  const [filas, setFilas] = useState<number | undefined>();

  useEffect(() => {
    const el = contenido.current;
    if (!el) return;

    // Se observa el CONTENIDO, no la celda: el alto de la celda es justamente
    // lo que estamos calculando, y observarla se realimentaría a sí misma.
    const observador = new ResizeObserver(() => {
      const alto = el.getBoundingClientRect().height;
      if (alto > 0) setFilas(Math.ceil((alto + SEPARACION) / (ALTO_FILA + SEPARACION)));
    });
    observador.observe(el);
    return () => observador.disconnect();
  }, []);

  // Hasta que la imagen cargue se reserva un alto provisorio (~200 px): sin esto,
  // las fotos que todavía no bajaron miden cero y se amontonan arriba de todo.
  return (
    <div className={s.celdaMosaico} style={{ gridRowEnd: `span ${filas ?? 10}` }}>
      <div ref={contenido}>{children}</div>
    </div>
  );
}

export interface DatosPerfil {
  nombre: string;
  apellido: string;
  club: string;
  titular: string | null;
  subtitulo: string | null;
  bio: string | null;
  especialidades: string[];
  portadaUrl: string | null;
  avatarUrl: string | null;
  fotos: FotoPerfil[];
  hitos: HitoTrayectoria[];
}

interface Props {
  perfil: DatosPerfil;
  /** Texto para cuando el perfil está vacío (cambia si el que mira es su dueño). */
  mensajeVacio?: string;
}

/**
 * La carta de presentación, en modo lectura. Es UN componente para los dos usos:
 * lo que ve el alumno en Mi club y la vista previa del propio profe mientras la
 * arma — así lo que edita es exactamente lo que se va a ver.
 */
export default function PerfilVista({ perfil, mensajeVacio }: Props) {
  const [ampliada, setAmpliada] = useState<FotoPerfil | null>(null);
  const sinContenido = !perfil.bio && perfil.hitos.length === 0 && perfil.fotos.length === 0;

  // Cerrar el visor con Escape (y no dejar el fondo scrolleando detrás)
  useEffect(() => {
    if (!ampliada) return;
    const alTeclear = (e: KeyboardEvent) => { if (e.key === 'Escape') setAmpliada(null); };
    window.addEventListener('keydown', alTeclear);
    const overflowPrevio = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      window.removeEventListener('keydown', alTeclear);
      document.body.style.overflow = overflowPrevio;
    };
  }, [ampliada]);

  return (
    <div className={s.pagina}>
      <div className={s.hero}>
        <div className={`${s.portada} ${perfil.portadaUrl ? '' : s.portadaVacia}`}>
          {perfil.portadaUrl && <img src={perfil.portadaUrl} alt="" />}
        </div>

        <div className={s.heroBody}>
          <div className={s.avatarWrap}>
            <Avatar
              nombre={perfil.nombre}
              apellido={perfil.apellido}
              fotoUrl={perfil.avatarUrl}
              size={104}
              radius={999}
            />
          </div>

          <h2 className={s.nombre}>{perfil.nombre} {perfil.apellido}</h2>
          {perfil.titular && <div className={s.titular}>{perfil.titular}</div>}
          <div className={s.club}>
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M3 21h18M5 21V7l7-4 7 4v14M9 21v-6h6v6" />
            </svg>
            {perfil.club}
          </div>

          {perfil.subtitulo && <p className={s.subtitulo}>{perfil.subtitulo}</p>}

          {perfil.especialidades.length > 0 && (
            <div className={s.chips}>
              {perfil.especialidades.map((e) => <span key={e} className={s.chip}>{e}</span>)}
            </div>
          )}
        </div>
      </div>

      {sinContenido && mensajeVacio && (
        <div className={s.seccion}>
          <p className={s.vacio}>{mensajeVacio}</p>
        </div>
      )}

      {perfil.bio && (
        <section className={s.seccion}>
          <h3 className={s.tituloSeccion}>Quién soy</h3>
          <p className={s.bio}>{perfil.bio}</p>
        </section>
      )}

      {perfil.hitos.length > 0 && (
        <section className={s.seccion}>
          <h3 className={s.tituloSeccion}>Mi trayectoria</h3>
          <div className={s.timeline}>
            {perfil.hitos.map((h) => (
              <div key={h.id} className={s.hito}>
                <div className={s.hitoAnio}>{h.anio}</div>
                <div className={s.hitoTitulo}>{h.titulo}</div>
                {h.detalle && <div className={s.hitoDetalle}>{h.detalle}</div>}
              </div>
            ))}
          </div>
        </section>
      )}

      {perfil.fotos.length > 0 && (
        <section className={s.seccion}>
          <h3 className={s.tituloSeccion}>Galería</h3>
          <div className={s.galeria}>
            {perfil.fotos.map((f) => (
              <CeldaMosaico key={f.id}>
                <button
                  className={s.foto}
                  onClick={() => setAmpliada(f)}
                  aria-label={f.pieDeFoto ?? 'Ampliar foto'}
                >
                  <img src={f.url} alt={f.pieDeFoto ?? ''} loading="lazy" />
                  {f.pieDeFoto && <span className={s.pie}>{f.pieDeFoto}</span>}
                </button>
              </CeldaMosaico>
            ))}
          </div>
        </section>
      )}

      {ampliada && (
        <div className={s.visor} onClick={() => setAmpliada(null)} role="dialog" aria-modal="true">
          <button className={s.visorCerrar} onClick={() => setAmpliada(null)} aria-label="Cerrar">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
              <path d="M18 6L6 18M6 6l12 12" />
            </svg>
          </button>
          <img src={ampliada.url} alt={ampliada.pieDeFoto ?? ''} onClick={(e) => e.stopPropagation()} />
          {ampliada.pieDeFoto && <p className={s.visorPie}>{ampliada.pieDeFoto}</p>}
        </div>
      )}
    </div>
  );
}

/** Mientras carga: el mismo esqueleto de la tarjeta, para que no salte la pantalla. */
export function PerfilVistaCargando() {
  return (
    <div className={s.pagina}>
      <div className={s.skeleton}>
        <div className={`${s.skeletonPortada} ${s.brillo}`} />
        <div className={`${s.skeletonLinea} ${s.brillo}`} style={{ width: '45%', height: 20, marginTop: 20 }} />
        <div className={`${s.skeletonLinea} ${s.brillo}`} style={{ width: '30%' }} />
        <div style={{ height: 10 }} />
      </div>
      <div className={s.skeleton}>
        <div className={`${s.skeletonLinea} ${s.brillo}`} style={{ width: '25%', marginTop: 20 }} />
        <div className={`${s.skeletonLinea} ${s.brillo}`} style={{ width: '85%' }} />
        <div className={`${s.skeletonLinea} ${s.brillo}`} style={{ width: '70%' }} />
        <div style={{ height: 10 }} />
      </div>
    </div>
  );
}
