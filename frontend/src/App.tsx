import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import AppLayout from './components/layout/AppLayout';
import Placeholder from './components/Placeholder';
import AlumnosPage from './features/alumnos/AlumnosPage';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<AppLayout />}>
          <Route index element={<Navigate to="/alumnos" replace />} />
          <Route path="/dashboard" element={<Placeholder titulo="Dashboard" />} />
          <Route path="/alumnos" element={<AlumnosPage />} />
          <Route path="/calendario" element={<Placeholder titulo="Calendario" />} />
          <Route path="/grupos" element={<Placeholder titulo="Grupos" />} />
          <Route path="/horarios" element={<Placeholder titulo="Horarios" />} />
          <Route path="/cuotas" element={<Placeholder titulo="Cuotas" />} />
          <Route path="/bloqueos" element={<Placeholder titulo="Bloqueos" />} />
          <Route path="/cancelaciones" element={<Placeholder titulo="Cancelaciones" />} />
          <Route path="/reportes" element={<Placeholder titulo="Reportes" />} />
          <Route path="/configuracion" element={<Placeholder titulo="Configuración" />} />
          <Route path="*" element={<Navigate to="/alumnos" replace />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
