import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import { CATEGORIAS, CAT_LABEL } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import type { CreateGrupo } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  onClose: () => void;
  onCrear: (dto: CreateGrupo) => Promise<unknown>;
}

/** Alta de grupo fijo: nombre, categoría sugerida (opcional) y cupo (opcional). */
export default function NuevoGrupoModal({ onClose, onCrear }: Props) {
  const [nombre, setNombre] = useState('');
  const [categoria, setCategoria] = useState<'' | Categoria>('');
  const [cupo, setCupo] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onCrear({
        nombre: nombre.trim(),
        categoria: categoria === '' ? undefined : categoria,
        cupoMaximo: cupo === '' ? undefined : Number(cupo),
      });
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear el grupo.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Nuevo grupo"
      subtitulo='Grupo fijo que se repite, ej: "Intermedios martes 18hs"'
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={guardar} disabled={enviando || nombre.trim() === ''}>
            {enviando ? 'Creando…' : 'Crear grupo'}
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
