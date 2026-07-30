import { useEffect, useState } from 'react';
import Modal from '../../components/Modal';
import { api, ApiError } from '../../lib/api';
import type { StaffCreado } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';

interface AltaEmpleado {
  nombre: string;
  apellido: string;
  telefono: string;
  email?: string;
  valorHora?: number;
  sedeId?: string;
}

interface Props {
  onClose: () => void;
  onCrear: (dto: AltaEmpleado) => Promise<StaffCreado>;
  onCreado: (creado: StaffCreado) => void;
}

type SedeMin = { id: string; nombre: string; activo: boolean };

/**
 * Alta de un profe empleado: el dueño le crea la cuenta (su celular es el usuario
 * y la contraseña inicial). El email, el valor hora y el club son opcionales.
 */
export default function NuevoEmpleadoModal({ onClose, onCrear, onCreado }: Props) {
  const [form, setForm] = useState({ nombre: '', apellido: '', telefono: '', email: '', valorHora: '', sedeId: '' });
  const set = (campo: keyof typeof form, valor: string) => setForm((f) => ({ ...f, [campo]: valor }));
  const [sedes, setSedes] = useState<SedeMin[]>([]);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void api.get<SedeMin[]>('/sedes').then((xs) => setSedes(xs.filter((x) => x.activo)));
  }, []);

  const completo = form.nombre.trim() && form.apellido.trim() && form.telefono.trim();

  const guardar = async () => {
    if (!completo) return;
    setError(null);
    setEnviando(true);
    try {
      const creado = await onCrear({
        nombre: form.nombre.trim(),
        apellido: form.apellido.trim(),
        telefono: form.telefono.trim(),
        email: form.email.trim() || undefined,
        valorHora: form.valorHora.trim() === '' ? undefined : Number(form.valorHora),
        sedeId: form.sedeId || undefined,
      });
      onCreado(creado);
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear el profe.');
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Nuevo profe"
      subtitulo="Le creás la cuenta: su celular es el usuario y la contraseña inicial."
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={() => void guardar()} disabled={enviando || !completo}>
            {enviando ? 'Creando…' : 'Crear profe'}
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
          <span>Celular (usuario y contraseña inicial)</span>
          <input value={form.telefono} onChange={(e) => set('telefono', e.target.value)} maxLength={25} />
        </label>
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
          <span>Club (sede, opcional)</span>
          <select value={form.sedeId} onChange={(e) => set('sedeId', e.target.value)}>
            <option value="">Sin asignar</option>
            {sedes.map((x) => <option key={x.id} value={x.id}>{x.nombre}</option>)}
          </select>
        </label>
        <label className={`${s.campo} ${s.span2}`}>
          <span>Email (opcional)</span>
          <input type="email" value={form.email} onChange={(e) => set('email', e.target.value)} />
        </label>
        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
