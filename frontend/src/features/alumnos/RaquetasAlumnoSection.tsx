import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api, ApiError } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import FormEncordado from './FormEncordado';
import { fechaLegible, resumenEncordado, tituloRaqueta, detalleRaqueta } from './types';
import type { Raqueta } from './types';
import s from './RaquetasAlumnoSection.module.css';

interface Props {
  alumnoId: string;
  /** Cómo se llama, para las confirmaciones. */
  nombre: string;
}

/**
 * Las raquetas del alumno en la ficha del profe. Lo que el profe mira de verdad es
 * HACE CUÁNTO que está encordada cada una: una cuerda vencida cambia cómo juega y
 * es el momento de ofrecerle encordarla. Por eso el "hace 3 meses" va adelante y
 * el historial completo queda desplegable.
 *
 * El profe también puede cargar raquetas y encordados: muchas veces es él quien
 * encorda, y el alumno no siempre las tiene cargadas.
 */
export default function RaquetasAlumnoSection({ alumnoId, nombre }: Props) {
  const qc = useQueryClient();
  const confirmar = useConfirmar();
  const [error, setError] = useState<string | null>(null);
  const [encordandoId, setEncordandoId] = useState<string | null>(null);
  const [abiertoId, setAbiertoId] = useState<string | null>(null);
  const [agregando, setAgregando] = useState(false);
  const [nombreRaqueta, setNombreRaqueta] = useState('');
  const [marca, setMarca] = useState('');
  const [modelo, setModelo] = useState('');

  const raquetas = useQuery({
    queryKey: ['alumno-raquetas', alumnoId],
    queryFn: () => api.get<Raqueta[]>(`/alumnos/${alumnoId}/raquetas`),
  });

  const recargar = () => qc.invalidateQueries({ queryKey: ['alumno-raquetas', alumnoId] });

  const agregar = async () => {
    if (marca.trim() === '') return;
    setError(null);
    try {
      await api.post(`/alumnos/${alumnoId}/raquetas`, {
        nombre: nombreRaqueta.trim() || undefined,
        marca: marca.trim(),
        modelo: modelo.trim() || null,
      });
      setNombreRaqueta(''); setMarca(''); setModelo(''); setAgregando(false);
      await recargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo agregar la raqueta.');
    }
  };

  const borrar = async (r: Raqueta) => {
    if (!(await confirmar({
      titulo: 'Borrar raqueta',
      mensaje: `¿Borrar la "${tituloRaqueta(r)}" de ${nombre}? Se va con todo su historial de encordado.`,
      confirmar: 'Borrar',
      peligro: true,
    }))) return;
    setError(null);
    try {
      await api.delete(`/alumnos/${alumnoId}/raquetas/${r.id}`);
      await recargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo borrar.');
    }
  };

  const lista = raquetas.data ?? [];

  return (
    <div>
      <div className={s.cabecera}>
        <span>Raquetas</span>
        {!agregando && (
          <button className={s.btnAgregar} onClick={() => { setAgregando(true); setError(null); }}>
            + Agregar
          </button>
        )}
      </div>

      {error && <div className={s.error}>{error}</div>}

      {agregando && (
        <div className={s.altaForm}>
          <input placeholder="Nombre (opcional, ej: Raqueta 1)" value={nombreRaqueta} onChange={(e) => setNombreRaqueta(e.target.value)} maxLength={60} />
          <input placeholder="Marca (ej: Wilson)" value={marca} onChange={(e) => setMarca(e.target.value)} maxLength={80} />
          <input placeholder="Modelo (ej: Blade 98)" value={modelo} onChange={(e) => setModelo(e.target.value)} maxLength={80} />
          <button className={s.btnMini} onClick={() => setAgregando(false)}>Cancelar</button>
          <button className={s.btnGuardar} disabled={marca.trim() === ''} onClick={() => void agregar()}>
            Guardar
          </button>
        </div>
      )}

      {raquetas.isLoading && <div className={s.placeholder}>Cargando…</div>}
      {!raquetas.isLoading && lista.length === 0 && !agregando && (
        <div className={s.placeholder}>Todavía no cargó ninguna raqueta.</div>
      )}

      {lista.map((r) => (
        <div key={r.id} className={s.raqueta}>
          <div className={s.fila}>
            <div className={s.datos}>
              <div className={s.nombre}>{tituloRaqueta(r)}</div>
              {/* Con nombre propio, la marca pasa a ser el subtítulo. */}
              {detalleRaqueta(r) && <div className={s.detalle}>{detalleRaqueta(r)}</div>}
              {r.ultimoEncordado ? (
                <div className={s.detalle}>
                  {resumenEncordado(r.ultimoEncordado)}
                  {/* La fecha EXACTA y no un "hace 5 meses": el profe es el que
                      encorda, y para decidir necesita el dato, no la sensación. */}
                  <span className={s.fecha}>{fechaLegible(r.ultimoEncordado.fecha)}</span>
                </div>
              ) : (
                <div className={s.detalle}>Sin encordado registrado</div>
              )}
            </div>
            <button className={s.btnMini} onClick={() => void borrar(r)}>Borrar</button>
          </div>

          <div className={s.acciones}>
            {r.encordados.length > 1 && (
              <button
                className={s.btnMini}
                onClick={() => setAbiertoId(abiertoId === r.id ? null : r.id)}
              >
                {abiertoId === r.id ? 'Ocultar historial' : `Ver historial (${r.encordados.length})`}
              </button>
            )}
            {encordandoId !== r.id && (
              <button className={s.btnGuardar} onClick={() => { setEncordandoId(r.id); setError(null); }}>
                Registrar encordado
              </button>
            )}
          </div>

          {encordandoId === r.id && (
            <FormEncordado
              onCancelar={() => setEncordandoId(null)}
              onGuardar={async (cuerpo) => {
                await api.post(`/alumnos/${alumnoId}/raquetas/${r.id}/encordados`, cuerpo);
                setEncordandoId(null);
                await recargar();
              }}
              onError={setError}
            />
          )}

          {abiertoId === r.id && (
            <div className={s.historial}>
              {r.encordados.map((e) => (
                <div key={e.id} className={s.encordado}>
                  <b>{fechaLegible(e.fecha)}</b> — {resumenEncordado(e)}
                </div>
              ))}
            </div>
          )}
        </div>
      ))}
    </div>
  );
}
