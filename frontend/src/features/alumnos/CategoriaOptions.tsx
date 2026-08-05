import { CAT_LABEL, CATEGORIAS_DAMAS, CATEGORIAS_VARONES } from './types';

/**
 * Opciones agrupadas Varones (1ra-6ta) / Damas (A-D) para un &lt;select&gt; de
 * categoría. La opción "sin categoría" la agrega cada formulario aparte (el valor
 * difiere: 'SinCategoria' en alumnos, '' en los horarios).
 */
export default function CategoriaOptions() {
  return (
    <>
      <optgroup label="Varones">
        {CATEGORIAS_VARONES.map((c) => <option key={c} value={c}>{CAT_LABEL[c]}</option>)}
      </optgroup>
      <optgroup label="Damas">
        {CATEGORIAS_DAMAS.map((c) => <option key={c} value={c}>{CAT_LABEL[c]}</option>)}
      </optgroup>
    </>
  );
}
