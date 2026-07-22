import { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';
import { obtenerSesion } from '../auth/sesion';
import SinClub from './SinClub';
import InformarPagoModal from './InformarPagoModal';
import { formatoPlata } from '../alumnos/types';
import { ESTADO_LIQ_UI, MESES } from '../cuotas/types';
import { useMiCuota } from './hooks';
import s from './PortalPages.module.css';

/** El objeto de pago a informar: el mes entero o un cargo puntual. */
type AInformar =
  | { tipo: 'mes'; monto: number }
  | { tipo: 'cargo'; cargoId: string; concepto: string; monto: number };

/** Mi cuota: resumen del mes + detalle de cargos + "avisar que ya transferí". */
export default function MiCuotaPage() {
  const hoy = new Date();
  const conClub = obtenerSesion()?.alumno != null;
  const qc = useQueryClient();
  const [anio, setAnio] = useState(hoy.getFullYear());
  const [mes, setMes] = useState(hoy.getMonth() + 1);
  const cuotaQuery = useMiCuota(anio, mes);
  const cuota = cuotaQuery.data ?? null;
  const cargando = cuotaQuery.isLoading;
  const error = cuotaQuery.error ? (cuotaQuery.error.message || 'Error cargando tu cuota') : null;
  const [informar, setInformar] = useState<AInformar | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);

  if (!conClub) return <SinClub mensaje="Cuando estés en un club, acá vas a ver tu cuota mensual y tus pagos." />;

  const mesAnterior = () => {
    if (mes === 1) { setMes(12); setAnio(anio - 1); } else setMes(mes - 1);
  };
  const mesSiguiente = () => {
    if (mes === 12) { setMes(1); setAnio(anio + 1); } else setMes(mes + 1);
  };

  const confirmarInforme = async () => {
    if (!informar) return;
    if (informar.tipo === 'mes') {
      await api.post(`/portal/mi-cuota/${anio}/${mes}/informar`, {});
    } else {
      await api.post(`/portal/cargos/${informar.cargoId}/informar`, {});
    }
    setAviso('¡Aviso enviado! Tu profe va a confirmar el pago.');
    setTimeout(() => setAviso(null), 3500);
    await qc.invalidateQueries({ queryKey: ['portal-mi-cuota'] });
  };

  const estado = cuota ? ESTADO_LIQ_UI[cuota.estado] : null;
  const puedeInformarMes = (cuota?.saldo ?? 0) > 0 && cuota?.estado !== 'Informado';

  return (
    <div>
      <div className={s.toolbar}>
        <button className={s.nav} onClick={mesAnterior}>‹</button>
        <div className={s.rango}>{MESES[mes - 1]} {anio}</div>
        <button className={s.nav} onClick={mesSiguiente}>›</button>
      </div>

      {error && <div className={s.error}>{error}</div>}
      {aviso && <div className={s.avisoOk}>{aviso}</div>}
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
                  {cuota.estado === 'Pagada' ? 'Pagado'
                    : cuota.estado === 'Informado' ? 'Esperando confirmación'
                    : cuota.estado}
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
              {puedeInformarMes && (
                <button
                  className={s.btnInformar}
                  onClick={() => setInformar({ tipo: 'mes', monto: cuota!.saldo })}
                >
                  Ya transferí
                </button>
              )}
              {cuota?.estado === 'Informado' && (
                <div className={s.cuotaVence}>Avisaste que transferiste. Esperando a tu profe.</div>
              )}
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
                {c.pagado ? (
                  <span className={`${s.chip} ${s.chipVerde}`}>✓ Pagado{c.medioPago ? ` · ${c.medioPago}` : ''}</span>
                ) : c.pagoInformado ? (
                  <span className={`${s.chip} ${s.chipAzul}`}>Avisado ✓</span>
                ) : (
                  <button
                    className={s.btnAvisarCargo}
                    onClick={() => setInformar({ tipo: 'cargo', cargoId: c.id, concepto: c.concepto, monto: c.monto })}
                  >
                    Avisar pago
                  </button>
                )}
              </div>
            ))}
          </div>
          <p className={s.nota}>
            Transferí y tocá <b>“Ya transferí”</b>: tu profe confirma que le llegó y tu cuota queda al día.
          </p>
        </>
      )}

      {informar && (
        <InformarPagoModal
          titulo={informar.tipo === 'mes' ? 'Informar pago del mes' : `Informar pago · ${informar.concepto}`}
          monto={informar.monto}
          onConfirmar={confirmarInforme}
          onClose={() => setInformar(null)}
        />
      )}
    </div>
  );
}
