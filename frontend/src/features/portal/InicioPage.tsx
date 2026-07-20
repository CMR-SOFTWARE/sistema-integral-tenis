import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../../lib/api';
import { obtenerSesion } from '../auth/sesion';
import SinClub from './SinClub';
import { formatoPlata } from '../alumnos/types';
import { ESTADO_LIQ_UI, MESES } from '../cuotas/types';
import { fechaCorta, horaCorta, DIAS } from '../agenda/types';
import type { MiLiquidacion, MisTurnos, MiTurno, Publicidad } from './types';
import s from './PortalPages.module.css';

/** "2026-07-14" → "MAR" (para la columna de horarios asignados). */
function diaCorto(iso: string): string {
  const d = new Date(`${iso}T00:00:00`);
  const idx = (d.getDay() + 6) % 7; // lunes = 0
  return DIAS[idx].corto.toUpperCase();
}

/**
 * Inicio del portal (mockup): hero con la próxima clase, estado de la cuota,
 * horarios asignados y avisos del profe (placeholder hasta tener backend).
 */
export default function InicioPage() {
  const hoy = new Date();
  const conClub = obtenerSesion()?.alumno != null;
  const [turnos, setTurnos] = useState<MisTurnos | null>(null);
  const [cuota, setCuota] = useState<MiLiquidacion | null | undefined>(undefined); // undefined = cargando
  const [error, setError] = useState<string | null>(null);
  const [banners, setBanners] = useState<Publicidad[]>([]);
  const [bannerIdx, setBannerIdx] = useState(0);

  useEffect(() => {
    if (!conClub) return; // sin ficha no hay nada que pedir
    api.get<MisTurnos>('/portal/mis-turnos')
      .then(setTurnos)
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando tus clases'));
    api.get<MiLiquidacion | undefined>(`/portal/mi-cuota/${hoy.getFullYear()}/${hoy.getMonth() + 1}`)
      .then((c) => setCuota(c ?? null))
      .catch(() => setCuota(null));
    api.get<Publicidad[]>('/portal/publicidad').then(setBanners).catch(() => setBanners([]));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [conClub]);

  // Rotación de banners (si hay más de uno) cada 6s
  useEffect(() => {
    if (banners.length < 2) return;
    const t = setInterval(() => setBannerIdx((i) => (i + 1) % banners.length), 6000);
    return () => clearInterval(t);
  }, [banners.length]);

  if (!conClub) return <SinClub />;
  if (error) return <div className={s.error}>{error}</div>;
  if (!turnos || cuota === undefined) return <div className={s.vacio}>Cargando…</div>;

  const proxima: MiTurno | undefined = turnos.proximos.find((t) => t.estado !== 'Cancelado');
  const estadoCuota = cuota ? ESTADO_LIQ_UI[cuota.estado] : null;

  return (
    <div className={s.inicioGrilla}>
      {/* ── Hero: tu próxima clase ── */}
      <div className={s.hero}>
        <div className={s.heroPelota} />
        <div className={s.heroEyebrow}>Tu próxima clase</div>
        {proxima ? (
          <>
            <div className={s.heroFecha}>{fechaCorta(proxima.fecha)}</div>
            <div className={s.heroDetalle}>
              {horaCorta(proxima.horaInicio)} hs · {proxima.titulo}
            </div>
            {proxima.companeros.length > 0 && (
              <div className={s.heroCompaneros}>
                Con {proxima.companeros.map((c) => c.split(' ')[0]).join(', ')}
              </div>
            )}
          </>
        ) : (
          <div className={s.heroDetalle}>No tenés clases programadas por ahora.</div>
        )}
      </div>

      {/* ── Estado de la cuota ── */}
      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Estado de tu cuota</h3>
        <div className={s.cuotaMonto}>{formatoPlata(cuota ? cuota.saldo : 0)}</div>
        {estadoCuota ? (
          <span className={s.chip} style={{ background: estadoCuota.bg, color: estadoCuota.fg }}>
            {cuota!.estado === 'Pagada' ? 'Pagado' : cuota!.estado}
          </span>
        ) : (
          <span className={`${s.chip} ${s.chipVerde}`}>Sin movimientos</span>
        )}
        <div className={s.cuotaVence}>Vence el 10 de {MESES[hoy.getMonth()].toLowerCase()}</div>
        <Link to="/portal/cuota" className={s.btnPrimario}>Ver mi cuota</Link>
      </div>

      {/* ── Tus horarios asignados ── */}
      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Tus horarios asignados</h3>
        {turnos.proximos.length === 0 && <div className={s.vacio}>Sin clases próximas.</div>}
        <div className={s.horariosLista}>
          {turnos.proximos.slice(0, 4).map((t) => (
            <div key={t.id} className={s.horarioFila}>
              <div className={s.horarioDia}>
                <div className={s.horarioDiaNombre}>{diaCorto(t.fecha)}</div>
                <div className={s.horarioHora}>{horaCorta(t.horaInicio)}</div>
              </div>
              <div className={s.horarioInfo}>
                <div className={s.horarioTitulo}>{t.titulo}</div>
                <div className={s.horarioDetalle}>
                  {fechaCorta(t.fecha)}
                  {t.companeros.length > 0 && <> · {t.companeros.map((c) => c.split(' ')[0]).join(', ')}</>}
                </div>
              </div>
              {t.estado === 'Cancelado'
                ? <span className={`${s.chip} ${s.chipRojo}`}>Cancelada</span>
                : <span className={`${s.chip} ${s.chipVerde}`}>Confirmada</span>}
            </div>
          ))}
        </div>
      </div>

      {/* ── Avisos del profe (backend en una próxima versión) ── */}
      <div className={s.tarjeta}>
        <h3 className={s.tarjetaTitulo}>Avisos del profe</h3>
        <div className={s.vacio}>
          Los avisos del profe llegan en una próxima versión.
        </div>
      </div>

      {/* ── Publicidad (M6): carrusel de banners del club (desliza al costado) ── */}
      {banners.length > 0 && (
        <div className={s.bannerCard}>
          <span className={s.bannerLabel}>Publicidad</span>
          <div
            className={s.bannerTrack}
            style={{ transform: `translateX(-${(bannerIdx % banners.length) * 100}%)` }}
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
      )}
    </div>
  );
}
