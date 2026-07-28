import { useState } from 'react';
import ProfesoresPage from '../profesores/ProfesoresPage';
import SueldosPage from '../sueldos/SueldosPage';
import ConfiguracionPage from '../agenda/ConfiguracionPage';
import s from './MiAcademiaPage.module.css';

type Tab = 'profesores' | 'sueldos' | 'configuracion';

const TABS: { id: Tab; label: string }[] = [
  { id: 'profesores', label: 'Profesores' },
  { id: 'sueldos', label: 'Sueldos' },
  { id: 'configuracion', label: 'Configuración' },
];

/**
 * "Mi academia": la gestión del negocio en un solo lugar, con pestañas. Cada
 * pestaña es una pantalla que ya existía (Profesores, Sueldos, Configuración);
 * solo se monta la activa (la inactiva no consulta datos).
 */
export default function MiAcademiaPage() {
  const [tab, setTab] = useState<Tab>('profesores');

  return (
    <div>
      <div className={s.tabs}>
        {TABS.map((t) => (
          <button
            key={t.id}
            className={tab === t.id ? s.tabActivo : s.tab}
            onClick={() => setTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'profesores' && <ProfesoresPage />}
      {tab === 'sueldos' && <SueldosPage />}
      {tab === 'configuracion' && <ConfiguracionPage />}
    </div>
  );
}
