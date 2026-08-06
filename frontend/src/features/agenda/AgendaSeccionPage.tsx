import { useState } from 'react';
import { obtenerSesion } from '../auth/sesion';
import { useSedes } from './hooks';
import SelectSede from './SelectSede';
import SelectProfe from './SelectProfe';
import CalendarioPage from './CalendarioPage';
import s from './AgendaSeccionPage.module.css';

/**
 * Sección Agenda: el Calendario, que muestra los turnos con fecha (asistencia,
 * cancelaciones) Y gestiona los horarios que los generan, con toggle Día/Semana/Mes.
 * El filtro club+profe vive acá arriba. Ya no hay sub-tabs: la pestaña Grupos se fue
 * cuando el horario pasó a tener su propio cupo y su propia lista de alumnos.
 */
export default function AgendaSeccionPage() {
  const esOwner = obtenerSesion()?.rol === 'owner';
  const { sedes } = useSedes();
  const [sede, setSede] = useState(''); // nombre de sede; '' = todas
  const [profe, setProfe] = useState(''); // userId; '' = todos

  return (
    <div>
      <div className={s.barra}>
        <div className={s.filtros}>
          <SelectSede sedes={sedes} valor={sede} onChange={setSede} />
          {esOwner && <SelectProfe valor={profe} onChange={setProfe} />}
        </div>
      </div>

      <CalendarioPage sede={sede} profe={profe} />
    </div>
  );
}
