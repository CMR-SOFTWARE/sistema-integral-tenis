import { useNavigate } from 'react-router-dom';
import s from './LoginPage.module.css';

/**
 * Pantalla de entrada (mockup CourtSet, líneas 33-86). Login VISUAL:
 * elegir "Profesor" entra al tenant demo (sin auth real todavía, ADR-0004).
 * El acceso de Alumno se muestra deshabilitado: el portal del alumno es
 * una fase futura (ADR-0006 / modelo-identidad-roles.md).
 */
export default function LoginPage() {
  const navigate = useNavigate();

  const entrarComoProfesor = () => {
    sessionStorage.setItem('demoRol', 'profesor');
    navigate('/dashboard');
  };

  return (
    <div className={s.pantalla}>
      {/* ── Panel izquierdo: marca ── */}
      <div className={s.panelMarca}>
        <div className={s.grilla} />
        <div className={s.pelota} />
        <div className={s.aro} />

        <div className={s.marca}>
          <div className={s.marcaLogo}>C</div>
          <div className={s.marcaNombre}>CourtSet</div>
        </div>

        <div className={s.heroTexto}>
          <div className={s.eyebrow}>Gestión deportiva</div>
          <h1 className={s.titulo}>Tu cancha,<br />bajo control.</h1>
          <p className={s.bajada}>
            Alumnos, turnos, grupos por categoría, cuotas y disponibilidad.
            Todo en un solo lugar, rápido y claro.
          </p>
        </div>

        <div className={s.stats}>
          <div><div className={s.statNum}>12</div><div className={s.statLabel}>Alumnos</div></div>
          <div className={s.statSep} />
          <div><div className={s.statNum}>7</div><div className={s.statLabel}>Categorías</div></div>
          <div className={s.statSep} />
          <div><div className={s.statNum}>$228k</div><div className={s.statLabel}>Este mes</div></div>
        </div>
      </div>

      {/* ── Panel derecho: elección de rol ── */}
      <div className={s.panelLogin}>
        <div className={s.caja}>
          <h2 className={s.cajaTitulo}>Ingresá a tu cuenta</h2>
          <p className={s.cajaBajada}>Elegí cómo querés entrar al sistema.</p>

          <button className={s.tarjetaRol} onClick={entrarComoProfesor}>
            <div className={`${s.rolIcono} ${s.rolIconoProf}`}>
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2" strokeLinecap="round">
                <path d="M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21a8 8 0 0 1 16 0" />
              </svg>
            </div>
            <div className={s.rolTexto}>
              <div className={s.rolTitulo}>Profesor / Administrador</div>
              <div className={s.rolDetalle}>Club Demo · gestión completa</div>
            </div>
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#178a4c" strokeWidth="2.5" strokeLinecap="round">
              <path d="M9 6l6 6-6 6" />
            </svg>
          </button>

          <button className={`${s.tarjetaRol} ${s.tarjetaDeshabilitada}`} disabled>
            <div className={`${s.rolIcono} ${s.rolIconoAlumno}`}>
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#0e6b3c" strokeWidth="2" strokeLinecap="round">
                <path d="M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0zM4 21v-1a6 6 0 0 1 12 0v1" />
              </svg>
            </div>
            <div className={s.rolTexto}>
              <div className={s.rolTitulo}>Alumno</div>
              <div className={s.rolDetalle}>Tus turnos, cuota y avisos</div>
            </div>
            <span className={s.proximamente}>Próximamente</span>
          </button>

          <div className={s.pie}>Prototipo · datos de demostración</div>
        </div>
      </div>
    </div>
  );
}
