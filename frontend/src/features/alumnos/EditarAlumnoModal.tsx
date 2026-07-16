import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import { CATEGORIAS, CAT_LABEL, edad } from './types';
import type { Alumno, Categoria, Modalidad, UpdateAlumno } from './types';
import s from './NuevoAlumnoModal.module.css';

interface Props {
  alumno: Alumno;
  onClose: () => void;
  onEditar: (id: string, dto: UpdateAlumno) => Promise<unknown>;
}

/**
 * El profe corrige los datos de la ficha. El acceso al portal (email de
 * login y contraseña) no se toca acá: vive en la cuenta del alumno.
 */
export default function EditarAlumnoModal({ alumno, onClose, onEditar }: Props) {
  const [form, setForm] = useState({
    nombre: alumno.nombre,
    apellido: alumno.apellido,
    dni: alumno.dni,
    telefono: alumno.telefono,
    email: alumno.email ?? '',
    fechaNacimiento: alumno.fechaNacimiento.slice(0, 10),
    categoria: alumno.categoria,
    modalidad: alumno.modalidad ?? 'Mensual',
    notas: alumno.notas ?? '',
  });
  const set = (campo: keyof typeof form, valor: string) =>
    setForm((f) => ({ ...f, [campo]: valor }));
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const esMenor = form.fechaNacimiento !== '' && edad(form.fechaNacimiento) < 18;

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onEditar(alumno.id, {
        nombre: form.nombre.trim(),
        apellido: form.apellido.trim(),
        dni: form.dni.trim(),
        telefono: form.telefono.trim(),
        email: form.email.trim() || undefined,
        fechaNacimiento: form.fechaNacimiento,
        categoria: form.categoria as Categoria,
        modalidad: form.modalidad as Modalidad,
        notas: form.notas.trim() || undefined,
      });
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudieron guardar los cambios.');
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo={`Editar a ${alumno.nombre} ${alumno.apellido}`}
      subtitulo="El acceso al portal (email de login y contraseña) no se cambia desde acá."
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button
            className={s.btnPrimario}
            onClick={guardar}
            disabled={enviando || !form.nombre.trim() || !form.dni.trim() || !form.telefono.trim()}
          >
            {enviando ? 'Guardando…' : 'Guardar cambios'}
          </button>
        </>
      }
    >
      <div className={s.grid}>
        <label className={s.campo}>
          <span>Nombre</span>
          <input value={form.nombre} onChange={(e) => set('nombre', e.target.value)} maxLength={80} />
        </label>
        <label className={s.campo}>
          <span>Apellido</span>
          <input value={form.apellido} onChange={(e) => set('apellido', e.target.value)} maxLength={80} />
        </label>
        <label className={s.campo}>
          <span>DNI</span>
          <input value={form.dni} onChange={(e) => set('dni', e.target.value)} maxLength={15} />
        </label>
        <label className={s.campo}>
          <span>Teléfono</span>
          <input value={form.telefono} onChange={(e) => set('telefono', e.target.value)} maxLength={25} />
        </label>
        <label className={`${s.campo} ${s.span2}`}>
          <span>Email de contacto</span>
          <input type="email" value={form.email} onChange={(e) => set('email', e.target.value)} />
        </label>
        <label className={s.campo}>
          <span>Fecha de nacimiento</span>
          <input type="date" value={form.fechaNacimiento} onChange={(e) => set('fechaNacimiento', e.target.value)} />
        </label>
        <label className={s.campo}>
          <span>Categoría</span>
          <select value={form.categoria} onChange={(e) => set('categoria', e.target.value)}>
            {CATEGORIAS.map((c) => (
              <option key={c} value={c}>{c === 'SinCategoria' ? 'Sin categoría' : CAT_LABEL[c]}</option>
            ))}
          </select>
        </label>
        <label className={s.campo}>
          <span>Modalidad de pago</span>
          <select value={form.modalidad} onChange={(e) => set('modalidad', e.target.value)}>
            <option value="Mensual">Mensual (vence el 10)</option>
            <option value="PorClase">Por clase</option>
          </select>
        </label>
        <label className={`${s.campo} ${s.span2}`}>
          <span>Observaciones</span>
          <textarea
            rows={2}
            value={form.notas}
            onChange={(e) => set('notas', e.target.value)}
            maxLength={500}
            placeholder="Notas internas…"
          />
        </label>

        {esMenor && !alumno.tutorId && (
          <div className={`${s.span2} ${s.error}`}>
            Con esa fecha el alumno es menor y no tiene tutor cargado: el
            backend va a rechazar el cambio.
          </div>
        )}
        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
