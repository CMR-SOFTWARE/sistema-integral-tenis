import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import type { TipoCargo } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';

interface Props {
  alumno: { alumnoId: string; nombre: string; apellido: string };
  onClose: () => void;
  onCrear: (dto: { alumnoId: string; tipo: TipoCargo; concepto: string; monto: number }) => Promise<void>;
}

/** Cargo manual: Producto (encordado, pelotas) o Ajuste (+/- con motivo). */
export default function NuevoCargoModal({ alumno, onClose, onCrear }: Props) {
  const [tipo, setTipo] = useState<'Producto' | 'Ajuste'>('Producto');
  const [concepto, setConcepto] = useState('');
  const [monto, setMonto] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const valido = concepto.trim() !== '' && monto !== '' && Number(monto) !== 0;

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      await onCrear({
        alumnoId: alumno.alumnoId,
        tipo,
        concepto: concepto.trim(),
        monto: Number(monto),
      });
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo agregar el cargo.');
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Modal
      titulo="Agregar cargo"
      subtitulo={`A la cuenta de ${alumno.nombre} ${alumno.apellido} — entra en la liquidación del mes`}
      onClose={onClose}
      ancho={480}
      footer={
        <>
          <button className={s.btnSecundario} onClick={onClose}>Cancelar</button>
          <button className={s.btnPrimario} onClick={() => void guardar()} disabled={enviando || !valido}>
            {enviando ? 'Agregando…' : 'Agregar cargo'}
          </button>
        </>
      }
    >
      <div className={s.grid}>
        <label className={s.campo}>
          <span>Tipo</span>
          <select value={tipo} onChange={(e) => setTipo(e.target.value as 'Producto' | 'Ajuste')}>
            <option value="Producto">Producto / Servicio</option>
            <option value="Ajuste">Ajuste (+/-)</option>
          </select>
        </label>
        <label className={s.campo}>
          <span>Monto {tipo === 'Ajuste' ? '(negativo descuenta)' : ''}</span>
          <input
            type="number"
            value={monto}
            onChange={(e) => setMonto(e.target.value)}
            placeholder={tipo === 'Ajuste' ? '-3000' : '12000'}
          />
        </label>
        <label className={`${s.campo} ${s.span2}`}>
          <span>Concepto</span>
          <input
            value={concepto}
            onChange={(e) => setConcepto(e.target.value)}
            placeholder={tipo === 'Producto' ? 'Encordado Wilson NXT' : 'Descuento hermanos'}
          />
        </label>
        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
