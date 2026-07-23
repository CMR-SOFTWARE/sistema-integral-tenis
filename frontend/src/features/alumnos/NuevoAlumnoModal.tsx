import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import { CATEGORIAS, CAT_LABEL } from './types';
import type { AlumnoCreado, Categoria, CreateAlumno, RelacionTutor } from './types';
import { useProfesores } from '../profesores/useProfesores';
import s from './NuevoAlumnoModal.module.css';

interface Props {
  onClose: () => void;
  onCrear: (dto: CreateAlumno) => Promise<AlumnoCreado>;
  /** El alta devuelve las credenciales: el padre muestra el modal de éxito. */
  onCreado: (creado: AlumnoCreado) => void;
}

/**
 * Alta de alumno CON acceso al portal (plan v2: registro único — email
 * obligatorio, el sistema genera la contraseña temporal). Cuando la fecha
 * de nacimiento da menor de 18, aparece el bloque de tutor + consentimiento.
 */
export default function NuevoAlumnoModal({ onClose, onCrear, onCreado }: Props) {
  const [form, setForm] = useState({
    nombre: '', apellido: '', dni: '', telefono: '', email: '',
    fechaNacimiento: '', esMenor: false, categoria: 'SinCategoria' as Categoria,
    profesorId: '',
    notas: '',
    consentimientoWhatsapp: false, consentimientoDatos: false,
    tutorNombre: '', tutorApellido: '', tutorDni: '', tutorTelefono: '',
    tutorRelacion: 'Madre' as RelacionTutor,
  });
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { profes } = useProfesores();

  // Ahora lo marca el profe con un checkbox (la fecha es opcional)
  const esMenor = form.esMenor;

  const set = (campo: string, valor: string | boolean) =>
    setForm((f) => ({ ...f, [campo]: valor }));

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      const dto: CreateAlumno = {
        nombre: form.nombre.trim(),
        apellido: form.apellido.trim(),
        telefono: form.telefono.trim(),
        dni: form.dni.trim() || undefined,
        email: form.email.trim() || undefined,
        fechaNacimiento: form.fechaNacimiento || undefined,
        esMenor: form.esMenor,
        categoria: form.categoria,
        profesorUserId: form.profesorId || undefined,
        // Arancel ya no se carga acá: el monto real sale de los cargos (ADR-0009)
        notas: form.notas.trim() || undefined,
        consentimientoWhatsapp: form.consentimientoWhatsapp,
        consentimientoDatos: form.consentimientoDatos,
        tutor: esMenor
          ? {
              nombre: form.tutorNombre.trim(),
              apellido: form.tutorApellido.trim(),
              dni: form.tutorDni.trim(),
              telefono: form.tutorTelefono.trim(),
              relacion: form.tutorRelacion,
            }
          : undefined,
      };
      const creado = await onCrear(dto);
      onCreado(creado); // el padre muestra las credenciales (una sola vez)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear el alumno.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Nuevo alumno"
      subtitulo="Los datos del alumno quedan asociados a tu academia"
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button
            className={s.btnPrimario}
            onClick={guardar}
            disabled={enviando || !form.nombre.trim() || !form.apellido.trim() || !form.telefono.trim()}
          >
            {enviando ? 'Creando…' : 'Crear alumno'}
          </button>
        </>
      }
    >
      <div className={s.grid}>
        <label className={s.campo}>
          <span>Nombre</span>
          <input value={form.nombre} onChange={(e) => set('nombre', e.target.value)} placeholder="Juan" maxLength={80} />
        </label>
        <label className={s.campo}>
          <span>Apellido</span>
          <input value={form.apellido} onChange={(e) => set('apellido', e.target.value)} placeholder="Pérez" maxLength={80} />
        </label>
        <label className={s.campo}>
          <span>Celular (su usuario y contraseña)</span>
          <input value={form.telefono} onChange={(e) => set('telefono', e.target.value)} placeholder="1155551234" maxLength={25} />
        </label>
        <label className={s.campo}>
          <span>DNI (opcional)</span>
          <input value={form.dni} onChange={(e) => set('dni', e.target.value)} placeholder="35123456" maxLength={15} />
        </label>
        <label className={`${s.campo} ${s.span2}`}>
          <span>Email (opcional)</span>
          <input type="email" value={form.email} onChange={(e) => set('email', e.target.value)} placeholder="juan@email.com" />
        </label>
        <label className={s.campo}>
          <span>Fecha de nacimiento (opcional)</span>
          <input type="date" value={form.fechaNacimiento} onChange={(e) => set('fechaNacimiento', e.target.value)} />
        </label>
        <label className={s.campo}>
          <span>&nbsp;</span>
          <label className={s.check}>
            <input
              type="checkbox"
              checked={form.esMenor}
              onChange={(e) => set('esMenor', e.target.checked)}
            />
            Es menor de edad
          </label>
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
          <span>Profe de cabecera (opcional)</span>
          <select value={form.profesorId} onChange={(e) => set('profesorId', e.target.value)}>
            <option value="">Sin asignar</option>
            {profes.map((p) => (
              <option key={p.userId} value={p.userId}>{p.nombre}{p.esDueño ? ' (vos)' : ''}</option>
            ))}
          </select>
        </label>
        <label className={s.campo}>
          <span>&nbsp;</span>
          <label className={s.check}>
            <input
              type="checkbox"
              checked={form.consentimientoWhatsapp}
              onChange={(e) => set('consentimientoWhatsapp', e.target.checked)}
            />
            Acepta avisos por WhatsApp
          </label>
        </label>
        <label className={`${s.campo} ${s.span2}`}>
          <span>Observaciones</span>
          <textarea rows={2} value={form.notas} onChange={(e) => set('notas', e.target.value)} placeholder="Notas internas…" maxLength={500} />
        </label>

        {esMenor && (
          <div className={`${s.span2} ${s.bloqueTutor}`}>
            <div className={s.bloqueTitulo}>
              Alumno menor de edad — datos del tutor
            </div>
            <div className={s.grid}>
              <label className={s.campo}>
                <span>Nombre del tutor</span>
                <input value={form.tutorNombre} onChange={(e) => set('tutorNombre', e.target.value)} placeholder="Marta" maxLength={80} />
              </label>
              <label className={s.campo}>
                <span>Apellido</span>
                <input value={form.tutorApellido} onChange={(e) => set('tutorApellido', e.target.value)} placeholder="Gómez" maxLength={80} />
              </label>
              <label className={s.campo}>
                <span>DNI del tutor</span>
                <input value={form.tutorDni} onChange={(e) => set('tutorDni', e.target.value)} placeholder="22555666" maxLength={15} />
              </label>
              <label className={s.campo}>
                <span>Teléfono del tutor</span>
                <input value={form.tutorTelefono} onChange={(e) => set('tutorTelefono', e.target.value)} placeholder="+5491144443333" maxLength={25} />
              </label>
              <label className={s.campo}>
                <span>Relación</span>
                <select value={form.tutorRelacion} onChange={(e) => set('tutorRelacion', e.target.value)}>
                  <option value="Madre">Madre</option>
                  <option value="Padre">Padre</option>
                  <option value="TutorLegal">Tutor legal</option>
                  <option value="Otro">Otro</option>
                </select>
              </label>
              <label className={s.campo}>
                <span>&nbsp;</span>
                <label className={s.check}>
                  <input
                    type="checkbox"
                    checked={form.consentimientoDatos}
                    onChange={(e) => set('consentimientoDatos', e.target.checked)}
                  />
                  El tutor consiente el tratamiento de datos
                </label>
              </label>
            </div>
          </div>
        )}

        {!esMenor && (
          <label className={`${s.span2} ${s.check}`}>
            <input
              type="checkbox"
              checked={form.consentimientoDatos}
              onChange={(e) => set('consentimientoDatos', e.target.checked)}
            />
            El alumno consiente el tratamiento de sus datos
          </label>
        )}

        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
