import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from './lib/queryClient';
import { ConfirmarProvider } from './components/confirmar/ConfirmarProvider';
import AppLayout from './components/layout/AppLayout';
import AlumnosPage from './features/alumnos/AlumnosPage';
import LoginPage from './features/auth/LoginPage';
import RegistroPage from './features/auth/RegistroPage';
import RegistroJugadorPage from './features/auth/RegistroJugadorPage';
import RegistroProfesorPage from './features/auth/RegistroProfesorPage';
import CheckoutPage from './features/auth/CheckoutPage';
import CambiarPasswordPage from './features/auth/CambiarPasswordPage';
import SolicitudesPage from './features/solicitudes/SolicitudesPage';
import { obtenerSesion, obtenerToken } from './features/auth/sesion';
import DashboardPage from './features/dashboard/DashboardPage';
import StaffDashboardPage from './features/staff/StaffDashboardPage';
import GruposPage from './features/grupos/GruposPage';
import ProfesoresPage from './features/profesores/ProfesoresPage';
import PlataformaPage from './features/admin/PlataformaPage';
import CalendarioPage from './features/agenda/CalendarioPage';
import HorariosPage from './features/agenda/HorariosPage';
import ConfiguracionPage from './features/agenda/ConfiguracionPage';
import CuotasPage from './features/cuotas/CuotasPage';
import AvisosPage from './features/avisos/AvisosPage';
import BloqueosPage from './features/bloqueos/BloqueosPage';
import CancelacionesPage from './features/cancelaciones/CancelacionesPage';
import ReportesPage from './features/reportes/ReportesPage';
import PortalLayout from './features/portal/PortalLayout';
import InicioPage from './features/portal/InicioPage';
import MisTurnosPage from './features/portal/MisTurnosPage';
import ReservarPage from './features/portal/ReservarPage';
import MiCuotaPage from './features/portal/MiCuotaPage';
import ServiciosPage from './features/portal/ServiciosPage';
import PerfilPage from './features/portal/PerfilPage';
import BuscarClubPage from './features/portal/BuscarClubPage';

/** Gestión: hace falta token Y membresía de profesor (JWT real). */
function RequiereProfesor() {
  const sesion = obtenerSesion();
  if (!obtenerToken() || !sesion) return <Navigate to="/login" replace />;
  return sesion.esProfesor ? <Outlet /> : <Navigate to="/login" replace />;
}

/** Portal: alcanza con ser jugador logueado (con o SIN ficha vinculada —
 *  el portal muestra el estado "sin club" y deja solicitar un profe). */
function RequiereJugador() {
  const sesion = obtenerSesion();
  if (!obtenerToken() || !sesion) return <Navigate to="/login" replace />;
  if (sesion.esProfesor) return <Navigate to="/dashboard" replace />;
  if (sesion.estadoTenant === 'PendientePago') return <Navigate to="/checkout" replace />;
  return <Outlet />;
}

/** Checkout: solo para el profe registrado que todavía no pagó. */
function RequiereCheckout() {
  const sesion = obtenerSesion();
  if (!obtenerToken() || !sesion) return <Navigate to="/login" replace />;
  if (sesion.estadoTenant !== 'PendientePago') {
    return <Navigate to={sesion.esProfesor ? '/dashboard' : '/portal'} replace />;
  }
  return <Outlet />;
}

/** Cambio de contraseña: cualquier logueado (obligatorio si nació con temporal). */
function RequiereLogueado() {
  return obtenerToken() ? <Outlet /> : <Navigate to="/login" replace />;
}

/** Landing del panel de gestión: siempre /dashboard (que se adapta al rol). */
function InicioProfesor() {
  return <Navigate to="/dashboard" replace />;
}

/** El dashboard se adapta al rol: el dueño ve el del negocio; el staff, el suyo (su semana). */
function Dashboard() {
  return obtenerSesion()?.rol === 'staff' ? <StaffDashboardPage /> : <DashboardPage />;
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
    <BrowserRouter>
      <ConfirmarProvider>
        <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/registro" element={<RegistroPage />} />
        <Route path="/registro/jugador" element={<RegistroJugadorPage />} />
        <Route path="/registro/profesor" element={<RegistroProfesorPage />} />
        <Route element={<RequiereCheckout />}>
          <Route path="/checkout" element={<CheckoutPage />} />
        </Route>
        <Route element={<RequiereLogueado />}>
          <Route path="/cambiar-password" element={<CambiarPasswordPage />} />
        </Route>

        <Route element={<RequiereProfesor />}>
          <Route element={<AppLayout />}>
            <Route index element={<InicioProfesor />} />
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/alumnos" element={<AlumnosPage />} />
            <Route path="/calendario" element={<CalendarioPage />} />
            <Route path="/grupos" element={<GruposPage />} />
            <Route path="/profesores" element={<ProfesoresPage />} />
            <Route path="/horarios" element={<HorariosPage />} />
            <Route path="/cuotas" element={<CuotasPage />} />
            <Route path="/avisos" element={<AvisosPage />} />
            <Route path="/bloqueos" element={<BloqueosPage />} />
            <Route path="/cancelaciones" element={<CancelacionesPage />} />
            <Route path="/solicitudes" element={<SolicitudesPage />} />
            <Route path="/reportes" element={<ReportesPage />} />
            <Route path="/configuracion" element={<ConfiguracionPage />} />
            <Route path="/plataforma" element={<PlataformaPage />} />
          </Route>
        </Route>

        <Route element={<RequiereJugador />}>
          <Route path="/portal" element={<PortalLayout />}>
            <Route index element={<InicioPage />} />
            <Route path="turnos" element={<MisTurnosPage />} />
            <Route path="reservar" element={<ReservarPage />} />
            <Route path="cuota" element={<MiCuotaPage />} />
            <Route path="servicios" element={<ServiciosPage />} />
            <Route path="perfil" element={<PerfilPage />} />
            <Route path="club" element={<BuscarClubPage />} />
          </Route>
        </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </ConfirmarProvider>
    </BrowserRouter>
    </QueryClientProvider>
  );
}
