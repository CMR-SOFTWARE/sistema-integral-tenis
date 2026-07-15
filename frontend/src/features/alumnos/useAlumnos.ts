import { useCallback, useEffect, useState } from 'react';
import { api } from '../../lib/api';
import type { AccesoCreado, Alumno, AlumnoCreado, Categoria, CreateAlumno, Estado } from './types';

/**
 * Estado y operaciones de la pantalla Alumnos contra la API .NET.
 * Tras cada mutación se recarga la lista (simple y suficiente para el prototipo).
 */
export function useAlumnos(categoria: Categoria | 'todas') {
  const [alumnos, setAlumnos] = useState<Alumno[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    try {
      const query = categoria === 'todas' ? '' : `?categoria=${categoria}`;
      setAlumnos(await api.get<Alumno[]>(`/alumnos${query}`));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error cargando alumnos');
    } finally {
      setCargando(false);
    }
  }, [categoria]);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  const crear = async (dto: CreateAlumno) => {
    // Devuelve la ficha + credenciales (la temporal viaja UNA sola vez)
    const creado = await api.post<AlumnoCreado>('/alumnos', dto);
    await cargar();
    return creado;
  };

  const crearAcceso = async (id: string, email?: string) => {
    const acceso = await api.post<AccesoCreado>(`/alumnos/${id}/acceso`, { email });
    await cargar();
    return acceso;
  };

  const cambiarEstado = async (id: string, estado: Estado) => {
    await api.patch<Alumno>(`/alumnos/${id}/estado`, { estado });
    await cargar();
  };

  const darDeBaja = async (id: string) => {
    await api.delete<void>(`/alumnos/${id}`);
    await cargar();
  };

  return { alumnos, cargando, error, crear, crearAcceso, cambiarEstado, darDeBaja };
}
