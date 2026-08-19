import { useState } from 'react';
import Modal from '../../components/Modal';
import { ApiError } from '../../lib/api';
import SelectorProductos from './SelectorProductos';
import type { LineaElegida } from './SelectorProductos';
import type { TipoCargo } from './types';
import s from '../alumnos/NuevoAlumnoModal.module.css';

/**
 * De dónde sale lo que se le suma a la cuenta:
 *  - `Catalogo`: se elige del catálogo del profe. El cargo queda con su **desglose**
 *    (qué productos, cuántos, a qué precio) y el alumno lo ve en sus pedidos.
 *  - `Producto`: algo que no está en el catálogo, escrito a mano.
 *  - `Ajuste`: un +/- con motivo (descuento de hermanos, una corrección).
 */
type Origen = 'Catalogo' | 'Producto' | 'Ajuste';

interface Props {
  alumno: { alumnoId: string; nombre: string; apellido: string };
  onClose: () => void;
  onCrear: (dto: { alumnoId: string; tipo: TipoCargo; concepto: string; monto: number }) => Promise<void>;
  /** Del catálogo: nace el pedido ya aceptado, con su cargo y su desglose. */
  onCargarProductos: (alumnoId: string, lineas: LineaElegida[]) => Promise<void>;
}

/** Sumarle algo a la cuenta del alumno: del catálogo, a mano, o un ajuste. */
export default function NuevoCargoModal({ alumno, onClose, onCrear, onCargarProductos }: Props) {
  const [origen, setOrigen] = useState<Origen>('Catalogo');
  const [concepto, setConcepto] = useState('');
  const [monto, setMonto] = useState('');
  const [lineas, setLineas] = useState<LineaElegida[]>([]);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const delCatalogo = origen === 'Catalogo';
  const valido = delCatalogo
    ? lineas.length > 0
    : concepto.trim() !== '' && monto !== '' && Number(monto) !== 0;

  const guardar = async () => {
    setError(null);
    setEnviando(true);
    try {
      if (delCatalogo) {
        await onCargarProductos(alumno.alumnoId, lineas);
      } else {
        await onCrear({
          alumnoId: alumno.alumnoId,
          tipo: origen,
          concepto: concepto.trim(),
          monto: Number(monto),
        });
      }
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
        <label className={`${s.campo} ${delCatalogo ? s.span2 : ''}`}>
          <span>Qué le cargo</span>
          <select value={origen} onChange={(e) => setOrigen(e.target.value as Origen)}>
            <option value="Catalogo">Del catálogo (productos y servicios)</option>
            <option value="Producto">Otro concepto (a mano)</option>
            <option value="Ajuste">Ajuste (+/-)</option>
          </select>
        </label>

        {!delCatalogo && (
          <>
            <label className={s.campo}>
              <span>Monto {origen === 'Ajuste' ? '(negativo descuenta)' : ''}</span>
              <input
                type="number"
                value={monto}
                onChange={(e) => setMonto(e.target.value)}
                placeholder={origen === 'Ajuste' ? '-3000' : '12000'}
              />
            </label>
            <label className={`${s.campo} ${s.span2}`}>
              <span>Concepto</span>
              <input
                value={concepto}
                onChange={(e) => setConcepto(e.target.value)}
                placeholder={origen === 'Producto' ? 'Encordado Wilson NXT' : 'Descuento hermanos'}
              />
            </label>
          </>
        )}

        {delCatalogo && (
          <div className={s.span2}>
            <SelectorProductos onCambio={(l) => setLineas(l)} />
          </div>
        )}

        {error && <div className={`${s.span2} ${s.error}`}>{error}</div>}
      </div>
    </Modal>
  );
}
