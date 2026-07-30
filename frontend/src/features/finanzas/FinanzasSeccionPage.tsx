import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import CuotasPage from '../cuotas/CuotasPage';
import ReportesPage from '../reportes/ReportesPage';
import s from './FinanzasSeccionPage.module.css';

type Tab = 'cuotas' | 'reportes';

const TABS: { id: Tab; label: string }[] = [
  { id: 'cuotas', label: 'Cuotas' },
  { id: 'reportes', label: 'Reportes' },
];

/**
 * Sección Finanzas: la operación del mes (Cuotas) y la analítica (Reportes) en una
 * sola sección con sub-tabs. Reúne dos ítems que antes vivían sueltos en el menú.
 */
export default function FinanzasSeccionPage() {
  const [params] = useSearchParams();
  const pedido = params.get('tab');
  const inicial: Tab = TABS.some((t) => t.id === pedido) ? (pedido as Tab) : 'cuotas';
  const [tab, setTab] = useState<Tab>(inicial);

  return (
    <div>
      <div className={s.barra}>
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
      </div>

      {tab === 'cuotas' && <CuotasPage />}
      {tab === 'reportes' && <ReportesPage />}
    </div>
  );
}
