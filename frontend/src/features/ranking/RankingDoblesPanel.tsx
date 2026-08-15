import { useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { obtenerSesion } from '../auth/sesion';
import MiTarjetaRanking from './MiTarjetaRanking';
import TablaRanking from './TablaRanking';
import NuevoDesafioDoblesScreen from './NuevoDesafioDoblesScreen';
import MisJuegosDoblesScreen from './MisJuegosDoblesScreen';
import MiPerfilRankingDoblesScreen from './MiPerfilRankingDoblesScreen';
import PerfilJugadorDoblesScreen from './PerfilJugadorDoblesScreen';
import CargarResultadoDoblesModal from './CargarResultadoDoblesModal';
import {
  useInvalidarRankingDobles, useLeaderboardDobles, useMiPerfilDobles, useMisDesafiosDobles, useRankingOficialDobles,
} from './useRankingDobles';
import { useMiPerfilRanking } from './useRanking';
import type { DesafioDobles } from './types';
import s from './RankingPage.module.css';
import p from '../portal/PortalPages.module.css';

type Pantalla = 'lista' | 'nuevo-desafio' | 'mis-juegos' | 'mi-perfil' | 'perfil-jugador';
type ScopeOficial = 'Global' | 'Ciudad' | 'Provincia' | 'Pais';

interface Props {
  tab: 'singles' | 'dobles';
  onCambiarTab: (t: 'singles' | 'dobles') => void;
}

/** Tabla de dobles + flujo de desafío de parejas ad-hoc. Requiere estar
 *  inscripto en singles primero (JugadorRankingDobles es 1:1 con JugadorRanking). */
export default function RankingDoblesPanel({ tab, onCambiarTab }: Props) {
  const [pantalla, setPantalla] = useState<Pantalla>('lista');
  const [vista, setVista] = useState<'envivo' | 'oficial'>('envivo');
  const [scopeOficial, setScopeOficial] = useState<ScopeOficial>('Global');
  const [valorOficial, setValorOficial] = useState('');
  const [inscribiendo, setInscribiendo] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [cargandoResultadoDe, setCargandoResultadoDe] = useState<DesafioDobles | null>(null);
  const [jugadorViendo, setJugadorViendo] = useState<string | null>(null);

  const miPerfilSingles = useMiPerfilRanking();
  const leaderboard = useLeaderboardDobles(vista === 'envivo');
  const miPerfil = useMiPerfilDobles();
  const invalidar = useInvalidarRankingDobles();
  const misDesafios = useMisDesafiosDobles();
  const oficial = useRankingOficialDobles(scopeOficial, valorOficial);
  const sesion = obtenerSesion();

  const inscribirme = async () => {
    setError(null);
    setInscribiendo(true);
    try {
      await api.post('/ranking/dobles/inscribirme', {});
      invalidar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo inscribir.');
    } finally {
      setInscribiendo(false);
    }
  };

  if (miPerfilSingles.isLoading || miPerfil.isLoading || leaderboard.isLoading) {
    return <div className={p.vacio}>Cargando…</div>;
  }

  if (!miPerfilSingles.data?.inscripto) {
    return (
      <div>
        <TabsModalidad tab={tab} onCambiar={onCambiarTab} />
        <div className={p.vacio}>Primero tenés que inscribirte al ranking de singles.</div>
      </div>
    );
  }

  const miJugadorId = miPerfilSingles.data.jugadorId;
  const volver = () => setPantalla('lista');

  if (pantalla === 'nuevo-desafio' && miJugadorId) {
    return (
      <>
        <NuevoDesafioDoblesScreen
          jugadores={leaderboard.data ?? []}
          miJugadorId={miJugadorId}
          miNombre={sesion?.nombre ?? ''}
          miApellido={sesion?.apellido ?? ''}
          onVolver={volver}
        />
        {cargandoResultadoDe && (
          <CargarResultadoDoblesModal desafio={cargandoResultadoDe} onClose={() => setCargandoResultadoDe(null)} />
        )}
      </>
    );
  }

  if (pantalla === 'mis-juegos' && miJugadorId) {
    return (
      <>
        <MisJuegosDoblesScreen
          miJugadorId={miJugadorId}
          onVolver={volver}
          onNuevoDesafio={() => setPantalla('nuevo-desafio')}
          onCargarResultado={setCargandoResultadoDe}
        />
        {cargandoResultadoDe && (
          <CargarResultadoDoblesModal desafio={cargandoResultadoDe} onClose={() => setCargandoResultadoDe(null)} />
        )}
      </>
    );
  }

  if (pantalla === 'mi-perfil' && miJugadorId && miPerfil.data) {
    return <MiPerfilRankingDoblesScreen perfil={miPerfil.data} onVolver={volver} />;
  }

  if (pantalla === 'perfil-jugador' && jugadorViendo) {
    return <PerfilJugadorDoblesScreen jugadorId={jugadorViendo} onVolver={volver} />;
  }

  return (
    <div>
      {error && <div className={p.error}>{error}</div>}

      <TabsModalidad tab={tab} onCambiar={onCambiarTab} />

      {!miPerfil.data?.inscripto && (
        <div className={p.tarjeta}>
          <h2 className={p.seccion}>Sumate al ranking de dobles</h2>
          <p className={s.explicacion}>
            Es un pool de puntos separado del de singles: las parejas se arman
            partido a partido, no hace falta tener un compañero fijo.
          </p>
          <button className={p.btnPrimario} disabled={inscribiendo} onClick={() => void inscribirme()}>
            {inscribiendo ? 'Inscribiendo…' : 'Inscribirme al ranking de dobles'}
          </button>
        </div>
      )}

      {miPerfil.data?.inscripto && (
        <MiTarjetaRanking
          nombre={sesion?.nombre ?? ''}
          apellido={sesion?.apellido ?? ''}
          rango={miPerfil.data.rango ?? '—'}
          cf={miPerfil.data.cf ?? 0}
          posicion={miPerfil.data.posicion}
          puntos={miPerfil.data.puntos}
          mejorPuesto={miPerfil.data.mejorPuestoHistorico}
        />
      )}

      {miPerfil.data?.inscripto && (
        <>
          <button className={s.btnNuevoDesafio} onClick={() => setPantalla('nuevo-desafio')}>
            Nuevo desafío
          </button>
          <div className={s.accionesFila}>
            <button className={s.btnAccion} onClick={() => setPantalla('mis-juegos')}>
              Mis juegos
              {!!misDesafios.data?.length && <span className={s.badgeAccion}>{misDesafios.data.length}</span>}
            </button>
            <button className={s.btnAccion} onClick={() => setPantalla('mi-perfil')}>
              Mi perfil
            </button>
          </div>
        </>
      )}

      <div className={s.subtabsFila}>
        <div className={s.tabsModalidad}>
          <button className={vista === 'envivo' ? s.tabActiva : s.tab} onClick={() => setVista('envivo')}>
            En vivo
          </button>
          <button className={vista === 'oficial' ? s.tabActiva : s.tab} onClick={() => setVista('oficial')}>
            Oficial
          </button>
        </div>
        {vista === 'envivo' && (
          <span className={s.envivoEstado}>
            <span className={leaderboard.isFetching ? `${s.envivoPunto} ${s.envivoPuntoActualizando}` : s.envivoPunto} />
            {leaderboard.dataUpdatedAt
              ? new Date(leaderboard.dataUpdatedAt).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
              : 'En vivo'}
          </span>
        )}
      </div>

      <TablaRanking
        filas={vista === 'envivo' ? leaderboard.data : oficial.data}
        miJugadorId={miJugadorId}
        vista={vista}
        scope={scopeOficial}
        valorScope={valorOficial}
        onCambiarScope={(scope, valor) => { setScopeOficial(scope); setValorOficial(valor); }}
        vacioTexto={vista === 'oficial'
          ? 'Todavía no hubo ningún cierre oficial (se hace los días 1 y 16).'
          : 'Todavía no hay nadie inscripto en el ranking de dobles.'}
        onVerPerfil={(id) => { setJugadorViendo(id); setPantalla('perfil-jugador'); }}
      />

      {cargandoResultadoDe && (
        <CargarResultadoDoblesModal desafio={cargandoResultadoDe} onClose={() => setCargandoResultadoDe(null)} />
      )}
    </div>
  );
}

function TabsModalidad({ tab, onCambiar }: { tab: 'singles' | 'dobles'; onCambiar: (t: 'singles' | 'dobles') => void }) {
  return (
    <div className={s.tabsModalidad}>
      <button className={tab === 'singles' ? s.tabActiva : s.tab} onClick={() => onCambiar('singles')}>
        Singles
      </button>
      <button className={tab === 'dobles' ? s.tabActiva : s.tab} onClick={() => onCambiar('dobles')}>
        Dobles
      </button>
    </div>
  );
}
