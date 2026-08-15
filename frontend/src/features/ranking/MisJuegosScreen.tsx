import { useState } from 'react';
import { api, ApiError } from '../../lib/api';
import PedirRevisionModal from './PedirRevisionModal';
import { useInvalidarRanking, useMisDesafios, useMisFinalizados } from './useRanking';
import type { Desafio } from './types';
import s from './RankingPage.module.css';
import p from '../portal/PortalPages.module.css';

interface Props {
  miJugadorId: string;
  onVolver: () => void;
  onNuevoDesafio: () => void;
  onCargarResultado: (desafio: Desafio) => void;
}

/** Pantalla "Mis juegos": mis desafíos pendientes o aceptados, con la acción
 *  que corresponda a cada uno (mismo estado que antes vivía en el panel
 *  inline de la lista, ahora en su propia pantalla). */
export default function MisJuegosScreen({ miJugadorId, onVolver, onNuevoDesafio, onCargarResultado }: Props) {
  const { data: desafios } = useMisDesafios();
  const misFinalizados = useMisFinalizados();
  const invalidar = useInvalidarRanking();
  const [ocupado, setOcupado] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pidiendoRevisionDe, setPidiendoRevisionDe] = useState<Desafio | null>(null);

  const accion = async (id: string, ruta: 'aceptar' | 'rechazar' | 'cancelar') => {
    setError(null);
    setOcupado(id);
    try {
      await api.post(`/desafios/${id}/${ruta}`, {});
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
        <button className={s.volverLink} onClick={onVolver}>← Ranking</button>
      </div>
      <h2 className={s.pantallaTitulo}>Mis juegos</h2>

      {error && <div className={p.error}>{error}</div>}

      {(!desafios || desafios.length === 0) && (
        <div className={p.vacio}>No tenés desafíos en curso.</div>
      )}

      {desafios?.map((d) => {
        const soyJugador1 = d.jugador1Id === miJugadorId;
        const rivalNombre = soyJugador1 ? d.jugador2Nombre : d.jugador1Nombre;
        const meDesafiaron = d.estado === 'Propuesto' && !soyJugador1;
        const esperandoMiPropuesta = d.estado === 'Propuesto' && soyJugador1;

        return (
          <div key={d.id} className={s.tarjetaJuego}>
            <div className={s.tarjetaJuegoEyebrow}>Desafío</div>
            <div className={s.tarjetaJuegoEquipo}>
              <span className={s.tarjetaJuegoEquipoLabel}>Rival</span>
              <span className={s.tarjetaJuegoNombres}>{rivalNombre}</span>
            </div>
            <div className={s.tarjetaJuegoEstado}>
              {meDesafiaron && <>Te desafió a un juego.</>}
              {esperandoMiPropuesta && <>Esperando que {rivalNombre} acepte.</>}
              {d.estado === 'Aceptado' && <>Desafío aceptado — falta cargar el resultado.</>}
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

      <h2 className={p.seccion}>Mi historial</h2>
      {(!misFinalizados.data || misFinalizados.data.length === 0) && (
        <div className={p.vacio}>Todavía no jugaste ningún partido.</div>
      )}
      {misFinalizados.data && misFinalizados.data.length > 0 && (
        <div className={p.tarjeta}>
          <div className={s.lista}>
            {misFinalizados.data.map((d) => {
              const soyJugador1 = d.jugador1Id === miJugadorId;
              const rival = soyJugador1 ? d.jugador2Nombre : d.jugador1Nombre;
              const gane = d.ganadorId === miJugadorId;
              const puntos = gane ? d.puntosGanador : d.puntosPerdedor;
              return (
                <div key={d.id} className={s.historialFila}>
                  <span className={s.historialTexto}>
                    {gane ? 'Le ganaste a' : 'Perdiste con'} <b>{rival}</b>
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
          juegoPendienteId={pidiendoRevisionDe.id}
          subtitulo={`vs. ${pidiendoRevisionDe.jugador1Id === miJugadorId ? pidiendoRevisionDe.jugador2Nombre : pidiendoRevisionDe.jugador1Nombre}`}
          onClose={() => setPidiendoRevisionDe(null)}
        />
      )}
    </div>
  );
}
