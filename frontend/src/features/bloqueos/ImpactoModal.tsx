import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import { fechaCorta, horaCorta } from '../agenda/types';
import type { Impacto } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';
import b from './BloqueosPage.module.css';

interface Props {
  impacto: Impacto;
  onClose: () => void;
  onConfirmar: () => Promise<unknown>;
}

/** Confirmación del bloqueo: qué turnos cancela y a quién avisar (WhatsApp). */
export default function ImpactoModal({ impacto, onClose, onConfirmar }: Props) {
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const confirmar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onConfirmar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo crear el bloqueo.');
      setEnviando(false);
    }
  };

  const textoAviso = (fecha: string, hora: string) =>
    encodeURIComponent(
      `Hola! Te aviso que la clase del ${fechaCorta(fecha)} a las ${horaCorta(hora)} queda cancelada. Coordinamos recuperación. ¡Gracias!`,
    );

  return (
    <Modal
      titulo="Impacto del bloqueo"
      subtitulo={
        impacto.turnosAfectados === 0
          ? 'No pisa ningún turno ya generado.'
          : `Cancela ${impacto.turnosAfectados} turno${impacto.turnosAfectados > 1 ? 's' : ''} ya generado${impacto.turnosAfectados > 1 ? 's' : ''}.`
      }
      onClose={onClose}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Volver</button>
          <button className={s.btnPrimario} onClick={confirmar} disabled={enviando}>
            {enviando ? 'Creando…' : 'Confirmar bloqueo'}
          </button>
        </>
      }
    >
      {impacto.turnosAfectados === 0 ? (
        <div className={b.impactoVacio}>
          Los horarios futuros de esta franja directamente no se van a generar.
        </div>
      ) : (
        <>
          <div className={b.impactoAviso}>
            Los alumnos <b>no pagan</b> estas clases (cancelación del profe).
            Avisales por WhatsApp:
          </div>
          <div className={b.impactoLista}>
            {impacto.afectados.map((a, i) => (
              <div key={i} className={b.impactoFila}>
                <div>
                  <div className={b.impactoNombre}>{a.alumnoNombre}</div>
                  <div className={b.impactoDetalle}>
                    {a.titulo} · {fechaCorta(a.fecha)} {horaCorta(a.horaInicio)}
                  </div>
                </div>
                {a.telefono && (
                  <a
                    className={b.btnWhatsapp}
                    href={`https://wa.me/${a.telefono.replace(/\D/g, '')}?text=${textoAviso(a.fecha, a.horaInicio)}`}
                    target="_blank"
                    rel="noreferrer"
                  >
                    WhatsApp
                  </a>
                )}
              </div>
            ))}
          </div>
        </>
      )}
      {error && <div className={s.error}>{error}</div>}
    </Modal>
  );
}
