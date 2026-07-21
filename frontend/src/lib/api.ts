// Cliente HTTP mínimo hacia la API .NET.
// En desarrollo, las rutas son relativas a /api: el proxy de Vite
// (vite.config.ts) las reenvía a Kestrel (localhost:5223), así el navegador
// ve un solo origen y no hay fricción de CORS. En producción (Vercel), VITE_API_URL
// apunta a la API real (Railway) y acá se arma la URL absoluta; el CORS de
// la API debe permitir el dominio del front (Cors:AllowedOrigins).
import { cerrarSesion, obtenerToken } from '../features/auth/sesion';

const BASE = `${import.meta.env.VITE_API_URL ?? ''}/api`;

export class ApiError extends Error {
  status: number;
  details?: unknown;

  constructor(status: number, message: string, details?: unknown) {
    super(message);
    this.status = status;
    this.details = details;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = obtenerToken();
  const res = await fetch(`${BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    ...init,
  });

  // Token vencido o inválido: a login de nuevo (si NO había token era un
  // login fallido y el error se muestra en el formulario, no acá)
  if (res.status === 401 && token) {
    cerrarSesion();
    window.location.href = '/login';
  }

  if (!res.ok) {
    // ASP.NET devuelve ProblemDetails (JSON) en errores; intentamos leerlo
    const body = await res.json().catch(() => undefined);
    const message =
      (body as { title?: string; detail?: string } | undefined)?.detail ??
      (body as { title?: string } | undefined)?.title ??
      `Error ${res.status}`;
    throw new ApiError(res.status, message, body);
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
