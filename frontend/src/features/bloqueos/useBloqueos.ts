import { useCallback, useEffect, useState } from 'react';
import { api } from '../../lib/api';
import type { Bloqueo, CreateBloqueo, Impacto } from './types';

export function useBloqueos() {
  const [bloqueos, setBloqueos] = useState<Bloqueo[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    try {
      setBloqueos(await api.get<Bloqueo[]>('/bloqueos'));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error cargando bloqueos');
    } finally {
      setCargando(false);
    }
  }, []);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  /** Preview del impacto: NO persiste nada (alimenta el modal Impacto). */
  const previsualizar = (dto: CreateBloqueo) =>
    api.post<Impacto>('/bloqueos/impacto', dto);

  const crear = async (dto: CreateBloqueo) => {
    await api.post('/bloqueos', dto);
    await cargar();
  };

  const eliminar = async (id: string) => {
    await api.delete(`/bloqueos/${id}`);
    await cargar();
  };

  return { bloqueos, cargando, error, previsualizar, crear, eliminar };
}
