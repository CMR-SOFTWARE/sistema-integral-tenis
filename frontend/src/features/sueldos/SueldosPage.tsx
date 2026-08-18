import { useState } from 'react';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { useSueldos } from './useSueldos';
import PagarSueldoModal from './PagarSueldoModal';
import type { EmpleadoSueldo, Medio } from './types';
import { MESES } from '../cuotas/types';
import { DIAS, horaCorta } from '../agenda/types';
import { avatarColor, formatoPlata, iniciales } from '../alumnos/types';
import { BotonNavFecha, ChevronRightIcon } from '../../components/iconos';
import s from './SueldosPage.module.css';

type FiltroSueldo = 'todos' | 'Pendiente' | 'Pagado';

const DIA_LABEL: Record<string, string> = Object.fromEntries(DIAS.map((d) => [d.valor, d.corto]));

/** Sueldos del mes: lo que cobra cada profe empleado (valor hora × horas dadas). */
export default function SueldosPage() {
  const hoy = new Date();
  const [anio, setAnio] = useState(hoy.getFullYear());
  const [mes, setMes] = useState(hoy.getMonth() + 1);
  const { datos, cargando, error, reporte, pagar, revertir } = useSueldos(anio, mes);

  const [filtro, setFiltro] = useState<FiltroSueldo>('todos');
  const [abiertos, setAbiertos] = useState<Set<string>>(new Set());
  const [pagando, setPagando] = useState<EmpleadoSueldo | null>(null);
  const confirmar = useConfirmar();

  const mesAnterior = () => {
    if (mes === 1) { setMes(12); setAnio(anio - 1); } else setMes(mes - 1);
  };
  const mesSiguiente = () => {
    if (mes === 12) { setMes(1); setAnio(anio + 1); } else setMes(mes + 1);
  };

  const toggleDetalle = (userId: string) => {
    const nuevo = new Set(abiertos);
    if (nuevo.has(userId)) nuevo.delete(userId);
    else nuevo.add(userId);
    setAbiertos(nuevo);
  };

  const deshacer = async (e: EmpleadoSueldo) => {
    if (!(await confirmar({
      titulo: `Revertir el pago de ${e.nombre} ${e.apellido}`,
      mensaje: `Borra el pago registrado de ${MESES[mes - 1]} (${formatoPlata(e.pagado)}). Podés volver a registrarlo con el monto correcto.`,
      confirmar: 'Revertir pago',
      peligro: true,
    }))) return;
    await revertir(e.userId);
  };

  const empleados = (datos?.empleados ?? [])
    .filter((e) => filtro === 'todos' || e.estado === filtro);

  return (
    <div>
      <div className={s.toolbar}>
        <BotonNavFecha direccion="anterior" className={s.nav} label="Mes anterior" onClick={mesAnterior} />
        <div className={s.rango}>{MESES[mes - 1]} {anio}</div>
        <BotonNavFecha direccion="siguiente" className={s.nav} label="Mes siguiente" onClick={mesSiguiente} />

        <div className={s.spacer} />

        <div className={s.filtros}>
          {(['todos', 'Pendiente', 'Pagado'] as const).map((f) => (
            <button
              key={f}
              className={filtro === f ? s.filtroActivo : s.filtro}
              onClick={() => setFiltro(f)}
            >
              {f === 'todos' ? 'Todos' : f + 's'}
            </button>
          ))}
        </div>
      </div>

      {error && <div className={s.error}>{error}</div>}
      {cargando && <div className={s.vacio}>Calculando el mes…</div>}

      {datos && !cargando && (
        <>
          {/* ── Panel del mes (egreso de sueldos) ── */}
          <div className={s.stats}>
            <div className={s.stat}>
              <div className={s.statValor}>{formatoPlata(datos.totalAPagar)}</div>
              <div className={s.statLabel}>A pagar (mes)</div>
            </div>
            <div className={s.stat}>
              <div className={s.statValor} style={{ color: '#0e6b3c' }}>{formatoPlata(datos.totalPagado)}</div>
              <div className={s.statLabel}>Pagado</div>
            </div>
            <div className={s.stat}>
              <div className={s.statValor} style={{ color: datos.totalPendiente > 0 ? '#b7791f' : undefined }}>
                {formatoPlata(datos.totalPendiente)}
              </div>
              <div className={s.statLabel}>Pendiente</div>
            </div>
            <div className={s.stat}>
              <div className={s.statValor}>{datos.empleados.length}</div>
              <div className={s.statLabel}>Profes</div>
            </div>
          </div>

          {/* ── Balance: sueldos pagados por mes ── */}
          {reporte.length > 0 && (
            <div className={s.balance}>
              <div className={s.balanceTitulo}>Sueldos pagados por mes</div>
              <div className={s.balanceBarras}>
                {(() => {
                  const max = Math.max(...reporte.map((r) => r.pagado), 1);
                  return reporte.map((r) => (
                    <div key={`${r.anio}-${r.mes}`} className={s.balanceItem} title={formatoPlata(r.pagado)}>
                      <div className={s.balanceBarraWrap}>
                        <div
                          className={s.balanceBarra}
                          style={{ height: `${Math.round((r.pagado / max) * 100)}%` }}
                        />
                      </div>
                      <div className={s.balanceMes}>{MESES[r.mes - 1].slice(0, 3)}</div>
                    </div>
                  ));
                })()}
              </div>
            </div>
          )}

          {datos.empleados.length === 0 && (
            <div className={s.vacioCard}>
              Todavía no tenés profes empleados con clases este mes. Sumá profes en{' '}
              <b>Profesores</b> y asignales horarios con su <b>valor hora</b>.
            </div>
          )}

          {datos.empleados.length > 0 && empleados.length === 0 && (
            <div className={s.vacioCard}>
              Ningún profe tiene el sueldo <b>{filtro.toLowerCase()}</b> este mes.
            </div>
          )}

          {/* ── Sueldo por empleado ── */}
          <div className={s.lista}>
            {empleados.map((e) => {
              const av = avatarColor(e.nombre + e.apellido);
              const abierto = abiertos.has(e.userId);
              return (
                <div key={e.userId} className={s.tarjeta}>
                  <button className={s.fila} onClick={() => toggleDetalle(e.userId)}>
                    <div className={s.avatar} style={{ background: `${av}1a`, color: av }}>
                      {iniciales(e.nombre, e.apellido)}
                    </div>
                    <div className={s.filaNombre}>
                      <span className={s.nombre}>
                        {e.nombre} {e.apellido}
                        {!e.activo && <span className={s.badgeInactivo}>Inactivo</span>}
                      </span>
                      <span className={s.sub}>{e.horasTotales} h este mes</span>
                    </div>
                    <div className={s.montos}>
                      <span className={s.total}>{formatoPlata(e.calculado)}</span>
                      {e.saldo > 0 && e.pagado > 0 && (
                        <span className={s.saldo}>falta {formatoPlata(e.saldo)}</span>
                      )}
                    </div>
                    {!e.tieneValorHora ? (
                      <span className={s.chip} style={{ background: '#f3f4f6', color: '#6b7280' }} title="Cargale el valor hora en Profesores o en el horario">
                        Sin valor hora
                      </span>
                    ) : e.estado === 'Pagado' ? (
                      <span className={s.chip} style={{ background: '#e7f6ec', color: '#0e6b3c' }}>Pagado</span>
                    ) : e.calculado === 0 ? (
                      <span className={s.chip} style={{ background: '#f3f4f6', color: '#6b7280' }}>Sin clases</span>
                    ) : (
                      <span className={s.chip} style={{ background: '#fef6e7', color: '#b7791f' }}>Pendiente</span>
                    )}
                    <span className={`${s.flecha} ${abierto ? s.flechaAbierta : ''}`} aria-hidden>
                      <ChevronRightIcon size={16} />
                    </span>
                  </button>

                  {abierto && (
                    <div className={s.detalle}>
                      {e.detalle.length === 0 && (
                        <div className={s.sinDetalle}>Sin clases dadas este mes.</div>
                      )}
                      {e.detalle.map((d) => (
                        // El título alcanza como key: las sueltas van todas en una línea
                        // sola ("Clases sueltas") y no tienen horarioId.
                        <div key={d.titulo} className={s.linea}>
                          <span className={s.lineaDia}>
                            {d.dia === '' ? horaCorta(d.horaInicio) : `${DIA_LABEL[d.dia] ?? d.dia} ${horaCorta(d.horaInicio)}`}
                          </span>
                          <span className={s.lineaTitulo}>{d.titulo}</span>
                          <span className={s.lineaHoras}>
                            {d.clases} {d.clases === 1 ? 'clase' : 'clases'} · {d.horas} h
                            {d.valorHora != null
                              ? <span className={s.lineaTarifa}> × {formatoPlata(d.valorHora)}/h</span>
                              : <span className={s.lineaSinTarifa}> · sin valor hora</span>}
                          </span>
                          <span className={s.lineaSubtotal}>{formatoPlata(d.subtotal)}</span>
                        </div>
                      ))}
                      <div className={s.detalleAcciones}>
                        {e.estado === 'Pagado' ? (
                          <>
                            <span className={s.pagadoInfo}>
                              ✓ Pagado {formatoPlata(e.pagado)}{e.medioPago ? ` · ${e.medioPago}` : ''}
                            </span>
                            <button className={s.btnRevertir} onClick={() => void deshacer(e)}>
                              Revertir
                            </button>
                          </>
                        ) : (
                          <>
                            <span className={s.spacer} />
                            <button
                              className={s.btnPagar}
                              disabled={e.saldo <= 0}
                              onClick={() => setPagando(e)}
                            >
                              Registrar pago ({formatoPlata(e.saldo)})
                            </button>
                          </>
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

      {pagando && (
        <PagarSueldoModal
          nombre={`${pagando.nombre} ${pagando.apellido} — ${MESES[mes - 1]} ${anio}`}
          montoSugerido={pagando.saldo}
          onClose={() => setPagando(null)}
          onPagar={(monto: number, medio: Medio) => pagar(pagando.userId, monto, medio)}
        />
      )}
    </div>
  );
}
