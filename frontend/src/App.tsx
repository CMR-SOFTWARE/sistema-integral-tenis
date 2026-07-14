import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom';
import AppLayout from './components/layout/AppLayout';
import AlumnosPage from './features/alumnos/AlumnosPage';
import LoginPage from './features/auth/LoginPage';
import { obtenerSesion, obtenerToken } from './features/auth/sesion';
import DashboardPage from './features/dashboard/DashboardPage';
import GruposPage from './features/grupos/GruposPage';
import CalendarioPage from './features/agenda/CalendarioPage';
import HorariosPage from './features/agenda/HorariosPage';
import ConfiguracionPage from './features/agenda/ConfiguracionPage';
import CuotasPage from './features/cuotas/CuotasPage';
import BloqueosPage from './features/bloqueos/BloqueosPage';
import CancelacionesPage from './features/cancelaciones/CancelacionesPage';
import ReportesPage from './features/reportes/ReportesPage';
import PortalLayout from './features/portal/PortalLayout';
import InicioPage from './features/portal/InicioPage';
import MisTurnosPage from './features/portal/MisTurnosPage';
import MiCuotaPage from './features/portal/MiCuotaPage';
import PerfilPage from './features/portal/PerfilPage';
import BuscarClubPage from './features/portal/BuscarClubPage';

/** Gestión: hace falta token Y membresía de profesor (JWT real). */
function RequiereProfesor() {
  return obtenerToken() && obtenerSesion()?.esProfesor
    ? <Outlet />
    : <Navigate to="/login" replace />;
}

/** Portal: alcanza con ser jugador logueado (con o SIN ficha vinculada —
 *  el portal muestra el estado "sin club" y deja elegir profe). */
function RequiereJugador() {
  const sesion = obtenerSesion();
  if (!obtenerToken() || !sesion) return <Navigate to="/login" replace />;
  if (sesion.esProfesor) return <Navigate to="/dashboard" replace />;
  return <Outlet />;
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />

        <Route element={<RequiereProfesor />}>
          <Route element={<AppLayout />}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/alumnos" element={<AlumnosPage />} />
            <Route path="/calendario" element={<CalendarioPage />} />
            <Route path="/grupos" element={<GruposPage />} />
            <Route path="/horarios" element={<HorariosPage />} />
            <Route path="/cuotas" element={<CuotasPage />} />
            <Route path="/bloqueos" element={<BloqueosPage />} />
            <Route path="/cancelaciones" element={<CancelacionesPage />} />
            <Route path="/reportes" element={<ReportesPage />} />
            <Route path="/configuracion" element={<ConfiguracionPage />} />
          </Route>
        </Route>

        <Route element={<RequiereJugador />}>
          <Route path="/portal" element={<PortalLayout />}>
            <Route index element={<InicioPage />} />
            <Route path="turnos" element={<MisTurnosPage />} />
            <Route path="cuota" element={<MiCuotaPage />} />
            <Route path="perfil" element={<PerfilPage />} />
            <Route path="club" element={<BuscarClubPage />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
