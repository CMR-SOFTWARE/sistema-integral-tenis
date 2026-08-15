import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import s from '../alumnos/NuevoAlumnoModal.module.css';

export interface AltaClub {
  nombreClub: string;
  nombre: string;
  apellido: string;
  telefono: string;
  email?: string;
}

export interface ClubCreado {
  club: { profesor: string };
  usuario: string;
  passwordTemporal: string;
}

interface Props {
  onClose: () => void;
  onCrear: (dto: AltaClub) => Promise<ClubCreado>;
  onCreado: (creado: ClubCreado) => void;
}

/**
 * Alta de una academia desde Plataforma (Bloque 6, pedido 10): el admin le crea la
 * cuenta al director (como a un empleado) y el club nace ya ACTIVO, sin pasar por el
 * checkout de Mercado Pago.
 */
export default function NuevaAcademiaModal({ onClose, onCrear, onCreado }: Props) {
  const [form, setForm] = useState({ nombreClub: '', nombre: '', apellido: '', telefono: '', email: '' });
  const set = (campo: keyof typeof form, valor: string) => setForm((f) => ({ ...f, [campo]: valor }));
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const completo = form.nombreClub.trim() && form.nombre.trim() && form.apellido.trim() && form.telefono.trim();

  const guardar = async () => {
    if (!completo) return;
    setError(null);
    setEnviando(true);
    try {
      const creado = await onCrear({
        nombreClub: form.nombreClub.trim(),
        nombre: form.nombre.trim(),
        apellido: form.apellido.trim(),
        telefono: form.telefono.trim(),
        email: form.email.trim() || undefined,
      });
      onCreado(creado);
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear la academia.');
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Nueva academia"
      subtitulo="Nace ACTIVA directo, sin pasar por el checkout: le creás la cuenta al director (su celular es el usuario y la contraseña inicial)."
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={() => void guardar()} disabled={enviando || !completo}>
            {enviando ? 'Creando…' : 'Crear academia'}
          </button>
        </>
      }
    >
      <div className={s.grid}>
        <label className={`${s.campo} ${s.span2}`}>
          <span>Nombre del club o academia</span>
          <input value={form.nombreClub} onChange={(e) => set('nombreClub', e.target.value)} maxLength={80} />
        </label>
        <label className={s.campo}>
          <span>Nombre del director</span>
          <input value={form.nombre} onChange={(e) => set('nombre', e.target.value)} maxLength={80} />
        </label>
        <label className={s.campo}>
          <span>Apellido del director</span>
          <input value={form.apellido} onChange={(e) => set('apellido', e.target.value)} maxLength={80} />
        </label>
        <label className={s.campo}>
          <span>Celular (usuario y contraseña inicial)</span>
          <input value={form.telefono} onChange={(e) => set('telefono', e.target.value)} maxLength={25} />
        </label>
        <label className={s.campo}>
          <span>Email (opcional)</span>
          <input type="email" value={form.email} onChange={(e) => set('email', e.target.value)} />
        </label>
        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
