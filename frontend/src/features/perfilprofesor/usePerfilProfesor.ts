import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../lib/api';
import { comprimirABlob } from '../portal/comprimirImagen';
import type {
  FotoPerfil, GuardarHito, GuardarPerfil, HitoTrayectoria,
  MiPerfilProfesor, PerfilProfesorPublico, ProfesorTarjeta,
} from './types';

const MIO = ['perfil-profesor', 'mio'];

/** El perfil del profe logueado (el back devuelve uno vacío si todavía no cargó nada). */
export function useMiPerfil() {
  return useQuery({
    queryKey: MIO,
    queryFn: () => api.get<MiPerfilProfesor>('/perfil-profesor/mio'),
  });
}

/**
 * Las acciones de edición. Cada una invalida el perfil para que la pantalla se
 * refresque sola; el mismo patrón que usan cuotas y grupos.
 */
export function useEditarPerfil() {
  const qc = useQueryClient();
  const refrescar = () => qc.invalidateQueries({ queryKey: MIO });

  /** Comprime en el navegador y sube el archivo: al storage llega ~200 KB, no la foto de 8 MB. */
  const subirImagen = async (path: string, file: File, maxAncho: number, extra?: Record<string, string>) => {
    const blob = await comprimirABlob(file, maxAncho);
    const form = new FormData();
    form.append('archivo', blob, 'foto.jpg');
    for (const [clave, valor] of Object.entries(extra ?? {})) form.append(clave, valor);
    const res = await api.postForm<unknown>(path, form);
    await refrescar();
    return res;
  };

  return {
    guardar: async (datos: GuardarPerfil) => {
      const perfil = await api.put<MiPerfilProfesor>('/perfil-profesor/mio', datos);
      qc.setQueryData(MIO, perfil); // ya vino el perfil entero: no hace falta re-pedirlo
      return perfil;
    },

    subirPortada: (file: File) => subirImagen('/perfil-profesor/mio/portada', file, 1600),
    subirAvatar: (file: File) => subirImagen('/perfil-profesor/mio/avatar', file, 500),
    quitarPortada: async () => { await api.delete('/perfil-profesor/mio/portada'); await refrescar(); },
    quitarAvatar: async () => { await api.delete('/perfil-profesor/mio/avatar'); await refrescar(); },

    agregarFoto: (file: File, pieDeFoto: string) =>
      subirImagen('/perfil-profesor/mio/fotos', file, 1400, { pieDeFoto }) as Promise<FotoPerfil>,
    cambiarPie: async (id: string, pieDeFoto: string | null) => {
      await api.patch(`/perfil-profesor/mio/fotos/${id}`, { pieDeFoto });
      await refrescar();
    },
    eliminarFoto: async (id: string) => {
      await api.delete(`/perfil-profesor/mio/fotos/${id}`);
      await refrescar();
    },
    reordenarFotos: async (ids: string[]) => {
      await api.put('/perfil-profesor/mio/fotos/orden', { ids });
      await refrescar();
    },

    agregarHito: async (hito: GuardarHito) => {
      const creado = await api.post<HitoTrayectoria>('/perfil-profesor/mio/hitos', hito);
      await refrescar();
      return creado;
    },
    editarHito: async (id: string, hito: GuardarHito) => {
      await api.put(`/perfil-profesor/mio/hitos/${id}`, hito);
      await refrescar();
    },
    eliminarHito: async (id: string) => {
      await api.delete(`/perfil-profesor/mio/hitos/${id}`);
      await refrescar();
    },
    reordenarHitos: async (ids: string[]) => {
      await api.put('/perfil-profesor/mio/hitos/orden', { ids });
      await refrescar();
    },
  };
}

// ── Lo que ve el alumno ──

/** Los profes de un club (el suyo, o uno que está mirando antes de unirse). */
export function useProfesoresDelClub(tenantId: string | undefined) {
  return useQuery({
    queryKey: ['profesores-club', tenantId],
    queryFn: () => api.get<ProfesorTarjeta[]>(`/publico/clubes/${tenantId}/profesores`),
    enabled: !!tenantId,
  });
}

export function usePerfilPublico(tenantId: string | undefined, userId: string | undefined) {
  return useQuery({
    queryKey: ['perfil-publico', tenantId, userId],
    queryFn: () => api.get<PerfilProfesorPublico>(`/publico/clubes/${tenantId}/profesores/${userId}`),
    enabled: !!tenantId && !!userId,
  });
}
