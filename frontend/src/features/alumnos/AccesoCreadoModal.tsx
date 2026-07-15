import { useState } from 'react';
import Modal from '../../components/Modal';
import s from './NuevoAlumnoModal.module.css';

interface Props {
  nombre: string;
  email: string;
  passwordTemporal: string;
  onClose: () => void;
}

/**
 * Credenciales del alumno recién creado: entra con su email y su número de
 * teléfono como contraseña (después puede cambiarla desde su perfil).
 */
export default function AccesoCreadoModal({ nombre, email, passwordTemporal, onClose }: Props) {
  const [copiado, setCopiado] = useState(false);

  const copiar = async () => {
    await navigator.clipboard.writeText(
      `Acceso a CourtSet\nEmail: ${email}\nContraseña: ${passwordTemporal} (tu número de teléfono)\nDespués podés cambiarla desde tu perfil.`,
    );
    setCopiado(true);
    setTimeout(() => setCopiado(false), 2000);
  };

  return (
    <Modal
      titulo={`Acceso creado para ${nombre}`}
      subtitulo="Su contraseña inicial es su número de teléfono (solo dígitos)."
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cerrar</button>
          <button className={s.btnPrimario} onClick={() => void copiar()}>
            {copiado ? '¡Copiado!' : 'Copiar credenciales'}
          </button>
        </>
      }
    >
      <div className={s.credenciales}>
        <div className={s.credFila}>
          <span className={s.credLabel}>Email</span>
          <code className={s.credValor}>{email}</code>
        </div>
        <div className={s.credFila}>
          <span className={s.credLabel}>Contraseña (su teléfono)</span>
          <code className={s.credValor}>{passwordTemporal}</code>
        </div>
      </div>
      <p className={s.credAviso}>
        El alumno puede cambiarla cuando quiera desde su perfil en el portal.
      </p>
    </Modal>
  );
}
