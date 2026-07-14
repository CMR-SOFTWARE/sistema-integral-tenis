import { useEffect, useState } from 'react';
import { api } from '../../lib/api';
import { obtenerSesion } from '../auth/sesion';
import SinClub from './SinClub';
import { formatoPlata } from '../alumnos/types';
import { ESTADO_LIQ_UI, MESES } from '../cuotas/types';
import type { MiLiquidacion } from './types';
import s from './PortalPages.module.css';

/** Mi cuota (mockup): tres cards de resumen + el detalle de cargos del mes. */
export default function MiCuotaPage() {
  const hoy = new Date();
  const conClub = obtenerSesion()?.alumno != null;
  const [anio, setAnio] = useState(hoy.getFullYear());
  const [mes, setMes] = useState(hoy.getMonth() + 1);
  const [cuota, setCuota] = useState<MiLiquidacion | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!conClub) return;
    setCargando(true);
    setError(null);
    api.get<MiLiquidacion | undefined>(`/portal/mi-cuota/${anio}/${mes}`)
      .then((c) => setCuota(c ?? null)) // 204 = sin movimientos
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando tu cuota'))
      .finally(() => setCargando(false));
  }, [anio, mes, conClub]);

  if (!conClub) return <SinClub mensaje="Cuando estés en un club, acá vas a ver tu cuota mensual y tus pagos." />;

  const mesAnterior = () => {
    if (mes === 1) { setMes(12); setAnio(anio - 1); } else setMes(mes - 1);
  };
  const mesSiguiente = () => {
    if (mes === 12) { setMes(1); setAnio(anio + 1); } else setMes(mes + 1);
  };

  const estado = cuota ? ESTADO_LIQ_UI[cuota.estado] : null;

  return (
    <div>
      <div className={s.toolbar}>
        <button className={s.nav} onClick={mesAnterior}>‹</button>
        <div className={s.rango}>{MESES[mes - 1]} {anio}</div>
        <button className={s.nav} onClick={mesSiguiente}>›</button>
      </div>

      {error && <div className={s.error}>{error}</div>}
      {cargando && !error && <div className={s.vacio}>Calculando…</div>}

      {!cargando && !error && (
        <>
          {/* ── Las 3 cards del mockup ── */}
          <div className={s.statsCuota}>
            <div className={s.tarjeta}>
              <h3 className={s.tarjetaTitulo}>Cuota del mes</h3>
              <div className={s.cuotaMonto}>{formatoPlata(cuota?.total ?? 0)}</div>
            </div>
            <div className={s.tarjeta}>
              <h3 className={s.tarjetaTitulo}>Estado actual</h3>
              {cuota && estado ? (
                <span className={s.chip} style={{ background: estado.bg, color: estado.fg }}>
                  {cuota.estado === 'Pagada' ? 'Pagado' : cuota.estado}
                </span>
              ) : (
                <span className={`${s.chip} ${s.chipVerde}`}>Sin movimientos</span>
              )}
              <div className={s.cuotaVence}>Vence el 10 de {MESES[mes - 1].toLowerCase()}</div>
            </div>
            <div className={s.tarjeta}>
              <h3 className={s.tarjetaTitulo}>Adeudado</h3>
              <div className={s.cuotaMonto} style={{ color: (cuota?.saldo ?? 0) > 0 ? '#b91c1c' : '#0e6b3c' }}>
                {formatoPlata(cuota?.saldo ?? 0)}
              </div>
            </div>
          </div>

          {/* ── Detalle del mes ── */}
          <h2 className={s.seccion}>Detalle del mes</h2>
          <div className={s.tarjeta}>
            {!cuota && <div className={s.vacio}>Sin movimientos este mes.</div>}
            {cuota?.cargos.map((c) => (
              <div key={c.id} className={s.cargo}>
                <span className={s.cargoFecha}>
                  {new Date(`${c.fecha}T00:00:00`).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' })}
                </span>
                <span className={s.cargoConcepto}>
                  {c.concepto}
                  {c.tipo !== 'Clase' && <span className={s.cargoTipo}> · {c.tipo}</span>}
                </span>
                <span className={s.cargoMonto}>{formatoPlata(c.monto)}</span>
                {c.pagado
                  ? <span className={`${s.chip} ${s.chipVerde}`}>✓ Pagado{c.medioPago ? ` · ${c.medioPago}` : ''}</span>
                  : <span className={`${s.chip} ${s.chipAmbar}`}>Pendiente</span>}
              </div>
            ))}
          </div>
          <p className={s.nota}>Los pagos se registran con tu profesor.</p>
        </>
      )}
    </div>
  );
}
