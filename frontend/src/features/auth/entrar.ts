import type { NavigateFunction } from 'react-router-dom';
import { guardarSesion } from './sesion';
import type { Sesion } from './sesion';

/** Después de login/registro/activación: guardar la sesión y decidir destino. */
export function entrarConSesion(s: Sesion, navigate: NavigateFunction): void {
  guardarSesion(s);
  if (s.esProfesor) navigate('/dashboard');
  else if (s.estadoTenant === 'PendientePago') navigate('/checkout'); // profe que no pagó
  else navigate('/portal'); // jugador (con club, o sin club en modo vacío)
}
