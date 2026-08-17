import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { api } from '../../lib/api';
import ProfesoresPage from '../profesores/ProfesoresPage';
import SueldosPage from '../sueldos/SueldosPage';
import ProductosPage from './ProductosPage';
import PedidosPage from './PedidosPage';
import ConfiguracionPage from '../agenda/ConfiguracionPage';
import s from './MiAcademiaPage.module.css';

type Tab = 'profesores' | 'sueldos' | 'productos' | 'pedidos' | 'configuracion';

const TABS: { id: Tab; label: string }[] = [
  { id: 'profesores', label: 'Profesores' },
  { id: 'sueldos', label: 'Sueldos' },
  { id: 'productos', label: 'Productos' },
  { id: 'pedidos', label: 'Pedidos' },
  { id: 'configuracion', label: 'Configuración' },
];

/**
 * "Mi academia": la gestión del negocio en un solo lugar, con pestañas. Cada
 * pestaña es una pantalla propia; solo se monta la activa (la inactiva no
 * consulta datos).
 *
 * El Shop entero vive acá: el catálogo (Productos) y la bandeja de lo que piden
 * los alumnos (Pedidos), que antes estaba arriba de las cuotas.
 */
export default function MiAcademiaPage() {
  const [params] = useSearchParams();
  const pedido = params.get('tab');
  const inicial: Tab = TABS.some((t) => t.id === pedido) ? (pedido as Tab) : 'profesores';
  const [tab, setTab] = useState<Tab>(inicial);

  // Contador del badge. Comparte la query key con el aviso del Inicio: es el mismo
  // dato, así que se piden una sola vez y bajan los dos juntos al resolver un pedido.
  const { data: pendientes = 0 } = useQuery({
    queryKey: ['pedidos-pendientes-cuenta'],
    queryFn: () => api.get<number>('/pedidos/pendientes/cuenta'),
  });

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
            {t.id === 'pedidos' && pendientes > 0 && <span className={s.badge}>{pendientes}</span>}
          </button>
        ))}
      </div>

      {tab === 'profesores' && <ProfesoresPage />}
      {tab === 'sueldos' && <SueldosPage />}
      {tab === 'productos' && <ProductosPage />}
      {tab === 'pedidos' && <PedidosPage />}
      {tab === 'configuracion' && <ConfiguracionPage />}
    </div>
  );
}
