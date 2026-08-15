import { useMemo, useState } from 'react';
import Avatar from '../../components/Avatar';
import s from './RankingPage.module.css';
import p from '../portal/PortalPages.module.css';

interface FilaRankingBase {
  jugadorId: string;
  nombre: string;
  apellido: string;
  posicion: number;
  puntos: number;
  rango: string;
}

type ScopeOficial = 'Global' | 'Ciudad' | 'Provincia' | 'Pais';

interface Props<T extends FilaRankingBase> {
  filas: T[] | undefined;
  miJugadorId: string | null;
  vista: 'envivo' | 'oficial';
  scope: ScopeOficial;
  valorScope: string;
  onCambiarScope: (scope: ScopeOficial, valor: string) => void;
  vacioTexto: string;
  /** Si se pasa, cada fila se puede tocar para ver el perfil público de ese jugador. */
  onVerPerfil?: (jugadorId: string) => void;
}

/** Buscador + (para "Oficial") filtro de ámbito + lista de posiciones.
 *  Una sola tabla para singles y dobles: recibe las filas ya resueltas. */
export default function TablaRanking<T extends FilaRankingBase>({
  filas, miJugadorId, vista, scope, valorScope, onCambiarScope, vacioTexto, onVerPerfil,
}: Props<T>) {
  const [busqueda, setBusqueda] = useState('');
  const [filtroAbierto, setFiltroAbierto] = useState(false);

  const visibles = useMemo(() => {
    const q = busqueda.trim().toLowerCase();
    if (!q) return filas ?? [];
    return (filas ?? []).filter((f) => `${f.nombre} ${f.apellido}`.toLowerCase().includes(q));
  }, [filas, busqueda]);

  return (
    <div>
      <div className={s.buscadorFila}>
        <input
          className={s.buscadorInput}
          value={busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
          placeholder="Buscar jugador"
          aria-label="Buscar jugador"
        />
        {vista === 'oficial' && (
          <button
            type="button"
            className={filtroAbierto || scope !== 'Global' ? s.filtroBtnActivo : s.filtroBtn}
            aria-label="Filtros"
            aria-expanded={filtroAbierto}
            onClick={() => setFiltroAbierto((v) => !v)}
          >
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
              <path d="M4 7h11M18 7h2M4 17h2M9 17h11" />
              <circle cx="16.5" cy="7" r="2" />
              <circle cx="7.5" cy="17" r="2" />
            </svg>
          </button>
        )}
      </div>

      {vista === 'oficial' && filtroAbierto && (
        <div className={s.filtrosOficial}>
          <select
            value={scope}
            onChange={(e) => onCambiarScope(e.target.value as ScopeOficial, '')}
          >
            <option value="Global">Global</option>
            <option value="Ciudad">Por ciudad</option>
            <option value="Provincia">Por provincia</option>
            <option value="Pais">Por país</option>
          </select>
          {scope !== 'Global' && (
            <input
              value={valorScope}
              onChange={(e) => onCambiarScope(scope, e.target.value)}
              placeholder={scope === 'Pais' ? 'Nombre del país' : `Nombre de la ${scope.toLowerCase()}`}
            />
          )}
        </div>
      )}

      <div className={p.tarjeta}>
        <div className={s.categoriaHeader}>
          <span aria-hidden>🎾</span>
          <span className={s.categoriaHeaderTitulo}>Tenis</span>
          <span className={s.categoriaHeaderTag}>{vista === 'envivo' ? 'Provisional' : 'Oficial'}</span>
        </div>
        {(filas ?? []).length === 0 && <div className={p.vacio}>{vacioTexto}</div>}
        {(filas ?? []).length > 0 && visibles.length === 0 && (
          <div className={p.vacio}>Ningún jugador coincide con la búsqueda.</div>
        )}
        <div className={s.lista}>
          {visibles.map((f) => {
            const claseFila = f.jugadorId === miJugadorId ? `${s.fila} ${s.filaPropia}` : s.fila;
            const contenido = (
              <>
                <span className={s.posicion}>{f.posicion}</span>
                <Avatar nombre={f.nombre} apellido={f.apellido} size={32} />
                <div className={s.filaInfo}>
                  <div className={s.filaNombre}>{f.nombre} {f.apellido}</div>
                  <div className={s.filaSub}>Rango {f.rango}</div>
                </div>
                <span className={s.puntosGrandes}>{f.puntos}</span>
              </>
            );
            return onVerPerfil ? (
              <button key={f.jugadorId} type="button" className={`${claseFila} ${s.filaClickeable}`} onClick={() => onVerPerfil(f.jugadorId)}>
                {contenido}
              </button>
            ) : (
              <div key={f.jugadorId} className={claseFila}>
                {contenido}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
