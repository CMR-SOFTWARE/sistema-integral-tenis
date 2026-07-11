import { useEffect, useState } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { api } from '../../lib/api';
import { cerrarSesion, obtenerSesion } from '../auth/sesion';
import { alumnoNav, portalTitles } from '../../components/layout/nav';
import { CAT_LABEL } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import type { MiPerfil } from './types';
import s from '../../components/layout/AppLayout.module.css';

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
 * Shell del PORTAL DEL ALUMNO (mockup CourtSet): mismo esqueleto que el del
 * profesor (sidebar + header) con la navegación del alumno. Reusa el CSS de
 * AppLayout para que ambos roles se vean como un solo producto.
 */
export default function PortalLayout() {
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const sesion = obtenerSesion();
  const [perfil, setPerfil] = useState<MiPerfil | null>(null);
  const title = portalTitles[pathname] ?? 'CourtSet';

  useEffect(() => {
    api.get<MiPerfil>('/portal/perfil').then(setPerfil).catch(() => setPerfil(null));
  }, []);

  const salir = () => {
    cerrarSesion();
    navigate('/login');
  };

  const nombre = sesion ? `${sesion.nombre} ${sesion.apellido}` : '';
  const cat = perfil ? CAT_LABEL[perfil.categoria as Categoria] ?? perfil.categoria : null;

  return (
    <div className={s.shell}>
      <aside className={s.sidebar}>
        <div className={s.brand}>
          <div className={s.brandLogo}>C</div>
          <div>
            <div className={s.brandName}>CourtSet</div>
            <div className={s.brandTenant}>Portal del alumno</div>
          </div>
        </div>

        <nav className={s.nav}>
          {alumnoNav.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/portal'}
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
            <div className={s.userAvatar}>
              {sesion ? `${sesion.nombre.charAt(0)}${sesion.apellido.charAt(0)}`.toUpperCase() : ''}
            </div>
            <div className={s.userInfo}>
              <div className={s.userName}>{nombre}</div>
              <div className={s.userRole}>{cat ? `Cat. ${cat}` : sesion?.alumno?.club}</div>
            </div>
            <button className={s.logout} title="Salir" onClick={salir}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                <path d="M9 21H5V3h4M16 17l5-5-5-5M21 12H9" />
              </svg>
            </button>
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
