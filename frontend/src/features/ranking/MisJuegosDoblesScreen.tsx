import { useState } from 'react';
import { api, ApiError } from '../../lib/api';
import PedirRevisionModal from './PedirRevisionModal';
import { useInvalidarRankingDobles, useMisDesafiosDobles, useMisFinalizadosDobles } from './useRankingDobles';
import type { DesafioDobles } from './types';
import s from './RankingPage.module.css';
import p from '../portal/PortalPages.module.css';

interface Props {
  miJugadorId: string;
  onVolver: () => void;
  onNuevoDesafio: () => void;
  onCargarResultado: (desafio: DesafioDobles) => void;
}

/** Igual que MisJuegosScreen pero de a dos parejas por desafío. */
export default function MisJuegosDoblesScreen({ miJugadorId, onVolver, onNuevoDesafio, onCargarResultado }: Props) {
  const { data: desafios } = useMisDesafiosDobles();
  const misFinalizados = useMisFinalizadosDobles();
  const invalidar = useInvalidarRankingDobles();
  const [ocupado, setOcupado] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pidiendoRevisionDe, setPidiendoRevisionDe] = useState<DesafioDobles | null>(null);

  const accion = async (id: string, ruta: 'aceptar' | 'rechazar' | 'cancelar') => {
    setError(null);
    setOcupado(id);
    try {
      await api.post(`/desafios/dobles/${id}/${ruta}`, {});
      invalidar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo completar la acción.');
    } finally {
      setOcupado(null);
    }
  };

  return (
    <div>
      <div className={s.volverHeader}>
        <button className={s.volverLink} onClick={onVolver}>← Ranking dobles</button>
      </div>
      <h2 className={s.pantallaTitulo}>Mis juegos de dobles</h2>

      {error && <div className={p.error}>{error}</div>}

      {(!desafios || desafios.length === 0) && (
        <div className={p.vacio}>No tenés partidos de dobles en curso.</div>
      )}

      {desafios?.map((d) => {
        const soyPropiaPareja = d.jugador1Id === miJugadorId || d.jugador2Id === miJugadorId;
        const miEquipo = soyPropiaPareja ? `${d.jugador1Nombre} / ${d.jugador2Nombre}` : `${d.rival1Nombre} / ${d.rival2Nombre}`;
        const equipoRival = soyPropiaPareja ? `${d.rival1Nombre} / ${d.rival2Nombre}` : `${d.jugador1Nombre} / ${d.jugador2Nombre}`;
        const meDesafiaron = d.estado === 'Propuesto' && !soyPropiaPareja;
        const esperandoMiPropuesta = d.estado === 'Propuesto' && soyPropiaPareja;

        return (
          <div key={d.id} className={s.tarjetaJuego}>
            <div className={s.tarjetaJuegoEyebrow}>Desafío</div>
            <div className={s.tarjetaJuegoEquipo}>
              <span className={s.tarjetaJuegoEquipoLabel}>Tu pareja</span>
              <span className={s.tarjetaJuegoNombres}>{miEquipo}</span>
            </div>
            <div className={s.tarjetaJuegoEquipo}>
              <span className={s.tarjetaJuegoEquipoLabel}>Pareja rival</span>
              <span className={s.tarjetaJuegoNombres}>{equipoRival}</span>
            </div>
            <div className={s.tarjetaJuegoEstado}>
              {meDesafiaron && <>Los desafiaron a un partido de dobles.</>}
              {esperandoMiPropuesta && <>Esperando que algún rival acepte.</>}
              {d.estado === 'Aceptado' && <>Partido aceptado — falta cargar el resultado.</>}
            </div>
            <div className={s.tarjetaJuegoAcciones}>
              {meDesafiaron && (
                <>
                  <button className={s.btnRechazar} disabled={ocupado === d.id} onClick={() => void accion(d.id, 'rechazar')}>
                    Rechazar
                  </button>
                  <button className={s.btnAceptar} disabled={ocupado === d.id} onClick={() => void accion(d.id, 'aceptar')}>
                    {ocupado === d.id ? '…' : 'Aceptar'}
                  </button>
                </>
              )}
              {esperandoMiPropuesta && (
                <button className={s.btnRechazar} disabled={ocupado === d.id} onClick={() => void accion(d.id, 'cancelar')}>
                  Cancelar desafío
                </button>
              )}
              {d.estado === 'Aceptado' && (
                <>
                  <button className={s.btnRechazar} disabled={ocupado === d.id} onClick={() => void accion(d.id, 'cancelar')}>
                    Cancelar
                  </button>
                  <button className={s.btnAceptar} onClick={() => onCargarResultado(d)}>
                    Cargar resultado
                  </button>
                </>
              )}
            </div>
          </div>
        );
      })}

      <button className={p.btnPrimario} onClick={onNuevoDesafio}>
        Nuevo desafío
      </button>

      <h2 className={p.seccion}>Mi historial de dobles</h2>
      {(!misFinalizados.data || misFinalizados.data.length === 0) && (
        <div className={p.vacio}>Todavía no jugaste ningún partido de dobles.</div>
      )}
      {misFinalizados.data && misFinalizados.data.length > 0 && (
        <div className={p.tarjeta}>
          <div className={s.lista}>
            {misFinalizados.data.map((d) => {
              const soyPropiaPareja = d.jugador1Id === miJugadorId || d.jugador2Id === miJugadorId;
              const rivalTexto = soyPropiaPareja
                ? `${d.rival1Nombre} y ${d.rival2Nombre}`
                : `${d.jugador1Nombre} y ${d.jugador2Nombre}`;
              const gane = soyPropiaPareja === d.ganoParejaA;
              const puntos = gane ? d.puntosGanadores : d.puntosPerdedores;
              return (
                <div key={d.id} className={s.historialFila}>
                  <span className={s.historialTexto}>
                    {gane ? 'Le ganaron a' : 'Perdieron con'} <b>{rivalTexto}</b>
                  </span>
                  {puntos != null && <span className={s.historialPuntos}>+{puntos}</span>}
                  <button className={s.btnRevision} onClick={() => setPidiendoRevisionDe(d)}>
                    Pedir revisión
                  </button>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {pidiendoRevisionDe && (
        <PedirRevisionModal
          juegoDoblesPendienteId={pidiendoRevisionDe.id}
          subtitulo={
            pidiendoRevisionDe.jugador1Id === miJugadorId || pidiendoRevisionDe.jugador2Id === miJugadorId
              ? `vs. ${pidiendoRevisionDe.rival1Nombre} y ${pidiendoRevisionDe.rival2Nombre}`
              : `vs. ${pidiendoRevisionDe.jugador1Nombre} y ${pidiendoRevisionDe.jugador2Nombre}`
          }
          onClose={() => setPidiendoRevisionDe(null)}
        />
      )}
    </div>
  );
}
