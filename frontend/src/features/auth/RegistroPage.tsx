import { Link, useNavigate } from 'react-router-dom';
import AuthShell from './AuthShell';
import s from './LoginPage.module.css';

/** Landing del registro segmentado (ADR-0007): jugador / profesor / club. */
export default function RegistroPage() {
  const navigate = useNavigate();

  return (
    <AuthShell>
      <h2 className={s.cajaTitulo}>Creá tu cuenta</h2>
      <p className={s.cajaBajada}>¿Cómo vas a usar CourtSet?</p>

      <button className={s.tarjetaRol} onClick={() => navigate('/registro/jugador')}>
        <div className={`${s.rolIcono} ${s.rolIconoAlumno}`}>
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#0e6b3c" strokeWidth="2" strokeLinecap="round">
            <path d="M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0zM4 21v-1a6 6 0 0 1 12 0v1" />
          </svg>
        </div>
        <div className={s.rolTexto}>
          <div className={s.rolTitulo}>Juego o entreno</div>
          <div className={s.rolDetalle}>Gratis. Tus clases, tu cuota y tu perfil en un solo lugar.</div>
        </div>
      </button>

      <button className={s.tarjetaRol} onClick={() => navigate('/registro/profesor')}>
        <div className={s.rolIcono}>
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#0e6b3c" strokeWidth="2" strokeLinecap="round">
            <path d="M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18zM12 7v5l3 3" />
          </svg>
        </div>
        <div className={s.rolTexto}>
          <div className={s.rolTitulo}>Soy profesor</div>
          <div className={s.rolDetalle}>Gestioná alumnos, agenda y cuotas. Con suscripción mensual.</div>
        </div>
      </button>

      <button className={s.tarjetaRol} disabled>
        <div className={s.rolIcono}>
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#6b7280" strokeWidth="2" strokeLinecap="round">
            <path d="M3 21h18M5 21V7l7-4 7 4v14" />
          </svg>
        </div>
        <div className={s.rolTexto}>
          <div className={s.rolTitulo}>Soy un club</div>
          <div className={s.rolDetalle}>Sedes, staff y socios.</div>
        </div>
        <span className={s.proximamente}>Próximamente</span>
      </button>

      <Link to="/login" className={s.linkCambio}>
        Ya tengo cuenta — <b>iniciar sesión</b>
      </Link>
    </AuthShell>
  );
}
