import { useState } from 'react';
import Modal from '../../components/Modal';
import s from './NuevoAlumnoModal.module.css';

interface Props {
  nombre: string;
  usuario: string;
  passwordTemporal: string;
  onClose: () => void;
}

/**
 * Credenciales del alumno recién creado: entra con su número de celular como
 * usuario y también como contraseña (después puede cambiarla desde su perfil).
 */
export default function AccesoCreadoModal({ nombre, usuario, passwordTemporal, onClose }: Props) {
  const [copiado, setCopiado] = useState(false);

  const copiar = async () => {
    await navigator.clipboard.writeText(
      `Acceso a Sistema Integral Deportivo\nUsuario: ${usuario} (tu celular)\nContraseña: ${passwordTemporal} (tu celular)\nDespués podés cambiarla desde tu perfil.`,
    );
    setCopiado(true);
    setTimeout(() => setCopiado(false), 2000);
  };

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
