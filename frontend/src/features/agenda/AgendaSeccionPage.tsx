import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { obtenerSesion } from '../auth/sesion';
import { useSedes } from './hooks';
import SelectSede from './SelectSede';
import SelectProfe from './SelectProfe';
import CalendarioPage from './CalendarioPage';
import GruposPage from '../grupos/GruposPage';
import s from './AgendaSeccionPage.module.css';

type Tab = 'calendario' | 'grupos';

const TABS: { id: Tab; label: string }[] = [
  { id: 'calendario', label: 'Calendario' },
  { id: 'grupos', label: 'Grupos' },
];

/**
 * Sección Agenda: el Calendario (turnos con fecha + la gestión de los horarios que
 * los generan, todo en una vista con toggle Semana/Mes) y Grupos. El filtro club+profe
 * vive acá y baja a cada tab. El staff ve solo Calendario (Grupos es del dueño).
 */
export default function AgendaSeccionPage() {
  const esOwner = obtenerSesion()?.rol === 'owner';
  const [params] = useSearchParams();
  const pedido = params.get('tab');
  const inicial: Tab = TABS.some((t) => t.id === pedido) ? (pedido as Tab) : 'calendario';
  const [tab, setTab] = useState<Tab>(esOwner ? inicial : 'calendario');

  const { sedes } = useSedes();
  const [sede, setSede] = useState(''); // nombre de sede; '' = todas
  const [profe, setProfe] = useState(''); // userId; '' = todos

  const tabs = esOwner ? TABS : TABS.filter((t) => t.id === 'calendario');

  return (
    <div>
      <div className={s.barra}>
        <div className={s.tabs}>
          {tabs.map((t) => (
            <button
              key={t.id}
              className={tab === t.id ? s.tabActivo : s.tab}
              onClick={() => setTab(t.id)}
            >
              {t.label}
            </button>
          ))}
        </div>
        <div className={s.filtros}>
          <SelectSede sedes={sedes} valor={sede} onChange={setSede} />
          {esOwner && <SelectProfe valor={profe} onChange={setProfe} />}
        </div>
      </div>

      {tab === 'calendario' && <CalendarioPage sede={sede} profe={profe} />}
      {tab === 'grupos' && esOwner && <GruposPage profe={profe} />}
    </div>
  );
}
