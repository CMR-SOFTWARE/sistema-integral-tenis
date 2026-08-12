import { useEffect, useRef, useState } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { api } from '../../lib/api';
import { cerrarSesion, guardarSesion, obtenerSesion } from '../auth/sesion';
import type { Sesion } from '../auth/sesion';
import BotonMenu from '../../components/layout/BotonMenu';
import BarraInferior from '../../components/layout/BarraInferior';
import Avatar from '../../components/Avatar';
import { useConfirmar } from '../../components/confirmar/ConfirmarProvider';
import { alumnoNav, coincideRuta, portalTitles } from '../../components/layout/nav';
import PelotaNav from '../../components/tenis/PelotaNav';
import { CAT_LABEL } from '../alumnos/types';
import type { Categoria } from '../alumnos/types';
import type { MiPerfil } from './types';
import { FichaActivaProvider, useFichaActiva } from './FichaActivaContext';
import { BotonTema } from '../../theme/Tema';
import s from '../../components/layout/AppLayout.module.css';

/** Selector de miembro de la familia (Capa 2): solo aparece si hay más de uno. */
function SelectorMiembro() {
  const alumnos = obtenerSesion()?.alumnos ?? [];
  const { alumnoId, setAlumnoId } = useFichaActiva();
  if (alumnos.length <= 1) return null;
  return (
    <label className={s.selectorMiembro}>
      <span>Viendo a</span>
      <select
        value={alumnoId ?? ''}
        onChange={(e) => setAlumnoId(e.target.value)}
      >
        {alumnos.map((a) => (
          <option key={a.alumnoId} value={a.alumnoId}>{a.nombre} {a.apellido}</option>
        ))}
      </select>
    </label>
  );
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
 * Shell del PORTAL DEL ALUMNO (mockup CourtSet): mismo esqueleto que el del
 * profesor (sidebar + header) con la navegación del alumno. Reusa el CSS de
 * AppLayout para que ambos roles se vean como un solo producto.
 */
export default function PortalLayout() {
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const confirmar = useConfirmar();
  const [sesion, setSesion] = useState<Sesion | null>(obtenerSesion());
  const [perfil, setPerfil] = useState<MiPerfil | null>(null);
  const [menuAbierto, setMenuAbierto] = useState(false);
  const title = portalTitles[pathname] ?? 'CMR';
  const navRef = useRef<HTMLElement>(null);

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

  useEffect(() => {
    // Refrescar la sesión al entrar: si reclamó ficha en otra pestaña o el
    // profe lo cargó después, acá se entera sin re-loguear
    api.get<Sesion>('/auth/yo')
      .then((s2) => {
        guardarSesion(s2);
        setSesion(s2);
        if (s2.alumno) {
          api.get<MiPerfil>('/portal/perfil').then(setPerfil).catch(() => setPerfil(null));
        }
      })
      .catch(() => {}); // sin red: seguimos con la sesión guardada
  }, []);

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

  const nombre = sesion ? `${sesion.nombre} ${sesion.apellido}` : '';
  const cat = perfil ? CAT_LABEL[perfil.categoria as Categoria] ?? perfil.categoria : null;

  return (
    <FichaActivaProvider>
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
            <div className={s.brandTenant}>{sesion?.alumno?.club ?? 'Portal'}</div>
          </div>
        </div>

        <nav ref={navRef} className={s.nav}>
          <PelotaNav contenedorRef={navRef} />
          {alumnoNav.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/portal'}
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
            <Avatar
              nombre={sesion?.nombre ?? ''}
              apellido={sesion?.apellido ?? ''}
              fotoUrl={perfil?.fotoUrl}
              size={38}
              radius={11}
            />
            <div className={s.userInfo}>
              <div className={s.userName}>{nombre}</div>
              <div className={s.userRole}>{cat ? `Cat. ${cat}` : sesion?.alumno?.club ?? 'Sin club'}</div>
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
          <span key={pathname} className={s.pelotaRuta} aria-hidden />
          <span className={s.headerVivo} aria-hidden />
          <BotonMenu onClick={() => setMenuAbierto(true)} />
          <div className={s.headerTitles}>
            <h1 className={s.pageTitle}>{title}</h1>
            <div className={s.pageDate}>{fechaDeHoy()}</div>
          </div>
          {/* Es alumno pero ADEMÁS profe (dueño/staff): vuelve a su panel. */}
          {sesion?.esProfesor && (
            <button className={s.switcher} onClick={() => navigate('/dashboard')}>
              ← Panel de profe
            </button>
          )}
          <SelectorMiembro />
          <BotonTema />
        </header>

        <main className={s.content}>
          <Outlet />
        </main>
      </div>

      <BarraInferior
        items={alumnoNav.filter((item) => item.enBarra)}
        onMas={() => setMenuAbierto(true)}
      />
    </div>
    </FichaActivaProvider>
  );
}
