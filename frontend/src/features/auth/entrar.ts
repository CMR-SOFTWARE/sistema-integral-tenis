import type { NavigateFunction } from 'react-router-dom';
import { guardarSesion } from './sesion';
import type { Sesion } from './sesion';

/**
 * Después de login/registro/activación: guardar la sesión y decidir destino.
 * Devuelve true si navegó (false = hay fichas por reclamar: la pantalla que
 * llama decide cómo ofrecerlas).
 */
export function entrarConSesion(s: Sesion, navigate: NavigateFunction): boolean {
  guardarSesion(s);
  if (s.esProfesor) {
    navigate('/dashboard');
    return true;
  }
  if (s.estadoTenant === 'PendientePago') {
    navigate('/checkout'); // profe registrado que todavía no pagó
    return true;
  }
  if (s.alumno) {
    navigate('/portal');
    return true;
  }
  if (s.fichasPorReclamar.length > 0) return false; // ofrecer el reclamo
  navigate('/portal'); // jugador sin club: portal en modo vacío
  return true;
}
