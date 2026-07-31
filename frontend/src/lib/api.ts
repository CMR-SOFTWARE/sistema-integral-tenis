// Cliente HTTP mínimo hacia la API .NET.
// En desarrollo, las rutas son relativas a /api: el proxy de Vite
// (vite.config.ts) las reenvía a Kestrel (localhost:5223), así el navegador
// ve un solo origen y no hay fricción de CORS. En producción (Vercel), VITE_API_URL
// apunta a la API real (Railway) y acá se arma la URL absoluta; el CORS de
// la API debe permitir el dominio del front (Cors:AllowedOrigins).
import { cerrarSesion, obtenerToken } from '../features/auth/sesion';

const BASE = `${import.meta.env.VITE_API_URL ?? ''}/api`;

// Ficha ACTIVA del portal (Capa 2, cuenta familiar): el selector del titular
// elige qué miembro se está viendo. El PortalLayout la fija; el back la lee del
// header X-Alumno-Id (si no viene, usa la ficha por defecto). Los endpoints del
// profe la ignoran, así que es inofensivo mandarla mientras hay un alumno activo.
let fichaActivaId: string | null = null;
export function setFichaActiva(id: string | null): void {
  fichaActivaId = id;
}

export class ApiError extends Error {
  status: number;
  details?: unknown;

  constructor(status: number, message: string, details?: unknown) {
    super(message);
    this.status = status;
    this.details = details;
  }
}

/**
 * Mensaje amigable cuando el error NO trae uno propio (nuestras reglas de negocio
 * mandan `detail` legible; esto cubre el resto: 500, 403, red, etc.). La idea es que
 * el usuario nunca vea un "Error 500" pelado.
 */
function mensajeAmigable(status: number): string {
  if (status === 0) return 'No pudimos conectar. Revisá tu internet e intentá de nuevo.';
  if (status === 402) return 'Tu club tiene un pago pendiente. Regularizalo para seguir.';
  if (status === 403) return 'No tenés permiso para hacer esto.';
  if (status === 404) return 'No encontramos lo que buscabas.';
  if (status === 409) return 'Ese dato ya existe o hubo un cambio; recargá e intentá de nuevo.';
  if (status >= 500) return 'Tuvimos un problema de nuestro lado. Probá de nuevo en un momento.';
  return 'Algo salió mal. Revisá los datos e intentá de nuevo.';
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = obtenerToken();

  let res: Response;
  try {
    res = await fetch(`${BASE}${path}`, {
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(fichaActivaId ? { 'X-Alumno-Id': fichaActivaId } : {}),
      },
      ...init,
    });
  } catch {
    // fetch rechaza solo por RED (server caído, sin internet), no por status HTTP.
    throw new ApiError(0, mensajeAmigable(0));
  }

  // Token vencido o inválido: a login de nuevo (si NO había token era un
  // login fallido y el error se muestra en el formulario, no acá)
  if (res.status === 401 && token) {
    cerrarSesion();
    window.location.href = '/login';
  }

  if (!res.ok) {
    // ASP.NET devuelve ProblemDetails (JSON) en errores de negocio con un `detail`
    // legible (en español): ese lo mostramos tal cual. Para el resto (500 sin detalle,
    // 403, etc.) usamos un mensaje amigable en vez del "Error 500" técnico.
    const body = await res.json().catch(() => undefined);
    const detail = (body as { detail?: string } | undefined)?.detail;
    throw new ApiError(res.status, detail ?? mensajeAmigable(res.status), body);
  }

  // 204 No Content no trae body
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  patch: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PATCH', body: JSON.stringify(body) }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
};
