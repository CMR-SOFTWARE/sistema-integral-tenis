import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { profNav, pageTitles } from './nav';
import s from './AppLayout.module.css';

/** Fecha de hoy estilo "Jueves 18 de junio, 2026" (como en el mockup). */
function fechaDeHoy(): string {
  const texto = new Date().toLocaleDateString('es-AR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
  return texto.charAt(0).toUpperCase() + texto.slice(1);
}

/**
 * Shell del rol Profesor: sidebar (marca + navegación + usuario) y header
 * sticky (título de página, buscador, acción principal). El contenido de
 * cada sección se renderiza en el <Outlet /> según la ruta.
 */
export default function AppLayout() {
  const { pathname } = useLocation();
  const title = pageTitles[pathname] ?? 'CourtSet';

  return (
    <div className={s.shell}>
      <aside className={s.sidebar}>
        <div className={s.brand}>
          <div className={s.brandLogo}>C</div>
          <div>
            <div className={s.brandName}>CourtSet</div>
            <div className={s.brandTenant}>Club Demo</div>
          </div>
        </div>

        <nav className={s.nav}>
          {profNav.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => (isActive ? `${s.navItem} ${s.navItemActive}` : s.navItem)}
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d={item.icon} />
              </svg>
              <span>{item.label}</span>
            </NavLink>
          ))}
        </nav>

        <div className={s.sidebarFooter}>
          <div className={s.userCard}>
            <div className={s.userAvatar}>CD</div>
            <div className={s.userInfo}>
              <div className={s.userName}>Club Demo</div>
              <div className={s.userRole}>Profesor</div>
            </div>
          </div>
        </div>
      </aside>

      <div className={s.main}>
        <header className={s.header}>
          <div className={s.headerTitles}>
            <h1 className={s.pageTitle}>{title}</h1>
            <div className={s.pageDate}>{fechaDeHoy()}</div>
          </div>
        </header>

        <main className={s.content}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
