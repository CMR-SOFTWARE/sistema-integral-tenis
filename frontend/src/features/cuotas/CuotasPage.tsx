import { useState } from 'react';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { useCuotas } from './useCuotas';
import MedioModal from './MedioModal';
import NuevoCargoModal from './NuevoCargoModal';
import EditarMontoModal from './EditarMontoModal';
import PanelMorosos from './PanelMorosos';
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
  const { datos, cargando, error, reporte, pagarMes, pagarCargo, rechazarMes, agregarCargo, editarMonto, recargar } = useCuotas(anio, mes);

  const [filtro, setFiltro] = useState<'todas' | EstadoLiquidacion>('todas');
  const [abiertos, setAbiertos] = useState<Set<string>>(new Set());
  const [pagandoMes, setPagandoMes] = useState<Liquidacion | null>(null);
  const [pagandoCargo, setPagandoCargo] = useState<{ id: string; concepto: string } | null>(null);
  const [editandoCargo, setEditandoCargo] = useState<{ id: string; concepto: string; monto: number } | null>(null);
  const [cargoPara, setCargoPara] = useState<Liquidacion | null>(null);
  const confirmar = useConfirmar();

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

  const rechazar = async (l: Liquidacion) => {
    if (!(await confirmar({
      titulo: `Rechazar el pago de ${l.nombre} ${l.apellido}`,
      mensaje: 'Volvés su cuota a "pendiente" y el alumno podrá volver a informar.',
      confirmar: 'Rechazar pago',
      peligro: true,
    }))) return;
    await rechazarMes(l.alumnoId);
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

      {/* Los pedidos del Shop se resuelven en Mi academia → Pedidos: acá se mezclaban
          con la cobranza. Lo que aceptás allá cae en esta liquidación como un cargo. */}
      <PanelMorosos onCambio={() => void recargar()} />

      {error && (
        <div className={s.error}>
          {error}
          {sinPrecios && <> — cargalos en <b>Mi academia</b> → Configuración.</>}
        </div>
      )}
      {cargando && <div className={s.vacio}>Calculando el mes…</div>}

      {datos && !cargando && (
        <>
          {/* ── Panel financiero del mes (datos reales) ── */}
          <div className={s.stats}>
            <div className={s.stat}>
              <div className={s.statValor} style={{ color: '#0e6b3c' }}>{formatoPlata(datos.totalCobrado)}</div>
              <div className={s.statLabel}>Recaudado</div>
            </div>
            <div className={s.stat}>
              <div className={s.statValor} style={{ color: '#b7791f' }}>{formatoPlata(datos.totalPendiente)}</div>
              <div className={s.statLabel}>Por cobrar</div>
            </div>
            <div className={s.stat}>
              <div className={s.statValor}>{formatoPlata(datos.totalFacturado)}</div>
              <div className={s.statLabel}>Potencial del mes</div>
            </div>
            <div className={s.stat}>
              <div className={s.statValor} style={{ color: datos.alumnosVencidos > 0 ? '#b91c1c' : undefined }}>
                {datos.alumnosVencidos}
              </div>
              <div className={s.statLabel}>Vencidos</div>
            </div>
          </div>

          {/* ── Balance: recaudado por mes (últimos meses) ── */}
          {reporte.length > 0 && (
            <div className={s.balance}>
              <div className={s.balanceTitulo}>Recaudado por mes</div>
              <div className={s.balanceBarras}>
                {(() => {
                  const max = Math.max(...reporte.map((r) => r.recaudado), 1);
                  return reporte.map((r) => (
                    <div key={`${r.anio}-${r.mes}`} className={s.balanceItem} title={formatoPlata(r.recaudado)}>
                      <div className={s.balanceBarraWrap}>
                        <div
                          className={s.balanceBarra}
                          style={{ height: `${Math.round((r.recaudado / max) * 100)}%` }}
                        />
                      </div>
                      <div className={s.balanceMes}>{MESES[r.mes - 1].slice(0, 3)}</div>
                    </div>
                  ));
                })()}
              </div>
            </div>
          )}

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
              // Familia: comparte cuenta (familiaId) con otra liquidación del mes
              const enFamilia = !!l.familiaId && liquidaciones.filter((o) => o.familiaId === l.familiaId).length > 1;
              return (
                <div key={l.alumnoId} className={s.tarjeta}>
                  <button className={s.fila} onClick={() => toggleDetalle(l.alumnoId)}>
                    <div className={s.avatar} style={{ background: `${av}1a`, color: av }}>
                      {iniciales(l.nombre, l.apellido)}
                    </div>
                    <div className={s.filaNombre}>
                      <span className={s.nombre}>
                        {l.nombre} {l.apellido}
                        {enFamilia && <span title="Comparte cuenta familiar"> 👪</span>}
                      </span>
                      <span className={s.modalidad}>{l.modalidad === 'PorClase' ? 'paga por clase' : 'mensual'}</span>
                    </div>
                    <div className={s.montos}>
                      <span className={s.total}>{formatoPlata(l.total)}</span>
                      {l.saldo > 0 && l.pagado > 0 && (
                        <span className={s.saldo}>debe {formatoPlata(l.saldo)}</span>
                      )}
                    </div>
                    {l.cuotaDefinida ? (
                      <span className={s.chip} style={{ background: estado.bg, color: estado.fg }}>
                        {l.estado}
                      </span>
                    ) : (
                      <span className={s.chip} style={{ background: '#f3f4f6', color: '#6b7280' }} title="Cargale la cuota mensual en la ficha del alumno">
                        Sin cuota
                      </span>
                    )}
                    <span className={`${s.flecha} ${abierto ? s.flechaAbierta : ''}`}>›</span>
                  </button>

                  {abierto && (
                    <div className={s.detalle}>
                      {l.estado === 'Informado' && (
                        <div className={s.avisoInformado}>
                          <span>
                            <b>{l.nombre}</b> informó que transfirió {formatoPlata(l.saldo)}. Confirmá si te llegó.
                          </span>
                          <div className={s.avisoAcciones}>
                            <button className={s.btnRechazar} onClick={() => void rechazar(l)}>
                              No me llegó
                            </button>
                            <button className={s.btnPagarMes} onClick={() => setPagandoMes(l)}>
                              Confirmar pago
                            </button>
                          </div>
                        </div>
                      )}
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
                          ) : c.pagoInformado ? (
                            <span className={s.cargoInformado}>Informó transf.</span>
                          ) : (
                            <span className={s.cargoAcciones}>
                              <button
                                className={s.btnEditarMonto}
                                title="Cambiar el monto"
                                onClick={() => setEditandoCargo({ id: c.id, concepto: c.concepto, monto: c.monto })}
                              >
                                ✎
                              </button>
                              <button
                                className={s.btnPagarCargo}
                                onClick={() => setPagandoCargo({ id: c.id, concepto: c.concepto })}
                              >
                                Pagar
                              </button>
                            </span>
                          )}
                        </div>
                      ))}
                      <div className={s.detalleAcciones}>
                        <button className={s.btnSecundario} onClick={() => setCargoPara(l)}>
                          + Agregar cargo
                        </button>
                        {l.saldo > 0 && l.estado !== 'Informado' && (
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
          medioInicial={pagandoMes.estado === 'Informado' ? 'Transferencia' : 'Efectivo'}
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
      {editandoCargo && (
        <EditarMontoModal
          concepto={editandoCargo.concepto}
          montoActual={editandoCargo.monto}
          onClose={() => setEditandoCargo(null)}
          onGuardar={(monto) => editarMonto(editandoCargo.id, monto)}
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
