import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import { CATEGORIAS, CAT_LABEL } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import type { Grupo, UpdateGrupo } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  grupo: Grupo;
  onClose: () => void;
  onEditar: (id: string, dto: UpdateGrupo) => Promise<unknown>;
}

/** Edición de un grupo: nombre, categoría y cupo (el profe se cambia desde la tarjeta). */
export default function EditarGrupoModal({ grupo, onClose, onEditar }: Props) {
  const [nombre, setNombre] = useState(grupo.nombre);
  const [categoria, setCategoria] = useState<'' | Categoria>(grupo.categoria ?? '');
  const [cupo, setCupo] = useState(grupo.cupoMaximo?.toString() ?? '');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onEditar(grupo.id, {
        nombre: nombre.trim(),
        categoria: categoria === '' ? undefined : categoria,
        cupoMaximo: cupo === '' ? undefined : Number(cupo),
      });
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo guardar el grupo.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Editar grupo"
      subtitulo={grupo.nombre}
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={guardar} disabled={enviando || nombre.trim() === ''}>
            {enviando ? 'Guardando…' : 'Guardar cambios'}
          </button>
        </>
      }
    >
      <div className={s.grid}>
        <label className={`${s.campo} ${s.span2}`}>
          <span>Nombre</span>
          <input value={nombre} onChange={(e) => setNombre(e.target.value)} placeholder="Intermedios martes" maxLength={80} />
        </label>
        <label className={s.campo}>
          <span>Categoría sugerida (opcional)</span>
          <select value={categoria} onChange={(e) => setCategoria(e.target.value as '' | Categoria)}>
            <option value="">Sin categoría</option>
            {CATEGORIAS.filter((c) => c !== 'SinCategoria').map((c) => (
              <option key={c} value={c}>{CAT_LABEL[c]}</option>
            ))}
          </select>
        </label>
        <label className={s.campo}>
          <span>Cupo máximo (vacío = sin límite)</span>
          <input type="number" min={1} value={cupo} onChange={(e) => setCupo(e.target.value)} placeholder="4" />
        </label>
        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
