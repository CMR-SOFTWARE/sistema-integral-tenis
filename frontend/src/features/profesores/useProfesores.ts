import { useCallback, useMemo } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';

/** Espejo de ProfesorAsignableDto: profe al que el dueño puede asignar clases. */
export interface ProfesorAsignable {
  userId: string;
  nombre: string;
  esDueño: boolean;
  /** Valor hora base del empleado (para pre-cargar el del horario); null en el dueño. */
  valorHora: number | null;
  /** Club (sede) del empleado, para defaultear la sede del horario; null en el dueño. */
  sedeId: string | null;
}

const CLAVE = ['staff-asignables'];

/**
 * Trae los profes asignables del club (el dueño + los staff activos) para los
 * selectores de horario y alumno, y mapea un userId a su nombre.
 *
 * Con `sedeId` se acota a los que dan clases en ese club — así la ficha del alumno
 * ofrece solo los profes de su club. El dueño aparece siempre (trabaja en todos).
 *
 * Se pide SIN filtrar y se filtra acá: el equipo de un club son un puñado de
 * personas, el backend ya descartaba las otras sedes en memoria (StaffService), y
 * así las ocho pantallas que usan este hook comparten UNA sola consulta cacheada.
 * Antes cada `sedeId` era un pedido aparte: la agenda pedía la lista dos veces y
 * abrir un modal costaba otros 350 ms con el select vacío mientras tanto.
 */
export function useProfesores(sedeId?: string | null) {
  const query = useQuery({
    queryKey: CLAVE,
    queryFn: () => api.get<ProfesorAsignable[]>('/staff/asignables'),
    // El equipo cambia cada muerte de obispo; las altas y bajas invalidan a mano.
    staleTime: 10 * 60_000,
  });

  const profes = useMemo(() => {
    const todos = query.data ?? [];
    if (!sedeId) return todos;
    // Misma regla que el backend: el que no tiene club fijo entra igual (el dueño
    // trabaja en todos, y al empleado recién creado todavía no se le asignó uno).
    return todos.filter((p) => p.sedeId === null || p.sedeId === sedeId);
  }, [query.data, sedeId]);

  const nombreDe = useCallback(
    (userId: string | null | undefined) =>
      userId ? profes.find((p) => p.userId === userId)?.nombre ?? null : null,
    [profes],
  );

  return { profes, nombreDe };
}

/**
 * Para después de tocar el equipo (alta, baja, edición) o el switch de "el director
 * da clases": sin esto la lista cacheada tardaría hasta 10 minutos en enterarse.
 */
export function useInvalidarProfesores() {
  const qc = useQueryClient();
  return useCallback(() => qc.invalidateQueries({ queryKey: CLAVE }), [qc]);
}
