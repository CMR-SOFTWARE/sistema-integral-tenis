import { useCallback, useEffect, useState } from 'react';
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

/**
 * Trae los profes asignables del club (el dueño + los staff activos) para los
 * selectores de horario/grupo/alumno, y mapea un userId a su nombre.
 *
 * Con `sedeId` se acota a los que dan clases en ese club — así la ficha del alumno
 * ofrece solo los profes de su club. El dueño aparece siempre (trabaja en todos).
 */
export function useProfesores(sedeId?: string | null) {
  const [profes, setProfes] = useState<ProfesorAsignable[]>([]);

  useEffect(() => {
    const query = sedeId ? `?sedeId=${sedeId}` : '';
    api.get<ProfesorAsignable[]>(`/staff/asignables${query}`).then(setProfes).catch(() => setProfes([]));
  }, [sedeId]);

  const nombreDe = useCallback(
    (userId: string | null | undefined) =>
      userId ? profes.find((p) => p.userId === userId)?.nombre ?? null : null,
    [profes],
  );

  return { profes, nombreDe };
}
