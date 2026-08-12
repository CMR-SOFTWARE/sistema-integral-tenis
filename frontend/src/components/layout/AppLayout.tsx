import { useEffect, useRef, useState } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { api } from '../../lib/api';
import { cerrarSesion, obtenerSesion } from '../../features/auth/sesion';
import BotonMenu from './BotonMenu';
import BarraInferior from './BarraInferior';
import { useConfirmar } from '../confirmar/ConfirmarProvider';
import { BotonTema } from '../../theme/Tema';
import PelotaNav from '../tenis/PelotaNav';
import { coincideRuta, profNav, pageTitles } from './nav';
import s from './AppLayout.module.css';

/** Pelota que cruza el header una vez al cambiar de pantalla. */
function PelotaRuta() {
  return <span className={s.pelotaRuta} aria-hidden />;
}

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
  const navigate = useNavigate();
  const confirmar = useConfirmar();
  const title = pageTitles[pathname] ?? 'CMR';
  const sesion = obtenerSesion();
  const [menuAbierto, setMenuAbierto] = useState(false);
  const navRef = useRef<HTMLElement>(null);
  const navItems = profNav.filter(
    (item) => (!item.soloOwner || sesion?.rol === 'owner') && (!item.soloAdmin || sesion?.esAdmin),
  );

  // Al navegar se cierra el drawer (en escritorio no se ve: CSS)
  useEffect(() => {
    setMenuAbierto(false);
  }, [pathname]);

  // Con el drawer abierto en mobile, bloqueamos el scroll del fondo para que el
  // gesto de scroll dentro del menú no arrastre la página de atrás.
  useEffect(() => {
    document.body.style.overflow = menuAbierto ? 'hidden' : '';
    return () => { document.body.style.overflow = ''; };
  }, [menuAbierto]);

  const salir = async () => {
    const ok = await confirmar({
      titulo: '¿Cerrar sesión?',
      mensaje: 'Vas a volver a la pantalla de inicio de sesión.',
      confirmar: 'Cerrar sesión',
    });
    if (!ok) return;
    cerrarSesion();
    navigate('/login');
  };

  const desvincularme = async () => {
    const ok = await confirmar({
      titulo: '¿Desvincularte del club?',
      mensaje: 'Vas a dejar de trabajar en esta academia y volvés a ser un usuario normal. El dueño puede sumarte de nuevo más adelante.',
      confirmar: 'Desvincularme',
      peligro: true,
    });
    if (!ok) return;
    try {
      await api.post('/staff/desvincularme', {});
    } catch {
      // si algo falla igual cerramos sesión: al re-loguear ya no será staff
    }
    cerrarSesion();
    navigate('/login');
  };

  return (
    <div className={s.shell}>
      {menuAbierto && (
        <button
          className={s.backdrop}
          onClick={() => setMenuAbierto(false)}
          aria-label="Cerrar menú"
        />
      )}

      <aside className={`${s.sidebar} ${menuAbierto ? s.sidebarAbierto : ''}`}>
        <div className={s.brand}>
          <div className={s.brandLogo} aria-hidden>CMR</div>
          <div>
            <div className={s.brandName}>CMR</div>
            <div className={s.brandTenant}>{sesion?.rol === 'staff' ? 'Staff' : 'Tenis'}</div>
          </div>
        </div>

        <nav ref={navRef} className={s.nav}>
          <PelotaNav contenedorRef={navRef} />
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              data-nav-activo={coincideRuta(pathname, item.to) ? '1' : undefined}
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
              {(sesion ? `${sesion.nombre.charAt(0)}${sesion.apellido.charAt(0)}` : 'CD').toUpperCase()}
            </div>
            <div className={s.userInfo}>
              <div className={s.userName}>{sesion ? `${sesion.nombre} ${sesion.apellido}` : 'Club Demo'}</div>
              <div className={s.userRole}>{sesion?.rol === 'staff' ? 'Profe empleado' : 'Director'}</div>
            </div>
            <button className={s.logout} title="Salir" onClick={salir}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                <path d="M9 21H5V3h4M16 17l5-5-5-5M21 12H9" />
              </svg>
            </button>
          </div>
          {sesion?.rol === 'staff' && (
            <button className={s.desvincular} onClick={desvincularme}>
              Desvincularme del club
            </button>
          )}
        </div>
      </aside>

      <div className={s.main}>
        <header className={s.header}>
          <PelotaRuta key={pathname} />
          <span className={s.headerVivo} aria-hidden />
          <BotonMenu onClick={() => setMenuAbierto(true)} />
          <div className={s.headerTitles}>
            <h1 className={s.pageTitle}>{title}</h1>
            <div className={s.pageDate}>{fechaDeHoy()}</div>
          </div>
          {/* Es profe y ADEMÁS alumno: puede pasarse a su portal de alumno. */}
          {sesion?.alumno && (
            <button className={s.switcher} onClick={() => navigate('/portal')}>
              Mi portal
            </button>
          )}
          <BotonTema />
        </header>

        <main className={s.content}>
          <Outlet />
        </main>
      </div>

      <BarraInferior
        items={profNav.filter((item) => item.enBarra && (!item.soloOwner || sesion?.rol === 'owner'))}
        onMas={() => setMenuAbierto(true)}
      />
    </div>
  );
}
