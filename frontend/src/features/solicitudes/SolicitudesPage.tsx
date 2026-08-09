import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import { CAT_COLOR, CAT_LABEL, avatarColor, iniciales } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import type { SolicitudPendiente } from './types';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import s from './SolicitudesPage.module.css';

/**
 * Lista de espera: los que se unieron a la academia (o cargó el profe) y todavía
 * NO tienen clase. Son miembros, no alumnos: se vuelven alumnos cuando se les
 * asigna un horario. El profe los puede quitar.
 */
export default function SolicitudesPage() {
  const [espera, setEspera] = useState<SolicitudPendiente[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [toast, setToast] = useState<string | null>(null);
  const [procesando, setProcesando] = useState<string | null>(null); // id en curso
  const confirmar = useConfirmar();

  const cargar = useCallback(() => {
    api.get<SolicitudPendiente[]>('/solicitudes')
      .then(setEspera)
      .catch((e) => setError(e instanceof Error ? e.message : 'Error cargando la lista de espera'));
  }, []);

  useEffect(() => {
    cargar();
  }, [cargar]);

  const avisar = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 3500);
  };

  const quitar = async (sol: SolicitudPendiente) => {
    const ok = await confirmar({
      titulo: `¿Quitar a ${sol.nombre} ${sol.apellido}?`,
      mensaje: 'Sale de la lista de espera. Su cuenta se conserva (puede volver a unirse).',
      confirmar: 'Quitar',
      peligro: true,
    });
    if (!ok) return;
    setProcesando(sol.id);
    try {
      await api.post(`/solicitudes/${sol.id}/quitar`, {});
      avisar(`${sol.nombre} ${sol.apellido} salió de la lista de espera.`);
      cargar();
    } catch (e) {
      avisar(e instanceof ApiError ? e.message : 'No se pudo quitar.');
    } finally {
      setProcesando(null);
    }
  };

  if (error) return <div className={s.error}>{error}</div>;
  if (!espera) return <div className={s.vacio}>Cargando…</div>;

  return (
    <div>
      <div className={s.intro}>
        Los que se unieron a tu academia (o cargaste) y todavía no tienen clase. Son
        miembros, todavía no alumnos: <b>se vuelven alumnos cuando les asignás un
        horario</b> — desde la Agenda, sumándolos a una clase.
      </div>

      {espera.length === 0 && (
        <div className={s.vacioCard}>
          No hay nadie en la lista de espera. Cuando alguien se una desde su portal
          —o lo cargues sin asignarle clase— aparece acá.
        </div>
      )}

      <div className={s.lista}>
        {espera.map((sol) => {
          const av = avatarColor(sol.nombre + sol.apellido);
          const cat = sol.categoria ? CAT_COLOR[sol.categoria as Categoria] : null;
          return (
            <div key={sol.id} className={s.tarjetaSol}>
              <div className={s.avatar} style={{ background: `${av}1a`, color: av }}>
                {iniciales(sol.nombre, sol.apellido)}
              </div>
              <div className={s.cuerpo}>
                <div className={s.nombreFila}>
                  <span className={s.nombre}>{sol.nombre} {sol.apellido}</span>
                  {cat && (
                    <span className={s.chip} style={{ background: `${cat}1a`, color: cat }}>
                      {CAT_LABEL[sol.categoria as Categoria]}
                    </span>
                  )}
                  {sol.esMenor && <span className={s.chipMenor}>Con responsable</span>}
                </div>
                <div className={s.detalle}>
                  {sol.email}
                  {sol.dni && ` · DNI ${sol.dni}`}
                  {sol.telefono && ` · ${sol.telefono}`}
                </div>
                {sol.mensaje && <div className={s.mensaje}>"{sol.mensaje}"</div>}
              </div>
              <div className={s.acciones}>
                <Link to="/agenda" className={s.btnAprobar}>Asignarle un horario</Link>
                <button
                  className={s.btnRechazar}
                  disabled={procesando === sol.id}
                  onClick={() => void quitar(sol)}
                >
                  {procesando === sol.id ? '…' : 'Quitar'}
                </button>
              </div>
            </div>
          );
        })}
      </div>

      {toast && <div className={s.toast}>{toast}</div>}
    </div>
  );
}
