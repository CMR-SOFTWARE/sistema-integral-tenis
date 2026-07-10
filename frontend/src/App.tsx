import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom';
import AppLayout from './components/layout/AppLayout';
import Placeholder from './components/Placeholder';
import AlumnosPage from './features/alumnos/AlumnosPage';
import LoginPage from './features/auth/LoginPage';
import DashboardPage from './features/dashboard/DashboardPage';
import GruposPage from './features/grupos/GruposPage';
import CalendarioPage from './features/agenda/CalendarioPage';
import HorariosPage from './features/agenda/HorariosPage';
import ConfiguracionPage from './features/agenda/ConfiguracionPage';
import CuotasPage from './features/cuotas/CuotasPage';

/**
 * Guardián del shell: sin "sesión" demo elegida, va al login. Cuando haya
 * auth real (JWT), este componente valida el token en vez del sessionStorage.
 */
function RequiereRol() {
  return sessionStorage.getItem('demoRol') === 'profesor'
    ? <Outlet />
    : <Navigate to="/login" replace />;
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />

        <Route element={<RequiereRol />}>
          <Route element={<AppLayout />}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/alumnos" element={<AlumnosPage />} />
            <Route path="/calendario" element={<CalendarioPage />} />
            <Route path="/grupos" element={<GruposPage />} />
            <Route path="/horarios" element={<HorariosPage />} />
            <Route path="/cuotas" element={<CuotasPage />} />
            <Route path="/bloqueos" element={<Placeholder titulo="Bloqueos" />} />
            <Route path="/cancelaciones" element={<Placeholder titulo="Cancelaciones" />} />
            <Route path="/reportes" element={<Placeholder titulo="Reportes" />} />
            <Route path="/configuracion" element={<ConfiguracionPage />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
