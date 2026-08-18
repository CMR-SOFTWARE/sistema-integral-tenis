import Avatar from '../../components/Avatar';
import MiTarjetaRanking from './MiTarjetaRanking';
import { useHistorialJugador, usePerfilPublico } from './useRanking';
import s from './RankingPage.module.css';
import p from '../portal/PortalPages.module.css';

interface Props {
  jugadorId: string;
  onVolver: () => void;
}

/** Perfil público de un jugador del ranking: se abre al tocar su fila en la
 *  tabla. A diferencia de "Mi perfil", es de solo lectura — sin "Pedir
 *  revisión" (esa acción es solo para partidos donde YO participé). */
export default function PerfilJugadorScreen({ jugadorId, onVolver }: Props) {
  const perfil = usePerfilPublico(jugadorId);
  const historial = useHistorialJugador(jugadorId);

  return (
    <div>
      <div className={s.volverHeader}>
        <button className={s.volverLink} onClick={onVolver}>← Ranking</button>
      </div>

      {perfil.isLoading && <div className={p.vacio}>Cargando…</div>}

      {perfil.data && (
        <MiTarjetaRanking
          nombre={perfil.data.nombre}
          apellido={perfil.data.apellido}
          rango={perfil.data.rango ?? '—'}
          cf={perfil.data.cf ?? 0}
          posicion={perfil.data.posicion}
          puntos={perfil.data.puntos}
          mejorPuesto={perfil.data.mejorPuestoHistorico}
          grande
        />
      )}

      <h2 className={p.seccion}>Historial</h2>
      {(!historial.data || historial.data.length === 0) && (
        <div className={p.vacio}>Todavía no jugó ningún partido.</div>
      )}
      {historial.data && historial.data.length > 0 && (
        <div className={p.tarjeta}>
          <div className={s.lista}>
            {historial.data.map((d) => {
              const esJugador1 = d.jugador1Id === jugadorId;
              const rivalNombre = esJugador1 ? d.jugador2Nombre : d.jugador1Nombre;
              const gano = d.ganadorId === jugadorId;
              const puntos = gano ? d.puntosGanador : d.puntosPerdedor;
              return (
                <div key={d.id} className={s.historialFila}>
                  <Avatar nombre={rivalNombre.split(' ')[0] ?? ''} apellido={rivalNombre.split(' ')[1] ?? ''} size={28} />
                  <span className={s.historialTexto}>
                    {gano ? 'Le ganó a' : 'Perdió con'} <b>{rivalNombre}</b>
                  </span>
                  {puntos != null && <span className={s.historialPuntos}>+{puntos}</span>}
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
