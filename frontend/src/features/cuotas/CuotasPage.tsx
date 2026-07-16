import { useState } from 'react';
import { useCuotas } from './useCuotas';
import MedioModal from './MedioModal';
import NuevoCargoModal from './NuevoCargoModal';
import { ESTADO_LIQ_UI, MESES } from './types';
import type { EstadoLiquidacion } from './types';
import type { Liquidacion, Medio } from './types';
import { avatarColor, formatoPlata, iniciales } from '../alumnos/types';
import s from './CuotasPage.module.css';

/** Cuotas del mes: la cuenta corriente por alumno, generada desde los turnos. */
export default function CuotasPage() {
  const hoy = new Date();
  const [anio, setAnio] = useState(hoy.getFullYear());
  const [mes, setMes] = useState(hoy.getMonth() + 1);
  const { datos, cargando, error, pagarMes, pagarCargo, agregarCargo } = useCuotas(anio, mes);

  const [filtro, setFiltro] = useState<'todas' | EstadoLiquidacion>('todas');
  const [abiertos, setAbiertos] = useState<Set<string>>(new Set());
  const [pagandoMes, setPagandoMes] = useState<Liquidacion | null>(null);
  const [pagandoCargo, setPagandoCargo] = useState<{ id: string; concepto: string } | null>(null);
  const [cargoPara, setCargoPara] = useState<Liquidacion | null>(null);

  const mesAnterior = () => {
    if (mes === 1) { setMes(12); setAnio(anio - 1); } else setMes(mes - 1);
  };
  const mesSiguiente = () => {
    if (mes === 12) { setMes(1); setAnio(anio + 1); } else setMes(mes + 1);
  };

  const toggleDetalle = (alumnoId: string) => {
    const nuevo = new Set(abiertos);
    if (nuevo.has(alumnoId)) nuevo.delete(alumnoId);
    else nuevo.add(alumnoId);
    setAbiertos(nuevo);
  };

  const sinPrecios = error?.toLowerCase().includes('configur');

  const liquidaciones = (datos?.liquidaciones ?? [])
    .filter((l) => filtro === 'todas' || l.estado === filtro);

  return (
    <div>
      <div className={s.toolbar}>
        <button className={s.nav} onClick={mesAnterior}>‹</button>
        <div className={s.rango}>{MESES[mes - 1]} {anio}</div>
        <button className={s.nav} onClick={mesSiguiente}>›</button>

        <div className={s.spacer} />

        {/* Filtro por estado de la liquidación (client-side: ya está cargada) */}
        <div className={s.filtros}>
          {(['todas', 'Pagada', 'Pendiente', 'Vencida'] as const).map((f) => (
            <button
              key={f}
              className={filtro === f ? s.filtroActivo : s.filtro}
              onClick={() => setFiltro(f)}
            >
              {f === 'todas' ? 'Todas' : f + 's'}
            </button>
          ))}
        </div>
      </div>

      {error && (
        <div className={s.error}>
          {error}
          {sinPrecios && <> — cargalos en <b>Configuración</b> (sidebar).</>}
        </div>
      )}
      {cargando && <div className={s.vacio}>Calculando el mes…</div>}

      {datos && !cargando && (
        <>
          {/* ── Stats del mes (datos reales) ── */}
          <div className={s.stats}>
            <div className={s.stat}>
              <div className={s.statValor}>{formatoPlata(datos.totalFacturado)}</div>
              <div className={s.statLabel}>Facturado</div>
            </div>
            <div className={s.stat}>
              <div className={s.statValor} style={{ color: '#0e6b3c' }}>{formatoPlata(datos.totalCobrado)}</div>
              <div className={s.statLabel}>Cobrado</div>
            </div>
            <div className={s.stat}>
              <div className={s.statValor} style={{ color: '#b7791f' }}>{formatoPlata(datos.totalPendiente)}</div>
              <div className={s.statLabel}>Pendiente</div>
            </div>
            <div className={s.stat}>
              <div className={s.statValor} style={{ color: datos.alumnosVencidos > 0 ? '#b91c1c' : undefined }}>
                {datos.alumnosVencidos}
              </div>
              <div className={s.statLabel}>Vencidos</div>
            </div>
          </div>

          {datos.liquidaciones.length === 0 && (
            <div className={s.vacioCard}>
              Sin movimientos este mes. Los cargos nacen de los turnos del <b>Calendario</b>
              {' '}(y de productos/ajustes que agregues acá).
            </div>
          )}

          {datos.liquidaciones.length > 0 && liquidaciones.length === 0 && (
            <div className={s.vacioCard}>
              Ningún alumno tiene la cuota <b>{filtro.toLowerCase()}</b> este mes.
            </div>
          )}

          {/* ── Liquidaciones por alumno ── */}
          <div className={s.lista}>
            {liquidaciones.map((l) => {
              const av = avatarColor(l.nombre + l.apellido);
              const estado = ESTADO_LIQ_UI[l.estado];
              const abierto = abiertos.has(l.alumnoId);
              return (
                <div key={l.alumnoId} className={s.tarjeta}>
                  <button className={s.fila} onClick={() => toggleDetalle(l.alumnoId)}>
                    <div className={s.avatar} style={{ background: `${av}1a`, color: av }}>
                      {iniciales(l.nombre, l.apellido)}
                    </div>
                    <div className={s.filaNombre}>
                      <span className={s.nombre}>{l.nombre} {l.apellido}</span>
                      <span className={s.modalidad}>{l.modalidad === 'PorClase' ? 'paga por clase' : 'mensual'}</span>
                    </div>
                    <div className={s.montos}>
                      <span className={s.total}>{formatoPlata(l.total)}</span>
                      {l.saldo > 0 && l.pagado > 0 && (
                        <span className={s.saldo}>debe {formatoPlata(l.saldo)}</span>
                      )}
                    </div>
                    <span className={s.chip} style={{ background: estado.bg, color: estado.fg }}>
                      {l.estado}
                    </span>
                    <span className={`${s.flecha} ${abierto ? s.flechaAbierta : ''}`}>›</span>
                  </button>

                  {abierto && (
                    <div className={s.detalle}>
                      {l.cargos.map((c) => (
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
                            <span className={s.cargoPagado}>✓ {c.medioPago}</span>
                          ) : (
                            <button
                              className={s.btnPagarCargo}
                              onClick={() => setPagandoCargo({ id: c.id, concepto: c.concepto })}
                            >
                              Pagar
                            </button>
                          )}
                        </div>
                      ))}
                      <div className={s.detalleAcciones}>
                        <button className={s.btnSecundario} onClick={() => setCargoPara(l)}>
                          + Agregar cargo
                        </button>
                        {l.saldo > 0 && (
                          <button className={s.btnPagarMes} onClick={() => setPagandoMes(l)}>
                            Confirmar pago del mes ({formatoPlata(l.saldo)})
                          </button>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </>
      )}

      {pagandoMes && (
        <MedioModal
          titulo="Confirmar pago del mes"
          subtitulo={`${pagandoMes.nombre} ${pagandoMes.apellido} — salda ${formatoPlata(pagandoMes.saldo)} (${MESES[mes - 1]})`}
          onClose={() => setPagandoMes(null)}
          onConfirmar={(medio: Medio) => pagarMes(pagandoMes.alumnoId, medio)}
        />
      )}
      {pagandoCargo && (
        <MedioModal
          titulo="Pagar cargo"
          subtitulo={pagandoCargo.concepto}
          onClose={() => setPagandoCargo(null)}
          onConfirmar={(medio: Medio) => pagarCargo(pagandoCargo.id, medio)}
        />
      )}
      {cargoPara && (
        <NuevoCargoModal
          alumno={cargoPara}
          onClose={() => setCargoPara(null)}
          onCrear={agregarCargo}
        />
      )}
    </div>
  );
}
