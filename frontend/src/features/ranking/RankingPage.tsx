import { useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { obtenerSesion } from '../auth/sesion';
import MiTarjetaRanking from './MiTarjetaRanking';
import TablaRanking from './TablaRanking';
import NuevoDesafioScreen from './NuevoDesafioScreen';
import MisJuegosScreen from './MisJuegosScreen';
import MiPerfilRankingScreen from './MiPerfilRankingScreen';
import PerfilJugadorScreen from './PerfilJugadorScreen';
import CargarResultadoModal from './CargarResultadoModal';
import RankingDoblesPanel from './RankingDoblesPanel';
import {
  useInvalidarRanking, useLeaderboard, useMiPerfilRanking, useMisDesafios, useRankingOficial,
} from './useRanking';
import type { Desafio } from './types';
import s from './RankingPage.module.css';
import p from '../portal/PortalPages.module.css';

type Pantalla = 'lista' | 'nuevo-desafio' | 'mis-juegos' | 'mi-perfil' | 'perfil-jugador';
type ScopeOficial = 'Global' | 'Ciudad' | 'Provincia' | 'Pais';

/** Ranking R.U.T.A.: leaderboard cross-tenant en vivo, inscripción, y el flujo
 *  de desafío completo (desafiar → aceptar/rechazar → cargar resultado). */
export default function RankingPage() {
  const [tab, setTab] = useState<'singles' | 'dobles'>('singles');
  const [pantalla, setPantalla] = useState<Pantalla>('lista');
  const [vista, setVista] = useState<'envivo' | 'oficial'>('envivo');
  const [scopeOficial, setScopeOficial] = useState<ScopeOficial>('Global');
  const [valorOficial, setValorOficial] = useState('');
  const [inscribiendo, setInscribiendo] = useState(false);
  const [ciudad, setCiudad] = useState('');
  const [provincia, setProvincia] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [cargandoResultadoDe, setCargandoResultadoDe] = useState<Desafio | null>(null);
  const [jugadorViendo, setJugadorViendo] = useState<string | null>(null);

  const leaderboard = useLeaderboard(vista === 'envivo');
  const miPerfil = useMiPerfilRanking();
  const invalidar = useInvalidarRanking();
  const misDesafios = useMisDesafios();
  const oficial = useRankingOficial(scopeOficial, valorOficial);
  const sesion = obtenerSesion();

  const inscribirme = async () => {
    setError(null);
    setInscribiendo(true);
    try {
      await api.post('/ranking/inscribirme', {
        ciudadResidencia: ciudad.trim() || undefined,
        provincia: provincia.trim() || undefined,
      });
      invalidar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo inscribir.');
    } finally {
      setInscribiendo(false);
    }
  };

  if (miPerfil.isLoading || leaderboard.isLoading) {
    return <div className={p.vacio}>Cargando…</div>;
  }

  const miJugadorId = miPerfil.data?.jugadorId ?? null;
  const volver = () => setPantalla('lista');
  const cambiarTab = (t: 'singles' | 'dobles') => { setTab(t); setPantalla('lista'); };

  if (tab === 'dobles') {
    return <RankingDoblesPanel tab={tab} onCambiarTab={cambiarTab} />;
  }

  if (pantalla === 'nuevo-desafio' && miJugadorId) {
    return (
      <>
        <NuevoDesafioScreen
          jugadores={leaderboard.data ?? []}
          miJugadorId={miJugadorId}
          onVolver={volver}
        />
        {cargandoResultadoDe && (
          <CargarResultadoModal desafio={cargandoResultadoDe} onClose={() => setCargandoResultadoDe(null)} />
        )}
      </>
    );
  }

  if (pantalla === 'mis-juegos' && miJugadorId) {
    return (
      <>
        <MisJuegosScreen
          miJugadorId={miJugadorId}
          onVolver={volver}
          onNuevoDesafio={() => setPantalla('nuevo-desafio')}
          onCargarResultado={setCargandoResultadoDe}
        />
        {cargandoResultadoDe && (
          <CargarResultadoModal desafio={cargandoResultadoDe} onClose={() => setCargandoResultadoDe(null)} />
        )}
      </>
    );
  }

  if (pantalla === 'mi-perfil' && miJugadorId && miPerfil.data) {
    return <MiPerfilRankingScreen perfil={miPerfil.data} onVolver={volver} />;
  }

  if (pantalla === 'perfil-jugador' && jugadorViendo) {
    return <PerfilJugadorScreen jugadorId={jugadorViendo} onVolver={volver} />;
  }

  return (
    <div>
      {error && <div className={p.error}>{error}</div>}
      <p className={s.subtitulo}>El oficial se actualiza los días 1 y 16. El de abajo es en vivo.</p>

      <TabsModalidad tab={tab} onCambiar={cambiarTab} />

      {miPerfil.data && !miPerfil.data.inscripto && (
        <div className={p.tarjeta}>
          <h2 className={p.seccion}>Sumate al ranking</h2>
          <p className={s.explicacion}>
            Arrancás con 0 puntos, al final de la tabla. Estos datos son
            opcionales — los podés completar después.
          </p>
          <div className={s.formInscripcion}>
            <label className={s.campo}>
              <span>Ciudad</span>
              <input value={ciudad} onChange={(e) => setCiudad(e.target.value)} placeholder="Tu ciudad" />
            </label>
            <label className={s.campo}>
              <span>Provincia</span>
              <input value={provincia} onChange={(e) => setProvincia(e.target.value)} placeholder="Tu provincia" />
            </label>
          </div>
          <button className={p.btnPrimario} disabled={inscribiendo} onClick={() => void inscribirme()}>
            {inscribiendo ? 'Inscribiendo…' : 'Inscribirme al ranking'}
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

      {miJugadorId && (
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
          : 'Todavía no hay nadie inscripto en el ranking.'}
        onVerPerfil={(id) => { setJugadorViendo(id); setPantalla('perfil-jugador'); }}
      />

      {cargandoResultadoDe && (
        <CargarResultadoModal desafio={cargandoResultadoDe} onClose={() => setCargandoResultadoDe(null)} />
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
