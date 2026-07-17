import { useState } from 'react';
import { api, ApiError } from '../../lib/api';
import type { Raqueta } from './types';
import s from './PortalPages.module.css';

interface Props {
  raquetas: Raqueta[];
  onCambio: () => void; // el padre recarga el perfil
}

const VACIA = { marca: '', tension: '', marcaEncordado: '' };

/** Mis raquetas: cada una con marca + tensión + marca del encordado. */
export default function RaquetasSection({ raquetas, onCambio }: Props) {
  const [form, setForm] = useState(VACIA);
  const [agregando, setAgregando] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState(VACIA);
  const [error, setError] = useState<string | null>(null);

  const cuerpo = (f: typeof VACIA) => ({
    marca: f.marca.trim(),
    tension: f.tension.trim() || null,
    marcaEncordado: f.marcaEncordado.trim() || null,
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
    if (!window.confirm(`¿Borrar la raqueta "${r.marca}"?`)) return;
    setError(null);
    try {
      await api.delete(`/portal/raquetas/${r.id}`);
      onCambio();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo borrar.');
    }
  };

  const empezarEdicion = (r: Raqueta) => {
    setEditId(r.id);
    setEditForm({ marca: r.marca, tension: r.tension ?? '', marcaEncordado: r.marcaEncordado ?? '' });
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
          <input placeholder="Marca (ej: Wilson Blade 98)" value={form.marca} onChange={(e) => setForm((f) => ({ ...f, marca: e.target.value }))} maxLength={80} />
          <input placeholder="Tensión (ej: 24 kg)" value={form.tension} onChange={(e) => setForm((f) => ({ ...f, tension: e.target.value }))} maxLength={40} />
          <input placeholder="Marca del encordado" value={form.marcaEncordado} onChange={(e) => setForm((f) => ({ ...f, marcaEncordado: e.target.value }))} maxLength={80} />
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
            <input placeholder="Tensión" value={editForm.tension} onChange={(e) => setEditForm((f) => ({ ...f, tension: e.target.value }))} maxLength={40} />
            <input placeholder="Marca del encordado" value={editForm.marcaEncordado} onChange={(e) => setEditForm((f) => ({ ...f, marcaEncordado: e.target.value }))} maxLength={80} />
            <div className={s.raquetaAcciones}>
              <button className={s.btnEditar} onClick={() => setEditId(null)}>Cancelar</button>
              <button className={s.btnGuardar} disabled={editForm.marca.trim() === ''} onClick={() => void guardarEdicion(r.id)}>Guardar</button>
            </div>
          </div>
        ) : (
          <div key={r.id} className={s.raquetaFila}>
            <div className={s.raquetaInfo}>
              <div className={s.raquetaMarca}>{r.marca}</div>
              <div className={s.raquetaDetalle}>
                {[r.tension, r.marcaEncordado].filter(Boolean).join(' · ') || 'Sin datos del encordado'}
              </div>
            </div>
            <button className={s.btnMiniPortal} onClick={() => empezarEdicion(r)}>Editar</button>
            <button className={s.btnMiniPortal} onClick={() => void borrar(r)}>Borrar</button>
          </div>
        )
      ))}
    </div>
  );
}
