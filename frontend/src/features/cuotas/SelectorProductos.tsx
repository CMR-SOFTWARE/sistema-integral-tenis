import { useEffect, useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '../../lib/api';
import { formatoPlata } from '../alumnos/types';
import type { Servicio } from './types';
import s from './SelectorProductos.module.css';

/** Una línea elegida del catálogo, lista para mandar al backend. */
export interface LineaElegida {
  servicioId: string;
  cantidad: number;
  nota?: string;
}

interface Props {
  /** Se avisa en cada cambio; el modal de arriba es el que guarda. */
  onCambio: (lineas: LineaElegida[], total: number) => void;
}

/**
 * Elegir del catálogo del profe para sumarlo a la cuenta de un alumno. Es el camino
 * que reemplaza al concepto escrito a mano: el cargo queda con su **desglose** (qué
 * productos, cuántos y a qué precio) en vez de un renglón de texto libre.
 */
export default function SelectorProductos({ onCambio }: Props) {
  const [carrito, setCarrito] = useState<Record<string, number>>({}); // servicioId → cantidad
  const [notas, setNotas] = useState<Record<string, string>>({});

  const query = useQuery({
    queryKey: ['servicios-catalogo'],
    queryFn: () => api.get<Servicio[]>('/configuracion/servicios'),
  });
  // El catálogo trae activos e inactivos (la pantalla de Productos los muestra todos);
  // acá solo se ofrece lo que sigue a la venta.
  const servicios = useMemo(() => (query.data ?? []).filter((sv) => sv.activo), [query.data]);

  const total = useMemo(
    () => servicios.reduce((acc, sv) => acc + sv.precio * (carrito[sv.id] ?? 0), 0),
    [servicios, carrito],
  );

  // El padre necesita las líneas para guardar; se las pasamos en cada cambio.
  useEffect(() => {
    const lineas = Object.entries(carrito)
      .filter(([, cantidad]) => cantidad > 0)
      .map(([servicioId, cantidad]) => ({
        servicioId,
        cantidad,
        nota: notas[servicioId]?.trim() || undefined,
      }));
    onCambio(lineas, total);
    // `onCambio` viene del padre sin memoizar: incluirlo re-dispararía en cada render.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [carrito, notas, total]);

  // Updaters funcionales: dos clicks en el mismo tick suman de a uno cada uno.
  const sumar = (id: string) => setCarrito((prev) => ({ ...prev, [id]: (prev[id] ?? 0) + 1 }));

  const restar = (id: string) =>
    setCarrito((prev) => {
      const actual = (prev[id] ?? 0) - 1;
      if (actual <= 0) {
        const { [id]: _quitado, ...resto } = prev;
        return resto;
      }
      return { ...prev, [id]: actual };
    });

  if (query.isLoading) return <div className={s.vacio}>Cargando el catálogo…</div>;

  // Un error NO es "no hay productos": decirle que no tiene nada cargado cuando en
  // realidad falló la consulta lo manda a buscar el problema al lugar equivocado.
  if (query.error) {
    return (
      <div className={s.error}>
        No se pudo traer el catálogo. {query.error.message || 'Probá de nuevo en un momento.'}
      </div>
    );
  }

  if (servicios.length === 0) {
    return (
      <div className={s.vacio}>
        No tenés productos activos. Cargalos en <b>Mi academia → Productos</b>, o usá
        <b> Otro concepto</b> acá arriba para escribirlo a mano.
      </div>
    );
  }

  return (
    <div className={s.lista}>
      {servicios.map((sv) => {
        const cantidad = carrito[sv.id] ?? 0;
        const foto = sv.fotos[0];
        return (
          <div key={sv.id} className={s.item}>
            <div className={s.fila}>
              {foto && <img src={foto.url} alt="" className={s.thumb} />}
              <span className={s.nombre}>{sv.nombre}</span>
              <span className={s.precio}>{formatoPlata(sv.precio)}</span>
              <div className={s.stepper}>
                <button className={s.stepperBtn} disabled={cantidad === 0} onClick={() => restar(sv.id)}>−</button>
                <span className={s.stepperCantidad}>{cantidad}</span>
                <button className={s.stepperBtn} onClick={() => sumar(sv.id)}>+</button>
              </div>
            </div>
            {/* La aclaración aparece recién cuando el producto entra: si no, la lista
                sería una tira de campos vacíos. */}
            {cantidad > 0 && (
              <input
                className={s.nota}
                maxLength={200}
                value={notas[sv.id] ?? ''}
                onChange={(e) => setNotas((prev) => ({ ...prev, [sv.id]: e.target.value }))}
                placeholder="Aclaración (opcional): marca, tensión, color…"
              />
            )}
          </div>
        );
      })}

      {total > 0 && (
        <div className={s.resumen}>
          <span>Total</span>
          <span className={s.total}>{formatoPlata(total)}</span>
        </div>
      )}
    </div>
  );
}
