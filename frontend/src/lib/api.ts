// Cliente HTTP mínimo hacia la API .NET.
// Las rutas son relativas a /api: el proxy de Vite (vite.config.ts) las
// reenvía a Kestrel (localhost:5223) en desarrollo, así el navegador ve
// un solo origen y no hay fricción de CORS en dev.

const BASE = '/api';

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
  const res = await fetch(`${BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  });

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
  patch: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PATCH', body: JSON.stringify(body) }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
};
