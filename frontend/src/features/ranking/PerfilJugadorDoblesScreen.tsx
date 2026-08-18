import Avatar from '../../components/Avatar';
import CargaTenis from '../../components/motion/CargaTenis';
import MiTarjetaRanking from './MiTarjetaRanking';
import { useHistorialJugadorDobles, usePerfilPublicoDobles } from './useRankingDobles';
import s from './RankingPage.module.css';
import p from '../portal/PortalPages.module.css';

interface Props {
  jugadorId: string;
  onVolver: () => void;
}

/** Igual que PerfilJugadorScreen pero de dobles: perfil público de solo
 *  lectura (sin "Pedir revisión") con su historial de partidos. */
export default function PerfilJugadorDoblesScreen({ jugadorId, onVolver }: Props) {
  const perfil = usePerfilPublicoDobles(jugadorId);
  const historial = useHistorialJugadorDobles(jugadorId);

  return (
    <div className="motion-perfil">
      <div className={s.volverHeader}>
        <button className={s.volverLink} onClick={onVolver}>← Ranking dobles</button>
      </div>

      {perfil.isLoading && <CargaTenis />}

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

      <h2 className={p.seccion}>Historial de dobles</h2>
      {(!historial.data || historial.data.length === 0) && (
        <div className={p.vacio}>Todavía no jugó ningún partido de dobles.</div>
      )}
      {historial.data && historial.data.length > 0 && (
        <div className={p.tarjeta}>
          <div className={`${s.lista} motion-stagger`}>
            {historial.data.map((d) => {
              const soyPropiaPareja = d.jugador1Id === jugadorId || d.jugador2Id === jugadorId;
              const rivalTexto = soyPropiaPareja
                ? `${d.rival1Nombre} y ${d.rival2Nombre}`
                : `${d.jugador1Nombre} y ${d.jugador2Nombre}`;
              const gano = soyPropiaPareja === d.ganoParejaA;
              const puntos = gano ? d.puntosGanadores : d.puntosPerdedores;
              return (
                <div key={d.id} className={s.historialFila}>
                  <Avatar nombre={rivalTexto.split(' ')[0] ?? ''} apellido="" size={28} />
                  <span className={s.historialTexto}>
                    {gano ? 'Le ganaron a' : 'Perdieron con'} <b>{rivalTexto}</b>
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
