import { useEffect, useState } from 'react';
import { api } from '../../lib/api';
import AlumnosPage from './AlumnosPage';
import SolicitudesPage from '../solicitudes/SolicitudesPage';
import s from './AlumnosSeccionPage.module.css';

type Tab = 'alumnos' | 'espera' | 'usuarios';

/**
 * Sección Alumnos con tres pestañas, que son tres recortes de la misma gente:
 *
 *  - **Alumnos**: los que tienen una clase asignada.
 *  - **Lista de espera**: los que quieren clase y no la tienen —sin ninguna, o con
 *    un pedido que el profe no resolvió—. Un alumno con un pedido pendiente está
 *    acá Y en Alumnos: por eso la espera dejó de ser un estado excluyente.
 *  - **Usuarios**: todos los que se unieron a la academia, tengan horario o no.
 *
 * Alumnos y Usuarios son la MISMA tabla con distinto filtro (`lista`). Solo se monta
 * la pestaña activa. El profe empleado (staff) ve las tres, acotadas a SUS alumnos.
 */
export default function AlumnosSeccionPage() {
  const [tab, setTab] = useState<Tab>('alumnos');
  const [enEspera, setEnEspera] = useState(0);

  // Contador para el badge de la pestaña. Se refresca al cambiar de pestaña
  // (p. ej. tras asignarle un horario a alguien, que lo saca de la espera).
  useEffect(() => {
    api.get<{ pendientes: number }>('/solicitudes/conteo')
      .then((c) => setEnEspera(c.pendientes))
      .catch(() => setEnEspera(0));
  }, [tab]);

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
        <button className={tab === 'usuarios' ? s.tabActivo : s.tab} onClick={() => setTab('usuarios')}>
          Usuarios
        </button>
      </div>

      {tab === 'alumnos' && <AlumnosPage lista="ConClase" />}
      {tab === 'espera' && <SolicitudesPage />}
      {tab === 'usuarios' && <AlumnosPage lista="Todos" />}
    </div>
  );
}
