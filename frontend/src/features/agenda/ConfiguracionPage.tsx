import { useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { useSedes } from './hooks';
import type { Precios } from '../cuotas/types';
import s from './ConfiguracionPage.module.css';

/** Card de precios del profe (la base de la fórmula de cuotas). */
function PreciosCard() {
  const [grupal, setGrupal] = useState('');
  const [individual, setIndividual] = useState('');
  const [guardado, setGuardado] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void api.get<Precios>('/configuracion/precios').then((p) => {
      setGrupal(p.valorHoraGrupal?.toString() ?? '');
      setIndividual(p.valorClaseIndividual?.toString() ?? '');
    });
  }, []);

  const guardar = async () => {
    setError(null);
    setGuardado(false);
    try {
      await api.put<Precios>('/configuracion/precios', {
        valorHoraGrupal: grupal === '' ? null : Number(grupal),
        valorClaseIndividual: individual === '' ? null : Number(individual),
      });
      setGuardado(true);
      setTimeout(() => setGuardado(false), 2500);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudieron guardar los precios.');
    }
  };

  return (
    <div className={s.tarjeta}>
      <h3 className={s.titulo}>Precios</h3>
      <p className={s.bajada}>
        La base de la liquidación: ambos valores son <b>por hora</b> y se prorratean
        por la duración del turno (30' = la mitad). La <b>grupal</b> además se divide
        entre los asignados del turno. Los cargos ya generados no cambian si
        actualizás estos valores.
      </p>
      {error && <div className={s.error}>{error}</div>}
      <div className={s.precios}>
        <label className={s.precio}>
          <span>Valor hora grupal</span>
          <input type="number" min={0} value={grupal} onChange={(e) => setGrupal(e.target.value)} placeholder="16000" />
        </label>
        <label className={s.precio}>
          <span>Valor hora individual</span>
          <input type="number" min={0} value={individual} onChange={(e) => setIndividual(e.target.value)} placeholder="16000" />
        </label>
        <button className={s.btnPrimario} onClick={() => void guardar()}>
          {guardado ? '✓ Guardado' : 'Guardar precios'}
        </button>
      </div>
    </div>
  );
}

/** Configuración del tenant. Por ahora: sedes y canchas (donde trabaja el profe). */
export default function ConfiguracionPage() {
  const { sedes, cargando, crearSede, agregarCancha } = useSedes();
  const [nombreSede, setNombreSede] = useState('');
  const [canchaEn, setCanchaEn] = useState<string | null>(null); // sedeId con input abierto
  const [nombreCancha, setNombreCancha] = useState('');
  const [error, setError] = useState<string | null>(null);

  const altaSede = async () => {
    if (nombreSede.trim() === '') return;
    setError(null);
    try {
      await crearSede(nombreSede.trim());
      setNombreSede('');
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear la sede.');
    }
  };

  const altaCancha = async (sedeId: string) => {
    if (nombreCancha.trim() === '') return;
    setError(null);
    try {
      await agregarCancha(sedeId, nombreCancha.trim());
      setNombreCancha('');
      setCanchaEn(null);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear la cancha.');
    }
  };

  return (
    <div className={s.contenedor}>
      <PreciosCard />
      <div className={s.tarjeta}>
        <h3 className={s.titulo}>Sedes y canchas</h3>
        <p className={s.bajada}>
          Los clubes donde trabajás y sus canchas. Los horarios se asignan a una cancha
          y no pueden superponerse dentro de la misma.
        </p>

        {error && <div className={s.error}>{error}</div>}

        <div className={s.altaSede}>
          <input
            value={nombreSede}
            onChange={(e) => setNombreSede(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && void altaSede()}
            placeholder="Nombre de la sede, ej: Club Atlético Norte"
          />
          <button className={s.btnPrimario} onClick={() => void altaSede()} disabled={nombreSede.trim() === ''}>
            + Agregar sede
          </button>
        </div>

        {cargando && <div className={s.vacio}>Cargando…</div>}
        {!cargando && sedes.length === 0 && (
          <div className={s.vacio}>Todavía no hay sedes. Agregá la primera arriba.</div>
        )}

        <div className={s.sedes}>
          {sedes.map((sede) => (
            <div key={sede.id} className={s.sede}>
              <div className={s.sedeHeader}>
                <span className={s.sedeNombre}>{sede.nombre}</span>
                <span className={s.sedeCanchas}>
                  {sede.canchas.length === 0
                    ? 'sin canchas'
                    : `${sede.canchas.length} cancha${sede.canchas.length > 1 ? 's' : ''}`}
                </span>
              </div>
              <div className={s.canchas}>
                {sede.canchas.map((c) => (
                  <span key={c.id} className={s.canchaChip}>{c.nombre}</span>
                ))}
                {canchaEn === sede.id ? (
                  <span className={s.altaCancha}>
                    <input
                      autoFocus
                      value={nombreCancha}
                      onChange={(e) => setNombreCancha(e.target.value)}
                      onKeyDown={(e) => e.key === 'Enter' && void altaCancha(sede.id)}
                      placeholder="Cancha 1"
                    />
                    <button className={s.btnMini} onClick={() => void altaCancha(sede.id)}>OK</button>
                    <button className={s.btnMiniGris} onClick={() => { setCanchaEn(null); setNombreCancha(''); }}>✕</button>
                  </span>
                ) : (
                  <button className={s.btnCancha} onClick={() => { setCanchaEn(sede.id); setNombreCancha(''); }}>
                    + cancha
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
