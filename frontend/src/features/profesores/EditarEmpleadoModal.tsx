import { useEffect, useState } from 'react';
import Modal from '../../components/Modal';
import { api, ApiError } from '../../lib/api';
import type { Staff, UpdateEmpleado } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  empleado: Staff;
  onClose: () => void;
  onEditar: (id: string, dto: UpdateEmpleado) => Promise<unknown>;
}

type SedeMin = { id: string; nombre: string; activo: boolean };

/** El dueño corrige la ficha del profe. El celular (login) no se toca acá. */
export default function EditarEmpleadoModal({ empleado, onClose, onEditar }: Props) {
  const [form, setForm] = useState({
    nombre: empleado.nombre,
    apellido: empleado.apellido,
    email: empleado.email ?? '',
    dni: empleado.dni ?? '',
    fechaNacimiento: empleado.fechaNacimiento?.slice(0, 10) ?? '',
    valorHora: empleado.valorHora?.toString() ?? '',
    sedeId: empleado.sedeId ?? '',
  });
  const set = (campo: keyof typeof form, valor: string) => setForm((f) => ({ ...f, [campo]: valor }));
  const [sedes, setSedes] = useState<SedeMin[]>([]);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void api.get<SedeMin[]>('/sedes').then((xs) => setSedes(xs.filter((x) => x.activo)));
  }, []);

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onEditar(empleado.id, {
        nombre: form.nombre.trim(),
        apellido: form.apellido.trim(),
        email: form.email.trim() || undefined,
        dni: form.dni.trim() || undefined,
        fechaNacimiento: form.fechaNacimiento || undefined,
        valorHora: form.valorHora.trim() === '' ? undefined : Number(form.valorHora),
        sedeId: form.sedeId || undefined,
      });
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudieron guardar los cambios.');
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo={`Editar a ${empleado.nombre} ${empleado.apellido}`}
      subtitulo="El celular es el usuario de login del profe y no se cambia desde acá."
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button
            className={s.btnPrimario}
            onClick={() => void guardar()}
            disabled={enviando || !form.nombre.trim() || !form.apellido.trim()}
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
          <input value={empleado.telefono} disabled readOnly />
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
        {/* El director no lleva valor hora (no se paga sueldo a sí mismo) ni club
            fijo (trabaja en todos): esos campos son solo para los empleados. */}
        {!empleado.esDueño && (
          <>
            <label className={s.campo}>
              <span>Valor hora (opcional)</span>
              <input
                type="number"
                min={0}
                value={form.valorHora}
                onChange={(e) => set('valorHora', e.target.value)}
                onWheel={(e) => e.currentTarget.blur()}
                placeholder="Ej: 8000"
              />
            </label>
            <label className={s.campo}>
              <span>Club (sede)</span>
              <select value={form.sedeId} onChange={(e) => set('sedeId', e.target.value)}>
                <option value="">Sin asignar</option>
                {sedes.map((x) => <option key={x.id} value={x.id}>{x.nombre}</option>)}
              </select>
            </label>
          </>
        )}
        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
