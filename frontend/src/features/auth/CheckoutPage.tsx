import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, ApiError } from '../../lib/api';
import AuthShell from './AuthShell';
import { entrarConSesion } from './entrar';
import { cerrarSesion, obtenerSesion } from './sesion';
import type { Sesion } from './sesion';
import s from './LoginPage.module.css';

/**
 * Checkout de la suscripción — SIMULADO en el prototipo. Al desplegar, el
 * botón redirige a Mercado Pago (Checkout Pro) y la activación la dispara
 * el webhook; la transición de estado es la MISMA (ActivarTenantAsync).
 */
export default function CheckoutPage() {
  const navigate = useNavigate();
  const sesion = obtenerSesion();
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const pagar = async () => {
    setError(null);
    setEnviando(true);
    try {
      const s2 = await api.post<Sesion>('/auth/activar-tenant', {});
      entrarConSesion(s2, navigate); // token nuevo con claims profe+tenant → dashboard
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo confirmar el pago.');
      setEnviando(false);
    }
  };

  const salir = () => {
    cerrarSesion();
    navigate('/login');
  };

  return (
    <AuthShell>
      <h2 className={s.cajaTitulo}>¡Ya casi, {sesion?.nombre}!</h2>
      <p className={s.cajaBajada}>
        Tu club quedó reservado. Confirmá la suscripción para activar la gestión.
      </p>

      <div className={s.checkoutResumen}>
        <div className={s.checkoutFila}>
          <span>Suscripción CourtSet — Profesor</span>
          <b>$25.000/mes</b>
        </div>
        <div className={s.checkoutDetalle}>
          Alumnos y grupos ilimitados · agenda con bloqueos · cuotas y
          reportes · portal para tus alumnos.
        </div>
      </div>

      {error && <div className={s.error}>{error}</div>}

      <button className={s.btnEntrar} onClick={() => void pagar()} disabled={enviando}>
        {enviando ? 'Confirmando…' : 'Pagar suscripción (simulado)'}
      </button>
      <p className={s.checkoutNota}>
        Pago de prueba: al desplegar se reemplaza por Mercado Pago.
      </p>

      <button className={s.linkCambio} onClick={salir}>
        Salir y pagar más tarde
      </button>
    </AuthShell>
  );
}
