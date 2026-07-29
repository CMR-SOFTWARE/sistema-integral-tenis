import { useEffect, useState } from 'react';
import { obtenerSesion } from '../auth/sesion';
import { api } from '../../lib/api';
import AlumnosPage from './AlumnosPage';
import SolicitudesPage from '../solicitudes/SolicitudesPage';
import s from './AlumnosSeccionPage.module.css';

type Tab = 'alumnos' | 'espera';

/**
 * Sección Alumnos con pestañas (como "Mi academia"): "Alumnos" (los que tienen
 * clase) y "Lista de espera" (miembros sin clase todavía). Solo se monta la
 * pestaña activa. El profe empleado (staff) solo ve su lista de alumnos.
 */
export default function AlumnosSeccionPage() {
  const esOwner = obtenerSesion()?.rol === 'owner';
  const [tab, setTab] = useState<Tab>('alumnos');
  const [enEspera, setEnEspera] = useState(0);

  // Contador para el badge de la pestaña (barato: un COUNT). Se refresca al
  // cambiar de pestaña (p. ej. tras quitar a alguien de la espera).
  useEffect(() => {
    if (!esOwner) return;
    api.get<{ pendientes: number }>('/solicitudes/conteo')
      .then((c) => setEnEspera(c.pendientes))
      .catch(() => setEnEspera(0));
  }, [tab, esOwner]);

  // Staff: la lista de espera es del dueño → solo mostramos Alumnos, sin pestañas.
  if (!esOwner) return <AlumnosPage />;

  return (
    <div>
      <div className={s.tabs}>
        <button className={tab === 'alumnos' ? s.tabActivo : s.tab} onClick={() => setTab('alumnos')}>
          Alumnos
        </button>
        <button className={tab === 'espera' ? s.tabActivo : s.tab} onClick={() => setTab('espera')}>
          Lista de espera
          {enEspera > 0 && <span className={s.badge}>{enEspera}</span>}
        </button>
      </div>

      {tab === 'alumnos' ? <AlumnosPage /> : <SolicitudesPage />}
    </div>
  );
}
