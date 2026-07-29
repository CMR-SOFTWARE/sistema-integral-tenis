import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import CategoriaOptions from './CategoriaOptions';
import { subPorEdad } from './types';
import type { Alumno, Categoria, Modalidad, RelacionTutor, UpdateAlumno } from './types';
import { useProfesores } from '../profesores/useProfesores';
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
    dni: alumno.dni ?? '',
    telefono: alumno.telefono,
    email: alumno.email ?? '',
    fechaNacimiento: alumno.fechaNacimiento?.slice(0, 10) ?? '',
    esMenor: alumno.esMenor,
    categoria: alumno.categoria,
    modalidad: alumno.modalidad ?? 'Mensual',
    arancel: alumno.arancel?.toString() ?? '',
    profesorId: alumno.profesorUserId ?? '',
    notas: alumno.notas ?? '',
    tutorNombre: '', tutorApellido: '', tutorDni: '', tutorTelefono: '',
    tutorRelacion: 'Madre' as RelacionTutor,
  });
  const set = (campo: keyof typeof form, valor: string | boolean) =>
    setForm((f) => ({ ...f, [campo]: valor }));
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { profes } = useProfesores();

  const esMenor = form.esMenor;

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onEditar(alumno.id, {
        nombre: form.nombre.trim(),
        apellido: form.apellido.trim(),
        dni: form.dni.trim() || undefined,
        telefono: form.telefono.trim(),
        email: form.email.trim() || undefined,
        fechaNacimiento: form.fechaNacimiento || undefined,
        esMenor: form.esMenor,
        categoria: form.categoria as Categoria,
        modalidad: form.modalidad as Modalidad,
        arancel: form.arancel.trim() === '' ? undefined : Number(form.arancel),
        profesorUserId: form.profesorId || undefined,
        notas: form.notas.trim() || undefined,
        // Tutor: solo si es menor, todavía no tiene uno, y cargaste al menos el nombre.
        tutor: form.esMenor && !alumno.tutorId && form.tutorNombre.trim()
          ? {
              nombre: form.tutorNombre.trim(),
              apellido: form.tutorApellido.trim(),
              dni: form.tutorDni.trim(),
              telefono: form.tutorTelefono.trim(),
              relacion: form.tutorRelacion,
            }
          : undefined,
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
      subtitulo="El acceso al portal (celular de login y contraseña) no se cambia desde acá."
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button
            className={s.btnPrimario}
            onClick={guardar}
            disabled={enviando || !form.nombre.trim() || !form.apellido.trim() || !form.telefono.trim()}
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
          <span>Celular (usuario de login)</span>
          <input value={form.telefono} onChange={(e) => set('telefono', e.target.value)} maxLength={25} />
        </label>
        <label className={s.campo}>
          <span>DNI (opcional)</span>
          <input value={form.dni} onChange={(e) => set('dni', e.target.value)} maxLength={15} />
        </label>
        <label className={`${s.campo} ${s.span2}`}>
          <span>Email de contacto (opcional)</span>
          <input type="email" value={form.email} onChange={(e) => set('email', e.target.value)} />
        </label>
        <label className={s.campo}>
          <span>Fecha de nacimiento (opcional)</span>
          <input type="date" value={form.fechaNacimiento} onChange={(e) => set('fechaNacimiento', e.target.value)} />
        </label>
        <label className={s.campo}>
          <span>Categoría por edad (automática)</span>
          <input value={subPorEdad(form.fechaNacimiento || null) ?? 'Adulto'} disabled readOnly />
        </label>
        <label className={s.campo}>
          <span>&nbsp;</span>
          <label className={s.check}>
            <input type="checkbox" checked={form.esMenor} onChange={(e) => set('esMenor', e.target.checked)} />
            Es menor de edad
          </label>
        </label>
        <label className={s.campo}>
          <span>Categoría</span>
          <select value={form.categoria} onChange={(e) => set('categoria', e.target.value)}>
            <option value="SinCategoria">Sin categoría</option>
            <CategoriaOptions />
          </select>
        </label>
        <label className={s.campo}>
          <span>Cuota mensual (opcional)</span>
          <input
            type="number"
            min={0}
            value={form.arancel}
            onChange={(e) => set('arancel', e.target.value)}
            onWheel={(e) => e.currentTarget.blur()}
            placeholder="Ej: 30000"
          />
        </label>
        <label className={s.campo}>
          <span>Modalidad de pago</span>
          <select value={form.modalidad} onChange={(e) => set('modalidad', e.target.value)}>
            <option value="Mensual">Mensual (vence el 10)</option>
            <option value="PorClase">Por clase</option>
          </select>
        </label>
        <label className={s.campo}>
          <span>Profe de cabecera</span>
          <select value={form.profesorId} onChange={(e) => set('profesorId', e.target.value)}>
            <option value="">Sin asignar</option>
            {profes.map((p) => (
              <option key={p.userId} value={p.userId}>{p.nombre}{p.esDueño ? ' (vos)' : ''}</option>
            ))}
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

        {esMenor && alumno.tutorId && (
          <div className={s.span2} style={{ color: '#0e6b3c', fontSize: 13, fontWeight: 600 }}>
            ✓ Ya tiene un tutor cargado.
          </div>
        )}
        {esMenor && !alumno.tutorId && (
          <div className={`${s.span2} ${s.bloqueTutor}`}>
            <div style={{ fontWeight: 700, fontSize: 13, marginBottom: 8 }}>
              Datos del tutor (opcional — podés cargarlo ahora o después)
            </div>
            <div className={s.grid}>
              <label className={s.campo}>
                <span>Nombre del tutor</span>
                <input value={form.tutorNombre} onChange={(e) => set('tutorNombre', e.target.value)} maxLength={80} />
              </label>
              <label className={s.campo}>
                <span>Apellido del tutor</span>
                <input value={form.tutorApellido} onChange={(e) => set('tutorApellido', e.target.value)} maxLength={80} />
              </label>
              <label className={s.campo}>
                <span>DNI del tutor</span>
                <input value={form.tutorDni} onChange={(e) => set('tutorDni', e.target.value)} maxLength={15} />
              </label>
              <label className={s.campo}>
                <span>Teléfono del tutor</span>
                <input value={form.tutorTelefono} onChange={(e) => set('tutorTelefono', e.target.value)} maxLength={25} />
              </label>
              <label className={s.campo}>
                <span>Relación</span>
                <select value={form.tutorRelacion} onChange={(e) => set('tutorRelacion', e.target.value)}>
                  <option value="Padre">Padre</option>
                  <option value="Madre">Madre</option>
                  <option value="TutorLegal">Tutor legal</option>
                  <option value="Otro">Otro</option>
                </select>
              </label>
            </div>
          </div>
        )}
        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
