import { useCallback, useEffect, useState } from 'react';
import { api } from '../../lib/api';
import type { CreateGrupo, Grupo } from './types';

/** Estado y operaciones de la pantalla Grupos contra la API .NET. */
export function useGrupos() {
  const [grupos, setGrupos] = useState<Grupo[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    try {
      setGrupos(await api.get<Grupo[]>('/grupos'));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error cargando grupos');
    } finally {
      setCargando(false);
    }
  }, []);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  const crear = async (dto: CreateGrupo) => {
    const creado = await api.post<Grupo>('/grupos', dto);
    await cargar();
    return creado;
  };

  const asignar = async (grupoId: string, alumnoId: string) => {
    await api.post<void>(`/grupos/${grupoId}/alumnos`, { alumnoId });
    await cargar();
  };

  const quitar = async (grupoId: string, alumnoId: string) => {
    await api.delete<void>(`/grupos/${grupoId}/alumnos/${alumnoId}`);
    await cargar();
  };

  return { grupos, cargando, error, crear, asignar, quitar, recargar: cargar };
}
