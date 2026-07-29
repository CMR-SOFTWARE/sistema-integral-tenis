import { useState } from 'react';
import Modal from '../../components/Modal';
import s from './NuevoAlumnoModal.module.css';

interface Props {
  nombre: string;
  usuario: string | null;
  passwordTemporal: string | null;
  /** El celular ya era de una cuenta: la ficha se vinculó a ESE login (sin credenciales nuevas). */
  vinculado: boolean;
  titular: string | null;
  onClose: () => void;
}

/**
 * Resultado de "Crear acceso":
 * - Cuenta NUEVA: entra con su número de celular como usuario y como contraseña.
 * - VINCULADO: el celular ya era de una cuenta (la misma persona que es staff, un
 *   hermano, el tutor…) → la ficha quedó bajo ESE login; no hay credenciales nuevas.
 */
export default function AccesoCreadoModal({ nombre, usuario, passwordTemporal, vinculado, titular, onClose }: Props) {
  const [copiado, setCopiado] = useState(false);

  const copiar = async () => {
    await navigator.clipboard.writeText(
      `Acceso a Sistema Integral Deportivo\nUsuario: ${usuario} (tu celular)\nContraseña: ${passwordTemporal} (tu celular)\nDespués podés cambiarla desde tu perfil.`,
    );
    setCopiado(true);
    setTimeout(() => setCopiado(false), 2000);
  };

  // Se vinculó a una cuenta existente: no hay clave para mostrar, solo el aviso.
  if (vinculado) {
    return (
      <Modal
        titulo={`Acceso vinculado para ${nombre}`}
        subtitulo="Ya tenía una cuenta en la plataforma: se usó esa."
        onClose={onClose}
        footer={<button className={s.btnPrimario} onClick={onClose}>Entendido</button>}
      >
        <p className={s.credAviso}>
          El celular ya era de la cuenta de <strong>{titular}</strong>. La ficha quedó
          vinculada a ESE login (no se creó uno nuevo): entra con su celular de siempre.
        </p>
      </Modal>
    );
  }

  return (
    <Modal
      titulo={`Acceso creado para ${nombre}`}
      subtitulo="Entra con su celular como usuario y como contraseña inicial."
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
          <span className={s.credLabel}>Usuario (su celular)</span>
          <code className={s.credValor}>{usuario}</code>
        </div>
        <div className={s.credFila}>
          <span className={s.credLabel}>Contraseña (su celular)</span>
          <code className={s.credValor}>{passwordTemporal}</code>
        </div>
      </div>
      <p className={s.credAviso}>
        Puede cambiarla cuando quiera desde su perfil.
      </p>
    </Modal>
  );
}
