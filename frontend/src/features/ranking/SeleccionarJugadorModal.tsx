import { useMemo, useState } from 'react';
import Modal from '../../components/Modal';
import Avatar from '../../components/Avatar';
import s from './SeleccionarJugadorModal.module.css';

interface JugadorSeleccionable {
  jugadorId: string;
  nombre: string;
  apellido: string;
  rango: string;
}

interface Props<T extends JugadorSeleccionable> {
  titulo?: string;
  jugadores: T[];
  /** jugadorIds que no deben aparecer (yo mismo + los ya elegidos en otros slots). */
  excluir: string[];
  ocupado?: string | null;
  onElegir: (jugador: T) => void;
  onClose: () => void;
}

/** Buscador de jugador por nombre — entrada única para armar un desafío de
 *  singles o cualquiera de los 3 slots de un partido de dobles. */
export default function SeleccionarJugadorModal<T extends JugadorSeleccionable>({
  titulo = 'Buscar jugadores', jugadores, excluir, ocupado, onElegir, onClose,
}: Props<T>) {
  const [busqueda, setBusqueda] = useState('');

  const resultados = useMemo(() => {
    const disponibles = jugadores.filter((j) => !excluir.includes(j.jugadorId));
    const q = busqueda.trim().toLowerCase();
    if (!q) return disponibles;
    return disponibles.filter((j) => `${j.nombre} ${j.apellido}`.toLowerCase().includes(q));
  }, [jugadores, excluir, busqueda]);

  return (
    <Modal titulo={titulo} onClose={onClose}>
      <input
        className={s.buscador}
        autoFocus
        placeholder="Nombre del jugador"
        value={busqueda}
        onChange={(e) => setBusqueda(e.target.value)}
      />
      <div className={s.lista}>
        {resultados.length === 0 && <div className={s.vacio}>No encontramos a nadie con ese nombre.</div>}
        {resultados.map((j) => (
          <button
            key={j.jugadorId}
            className={s.resultado}
            disabled={ocupado === j.jugadorId}
            onClick={() => onElegir(j)}
          >
            <Avatar nombre={j.nombre} apellido={j.apellido} size={32} />
            <span className={s.resultadoNombre}>{j.nombre} {j.apellido}</span>
            <span className={s.resultadoRango}>{ocupado === j.jugadorId ? '…' : `Rango ${j.rango}`}</span>
          </button>
        ))}
      </div>
    </Modal>
  );
}
