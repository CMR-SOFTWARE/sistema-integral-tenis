import { useCallback, useEffect, useState } from 'react';
import { api } from '../../lib/api';

/** Espejo de ProfesorAsignableDto: profe al que el dueño puede asignar clases. */
export interface ProfesorAsignable {
  userId: string;
  nombre: string;
  esDueño: boolean;
}

/**
 * Trae los profes asignables del club (el dueño + los staff activos) para los
 * selectores de horario/grupo/alumno, y mapea un userId a su nombre.
 */
export function useProfesores() {
  const [profes, setProfes] = useState<ProfesorAsignable[]>([]);

  useEffect(() => {
    api.get<ProfesorAsignable[]>('/staff/asignables').then(setProfes).catch(() => setProfes([]));
  }, []);

  const nombreDe = useCallback(
    (userId: string | null | undefined) =>
      userId ? profes.find((p) => p.userId === userId)?.nombre ?? null : null,
    [profes],
  );

  return { profes, nombreDe };
}
