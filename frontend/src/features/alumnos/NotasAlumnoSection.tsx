import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../../lib/api';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import s from './NotasAlumnoSection.module.css';

interface Nota {
  id: string;
  texto: string;
  compartida: boolean;
  creadoEl: string;
}

const fecha = (iso: string) =>
  new Date(iso).toLocaleDateString('es-AR', { day: 'numeric', month: 'short', year: 'numeric' });

/**
 * Notas de seguimiento del profe sobre un alumno (mejoras/fallas de la clase).
 * Cada una es privada salvo que el profe tilde "compartir": ahí el alumno la ve
 * en su portal. Vive dentro de la ficha del alumno.
 */
export default function NotasAlumnoSection({ alumnoId }: { alumnoId: string }) {
  const [notas, setNotas] = useState<Nota[]>([]);
  const [texto, setTexto] = useState('');
  const [compartir, setCompartir] = useState(false);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const confirmar = useConfirmar();

  const cargar = useCallback(() => {
    api.get<Nota[]>(`/alumnos/${alumnoId}/notas`).then(setNotas).catch(() => setNotas([]));
  }, [alumnoId]);

  useEffect(() => { cargar(); }, [cargar]);

  const agregar = async () => {
    if (texto.trim() === '') return;
    setGuardando(true);
    setError(null);
    try {
      await api.post(`/alumnos/${alumnoId}/notas`, { texto: texto.trim(), compartida: compartir });
      setTexto('');
      setCompartir(false);
      cargar();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo guardar la nota.');
    } finally {
      setGuardando(false);
    }
  };

  const borrar = async (n: Nota) => {
    if (!(await confirmar({
      titulo: 'Borrar nota',
      mensaje: '¿Borrar esta nota de seguimiento?',
      confirmar: 'Borrar',
      peligro: true,
    }))) return;
    await api.delete(`/alumnos/${alumnoId}/notas/${n.id}`);
    cargar();
  };

  return (
    <div>
      <div className={s.seccion}>Notas de seguimiento</div>
      <p className={s.ayuda}>
        Apuntes de la clase. Son privados; tildá <b>compartir</b> para que el alumno la vea en su portal.
      </p>

      {error && <div className={s.error}>{error}</div>}

      <div className={s.alta}>
        <textarea
          className={s.textarea}
          value={texto}
          onChange={(e) => setTexto(e.target.value)}
          placeholder="Ej: mejoró el revés a dos manos; trabajar el saque."
          maxLength={1000}
          rows={2}
        />
        <div className={s.altaPie}>
          <label className={s.check}>
            <input type="checkbox" checked={compartir} onChange={(e) => setCompartir(e.target.checked)} />
            Compartir con el alumno
          </label>
          <button
            className={s.btnAgregar}
            disabled={guardando || texto.trim() === ''}
            onClick={() => void agregar()}
          >
            {guardando ? 'Guardando…' : 'Agregar nota'}
          </button>
        </div>
      </div>

      {notas.length === 0 ? (
        <div className={s.vacio}>Todavía no hay notas de este alumno.</div>
      ) : (
        <div className={s.lista}>
          {notas.map((n) => (
            <div key={n.id} className={s.nota}>
              <div className={s.notaCuerpo}>
                <div className={s.notaTexto}>{n.texto}</div>
                <div className={s.notaMeta}>
                  {fecha(n.creadoEl)}
                  {n.compartida
                    ? <span className={s.badgeCompartida}>Compartida</span>
                    : <span className={s.badgePrivada}>Privada</span>}
                </div>
              </div>
              <button className={s.btnBorrar} onClick={() => void borrar(n)} aria-label="Borrar nota">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                  <path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6" />
                </svg>
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
