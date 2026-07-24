import { createContext, useContext, useState, type ReactNode } from 'react';
import { setFichaActiva } from '../../lib/api';
import { obtenerSesion } from '../auth/sesion';

/**
 * Ficha ACTIVA del portal (Capa 2, cuenta familiar): el titular elige con el
 * selector qué miembro está viendo. Provee el id para las keys de React Query y
 * fija el header X-Alumno-Id que lee el back.
 */
interface FichaActivaCtx {
  alumnoId: string | null;
  setAlumnoId: (id: string) => void;
}

const Ctx = createContext<FichaActivaCtx>({ alumnoId: null, setAlumnoId: () => {} });

export function FichaActivaProvider({ children }: { children: ReactNode }) {
  const alumnos = obtenerSesion()?.alumnos ?? [];
  const [alumnoId, setId] = useState<string | null>(alumnos[0]?.alumnoId ?? null);

  // Fijamos el header ANTES de que los hijos rendericen (el padre renderiza
  // primero): así las queries de los hijos ya salen con el miembro correcto.
  setFichaActiva(alumnoId);

  const setAlumnoId = (id: string) => {
    setFichaActiva(id); // sincrónico: el próximo fetch ya lo lleva
    setId(id);
  };

  return <Ctx.Provider value={{ alumnoId, setAlumnoId }}>{children}</Ctx.Provider>;
}

export const useFichaActiva = () => useContext(Ctx);
