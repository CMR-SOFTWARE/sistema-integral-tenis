import { useMemo, useState } from 'react';
import { obtenerSesion } from '../auth/sesion';
import { useMes, useSedes, useSemana } from './hooks';
import TurnoModal from './TurnoModal';
import VistaMes from './VistaMes';
import PanelClasesSueltas from './PanelClasesSueltas';
import SelectSede from './SelectSede';
import SelectProfe from './SelectProfe';
import { aISO, fechaCorta, horaCorta, lunesDe, rangoSemana, sumarDias } from './types';
import type { Turno } from './types';
import { MESES } from '../cuotas/types';
import { useBloqueos } from '../bloqueos/useBloqueos';
import { cubreFecha, franjaLegible } from '../bloqueos/types';
import s from './CalendarioPage.module.css';

type Vista = 'semana' | 'mes';

/** Calendario de turnos CONCRETOS: vista semanal o mensual (se generan al pedirlos). */
export default function CalendarioPage() {
  const ahora = new Date();
  const [vista, setVista] = useState<Vista>('semana');
  const [lunes, setLunes] = useState(() => lunesDe(new Date()));
  const [mesCursor, setMesCursor] = useState({ anio: ahora.getFullYear(), mes: ahora.getMonth() + 1 });

  // Solo la vista activa consulta (la otra queda deshabilitada).
  const semana = useSemana(lunes, vista === 'semana');
  const mes = useMes(mesCursor.anio, mesCursor.mes, vista === 'mes');
  const activo = vista === 'semana' ? semana : mes;

  const { bloqueos } = useBloqueos();
  const { sedes } = useSedes();
  const [sede, setSede] = useState(''); // '' = todas
  const [profe, setProfe] = useState(''); // '' = todos
  const [abierto, setAbierto] = useState<string | null>(null); // turnoId
  const esOwner = obtenerSesion()?.rol === 'owner';

  const visibles = activo.turnos.filter(
    (t) => (sede === '' || t.sede === sede) && (profe === '' || t.profesorUserId === profe),
  );

  const dias = useMemo(() => Array.from({ length: 7 }, (_, i) => sumarDias(lunes, i)), [lunes]);
  const semanaActual = lunesDe(new Date()) === lunes;
  const mesActual = mesCursor.anio === ahora.getFullYear() && mesCursor.mes === ahora.getMonth() + 1;
  const turnoAbierto: Turno | null = activo.turnos.find((t) => t.id === abierto) ?? null;

  const cambiarMes = (delta: number) =>
    setMesCursor(({ anio, mes: m }) => {
      const total = anio * 12 + (m - 1) + delta;
      return { anio: Math.floor(total / 12), mes: (total % 12) + 1 };
    });

  const retroceder = () => (vista === 'semana' ? setLunes(sumarDias(lunes, -7)) : cambiarMes(-1));
  const avanzar = () => (vista === 'semana' ? setLunes(sumarDias(lunes, 7)) : cambiarMes(1));
  const volverAHoy = () =>
    vista === 'semana'
      ? setLunes(lunesDe(new Date()))
      : setMesCursor({ anio: ahora.getFullYear(), mes: ahora.getMonth() + 1 });
  const esHoy = vista === 'semana' ? semanaActual : mesActual;

  return (
    <div>
      <div className={s.toolbar}>
        <button className={s.nav} onClick={retroceder}>‹</button>
        <div className={s.rango}>
          {vista === 'semana' ? `Semana del ${rangoSemana(lunes)}` : `${MESES[mesCursor.mes - 1]} ${mesCursor.anio}`}
          {!esHoy && (
            <button className={s.hoy} onClick={volverAHoy}>volver a hoy</button>
          )}
        </div>
        <button className={s.nav} onClick={avanzar}>›</button>

        <div className={s.toggle}>
          <button className={vista === 'semana' ? s.toggleActivo : s.toggleBtn} onClick={() => setVista('semana')}>Semana</button>
          <button className={vista === 'mes' ? s.toggleActivo : s.toggleBtn} onClick={() => setVista('mes')}>Mes</button>
        </div>

        <SelectSede sedes={sedes} valor={sede} onChange={setSede} />
        {esOwner && <SelectProfe valor={profe} onChange={setProfe} />}
      </div>

      <div className={s.leyenda}>
        <span className={s.leyendaItem}><i className={s.puntoProgramado} /> Programado</span>
        <span className={s.leyendaItem}><i className={s.puntoCancelado} /> Cancelado</span>
        <span className={s.leyendaItem}><i className={s.puntoBloqueado} /> Bloqueado</span>
      </div>

      {esOwner && <PanelClasesSueltas onCambio={() => void activo.recargar()} />}

      {activo.error && <div className={s.error}>{activo.error} — ¿está corriendo la API?</div>}
      {activo.cargando && <div className={s.vacio}>Cargando…</div>}

      {!activo.cargando && !activo.error && vista === 'semana' && (
        <div className={s.grilla}>
          {dias.map((fecha) => {
            const delDia = visibles.filter((t) => t.fecha === fecha);
            const bloqueosDia = bloqueos.filter((b) => cubreFecha(b, fecha));
            const hoyCol = fecha === aISO(new Date());
            return (
              <div key={fecha} className={`${s.columna} ${hoyCol ? s.columnaHoy : ''}`}>
                <div className={s.columnaTitulo}>{fechaCorta(fecha)}</div>
                {bloqueosDia.map((b) => (
                  <div key={b.id} className={s.franjaBloqueada}>
                    <div className={s.turnoHora}>{franjaLegible(b.horaInicio, b.horaFin)}</div>
                    <div className={s.franjaMotivo}>
                      {b.motivo ?? 'Bloqueo fijo'}{b.cancha ? ` · ${b.cancha}` : ''}
                    </div>
                  </div>
                ))}
                {delDia.length === 0 && bloqueosDia.length === 0 && <div className={s.libre}>—</div>}
                {delDia.map((t) => {
                  const cancelado = t.estado === 'Cancelado';
                  const ausentes = t.participantes.filter((p) => !p.presente).length;
                  return (
                    <button
                      key={t.id}
                      className={`${s.turno} ${cancelado ? s.turnoCancelado : ''}`}
                      onClick={() => setAbierto(t.id)}
                    >
                      <div className={s.turnoHora}>{horaCorta(t.horaInicio)}</div>
                      <div className={s.turnoTitulo}>{t.titulo}</div>
                      <div className={s.turnoDetalle}>
                        {cancelado
                          ? `Cancelado: ${t.canceladoMotivo}`
                          : `${t.cancha} · ${t.participantes.length} 👤${ausentes > 0 ? ` · ${ausentes} falta${ausentes > 1 ? 's' : ''}` : ''}`}
                      </div>
                    </button>
                  );
                })}
              </div>
            );
          })}
        </div>
      )}

      {!activo.cargando && !activo.error && vista === 'mes' && (
        <VistaMes
          key={`${mesCursor.anio}-${mesCursor.mes}`}
          anio={mesCursor.anio}
          mes={mesCursor.mes}
          turnos={visibles}
          bloqueos={bloqueos}
          onAbrirTurno={(t) => setAbierto(t.id)}
        />
      )}

      {!activo.cargando && !activo.error && vista === 'semana' && activo.turnos.length === 0 && (
        <div className={s.vacioCard}>
          No hay turnos esta semana. Los turnos nacen de los <b>Horarios</b> (sidebar):
          creá las plantillas y esta pantalla los genera sola.
        </div>
      )}

      {turnoAbierto && (
        <TurnoModal
          turno={turnoAbierto}
          onClose={() => setAbierto(null)}
          onAsistencia={activo.marcarAsistencia}
          onCancelar={activo.cancelar}
        />
      )}
    </div>
  );
}
