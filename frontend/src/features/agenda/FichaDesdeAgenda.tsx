import { useEffect, useState } from 'react';
import DetalleAlumnoModal from '../alumnos/DetalleAlumnoModal';
import EditarAlumnoModal from '../alumnos/EditarAlumnoModal';
import { useAlumnos } from '../alumnos/useAlumnos';
import type { Alumno } from '../alumnos/types';
import { obtenerSesion } from '../auth/sesion';

interface Props {
  alumnoId: string;
  onClose: () => void;
}

/**
 * La ficha del alumno abierta DESDE una clase del calendario. Encapsula lo que la
 * agenda no tiene a mano: la lista de alumnos (de ahí salen sus hermanos de cuenta
 * familiar) y las acciones de edición. Reusa los mismos modales que el listado de
 * Alumnos, así la ficha es una sola en toda la app.
 */
export default function FichaDesdeAgenda({ alumnoId, onClose }: Props) {
  // Ya suele estar en caché de React Query si el profe pasó por Alumnos.
  const { alumnos, cargando, editar, cambiarProfe } = useAlumnos('todas');
  const [editando, setEditando] = useState<Alumno | null>(null);
  // Mismos permisos que en el listado: el staff mira, el dueño edita.
  const esOwner = obtenerSesion()?.rol === 'owner';

  const alumno = alumnos.find((a) => a.id === alumnoId);

  // La ficha ya no está (se borró mientras el turno estaba abierto): se cierra sola.
  // Va en un efecto y no en el render, para no avisarle al padre en pleno pintado.
  const noExiste = !cargando && !alumno;
  useEffect(() => {
    if (noExiste) onClose();
  }, [noExiste, onClose]);

  if (editando) {
    return (
      <EditarAlumnoModal
        alumno={editando}
        onClose={() => { setEditando(null); onClose(); }}
        onEditar={editar}
      />
    );
  }

  // Mientras carga la lista no se muestra nada: con caché es instantáneo.
  if (!alumno) return null;

  const hermanos = alumno.familiaId
    ? alumnos.filter((a) => a.familiaId === alumno.familiaId && a.id !== alumno.id)
    : [];

  return (
    <DetalleAlumnoModal
      alumno={alumno}
      hermanos={hermanos}
      onClose={onClose}
      onCambiarProfe={esOwner ? cambiarProfe : undefined}
      onEditar={esOwner ? setEditando : undefined}
    />
  );
}
