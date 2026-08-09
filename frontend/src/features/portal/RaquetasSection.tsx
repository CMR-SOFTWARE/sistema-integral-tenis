import { useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { haceCuanto, resumenEncordado } from '../alumnos/types';
import FormEncordado from '../alumnos/FormEncordado';
import type { Encordado, Raqueta } from './types';
import s from './PortalPages.module.css';

interface Props {
  raquetas: Raqueta[];
  onCambio: () => void; // el padre recarga el perfil
}

const VACIA = { marca: '', modelo: '' };

/** Mis raquetas: marca y modelo, con el historial de encordados de cada una. */
export default function RaquetasSection({ raquetas, onCambio }: Props) {
  const [form, setForm] = useState(VACIA);
  const [agregando, setAgregando] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState(VACIA);
  const [encordandoId, setEncordandoId] = useState<string | null>(null);
  const [abiertoId, setAbiertoId] = useState<string | null>(null); // historial desplegado
  const [error, setError] = useState<string | null>(null);
  const confirmar = useConfirmar();

  const cuerpo = (f: typeof VACIA) => ({
    marca: f.marca.trim(),
    modelo: f.modelo.trim() || null,
  });

  const agregar = async () => {
    if (form.marca.trim() === '') return;
    setError(null);
    try {
      await api.post('/portal/raquetas', cuerpo(form));
      setForm(VACIA); setAgregando(false); onCambio();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo agregar la raqueta.');
    }
  };

  const guardarEdicion = async (id: string) => {
    if (editForm.marca.trim() === '') return;
    setError(null);
    try {
      await api.put(`/portal/raquetas/${id}`, cuerpo(editForm));
      setEditId(null); onCambio();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo guardar.');
    }
  };

  const borrar = async (r: Raqueta) => {
    if (!(await confirmar({
      titulo: 'Borrar raqueta',
      mensaje: `¿Borrar la raqueta "${r.marca}"? Se va con todo su historial de encordado.`,
      confirmar: 'Borrar',
      peligro: true,
    }))) return;
    setError(null);
    try {
      await api.delete(`/portal/raquetas/${r.id}`);
      onCambio();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo borrar.');
    }
  };

  const borrarEncordado = async (e: Encordado) => {
    if (!(await confirmar({
      titulo: 'Borrar del historial',
      mensaje: `¿Borrar el encordado del ${e.fecha}?`,
      confirmar: 'Borrar',
      peligro: true,
    }))) return;
    setError(null);
    try {
      await api.delete(`/portal/encordados/${e.id}`);
      onCambio();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo borrar.');
    }
  };

  const empezarEdicion = (r: Raqueta) => {
    setEditId(r.id);
    setEditForm({ marca: r.marca, modelo: r.modelo ?? '' });
  };

  return (
    <div className={s.tarjeta}>
      <div className={s.raquetaHead}>
        <h3 className={s.tarjetaTitulo}>Mis raquetas</h3>
        {!agregando && (
          <button className={s.btnAvisarCargo} onClick={() => { setAgregando(true); setForm(VACIA); }}>
            + Agregar
          </button>
        )}
      </div>

      {error && <div className={s.error}>{error}</div>}

      {agregando && (
        <div className={s.raquetaForm}>
          <input placeholder="Marca (ej: Wilson)" value={form.marca} onChange={(e) => setForm((f) => ({ ...f, marca: e.target.value }))} maxLength={80} />
          <input placeholder="Modelo (ej: Blade 98 v8)" value={form.modelo} onChange={(e) => setForm((f) => ({ ...f, modelo: e.target.value }))} maxLength={80} />
          <div className={s.raquetaAcciones}>
            <button className={s.btnEditar} onClick={() => { setAgregando(false); setError(null); }}>Cancelar</button>
            <button className={s.btnGuardar} disabled={form.marca.trim() === ''} onClick={() => void agregar()}>Guardar</button>
          </div>
        </div>
      )}

      {raquetas.length === 0 && !agregando && (
        <div className={s.vacio}>Todavía no cargaste ninguna raqueta.</div>
      )}

      {raquetas.map((r) => (
        editId === r.id ? (
          <div key={r.id} className={s.raquetaForm}>
            <input placeholder="Marca" value={editForm.marca} onChange={(e) => setEditForm((f) => ({ ...f, marca: e.target.value }))} maxLength={80} />
            <input placeholder="Modelo" value={editForm.modelo} onChange={(e) => setEditForm((f) => ({ ...f, modelo: e.target.value }))} maxLength={80} />
            <div className={s.raquetaAcciones}>
              <button className={s.btnEditar} onClick={() => setEditId(null)}>Cancelar</button>
              <button className={s.btnGuardar} disabled={editForm.marca.trim() === ''} onClick={() => void guardarEdicion(r.id)}>Guardar</button>
            </div>
          </div>
        ) : (
          <div key={r.id} className={s.raquetaBloque}>
            <div className={s.raquetaFila}>
              <div className={s.raquetaInfo}>
                <div className={s.raquetaMarca}>
                  {r.marca}{r.modelo ? ` ${r.modelo}` : ''}
                </div>
                <div className={s.raquetaDetalle}>
                  {r.ultimoEncordado
                    ? `${resumenEncordado(r.ultimoEncordado)} · ${haceCuanto(r.ultimoEncordado.fecha)}`
                    : 'Sin encordado cargado'}
                </div>
              </div>
              <button className={s.btnMiniPortal} onClick={() => empezarEdicion(r)}>Editar</button>
              <button className={s.btnMiniPortal} onClick={() => void borrar(r)}>Borrar</button>
            </div>

            <div className={s.raquetaAcciones}>
              {r.encordados.length > 1 && (
                <button
                  className={s.btnMiniPortal}
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
                onGuardar={async (cuerpoEncordado) => {
                  await api.post(`/portal/raquetas/${r.id}/encordados`, cuerpoEncordado);
                  setEncordandoId(null);
                  onCambio();
                }}
                onError={setError}
              />
            )}

            {abiertoId === r.id && (
              <div className={s.raquetaHistorial}>
                {r.encordados.map((e) => (
                  <div key={e.id} className={s.encordadoFila}>
                    <span>
                      <b>{e.fecha}</b> — {resumenEncordado(e)}
                      {e.esHibrido && <span className={s.chipHibrido}>híbrido</span>}
                    </span>
                    <button className={s.btnMiniPortal} onClick={() => void borrarEncordado(e)}>Borrar</button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )
      ))}
    </div>
  );
}
