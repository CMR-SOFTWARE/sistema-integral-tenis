import { useEffect, useState } from 'react';
import Modal from '../../components/Modal';
import { api, ApiError } from '../../lib/api';
import { formatoPlata } from '../alumnos/types';
import type { DatosPago } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  titulo: string;
  monto: number;
  /** Dispara el POST de informar (mes o cargo). */
  onConfirmar: () => Promise<void>;
  onClose: () => void;
}

/**
 * El alumno avisa que YA transfirió: primero ve a dónde mandar la plata
 * (alias/CBU del club) y después confirma el aviso. No mueve plata: queda
 * pendiente de que el profe confirme.
 */
export default function InformarPagoModal({ titulo, monto, onConfirmar, onClose }: Props) {
  const [datos, setDatos] = useState<DatosPago | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [copiado, setCopiado] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<DatosPago>('/portal/datos-pago')
      .then(setDatos)
      .catch(() => setDatos(null)); // sin datos: igual puede informar
  }, []);

  const copiar = async () => {
    if (!datos?.aliasCbu) return;
    try {
      await navigator.clipboard.writeText(datos.aliasCbu);
      setCopiado(true);
      setTimeout(() => setCopiado(false), 1800);
    } catch {
      /* algunos navegadores bloquean el portapapeles: no pasa nada */
    }
  };

  const confirmar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onConfirmar();
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo enviar el aviso.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo={titulo}
      subtitulo={`Transferí ${formatoPlata(monto)} y avisale a tu profe`}
      onClose={onClose}
      ancho={440}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={() => void confirmar()} disabled={enviando}>
            {enviando ? 'Enviando…' : 'Ya lo transferí'}
          </button>
        </>
      }
    >
      {datos?.aliasCbu ? (
        <div className={s.credenciales}>
          <div className={s.credFila}>
            <span className={s.credLabel}>Alias / CBU</span>
            <span className={s.credValor}>{datos.aliasCbu}</span>
            <button type="button" className={s.btnSecundario} onClick={() => void copiar()}>
              {copiado ? '¡Copiado!' : 'Copiar'}
            </button>
          </div>
          {datos.titular && (
            <div className={s.credFila}>
              <span className={s.credLabel}>Titular</span>
              <span className={s.credValor}>{datos.titular}</span>
            </div>
          )}
        </div>
      ) : (
        <p className={s.credAviso}>
          Tu profe todavía no cargó sus datos de transferencia. Coordiná con él a dónde
          mandar la plata y después avisá acá.
        </p>
      )}

      <p className={s.credAviso}>
        Cuando confirmes, tu cuota queda <b>“esperando confirmación”</b> hasta que tu profe
        verifique que le llegó.
      </p>
      {error && <div className={s.error}>{error}</div>}
    </Modal>
  );
}
