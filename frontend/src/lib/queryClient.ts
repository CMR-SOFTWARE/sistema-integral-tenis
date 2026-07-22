import { QueryClient } from '@tanstack/react-query';

/**
 * Cliente único de React Query. Guarda en memoria lo que ya se pidió, así volver
 * a una sección ya visitada la muestra al instante (desde el caché) mientras
 * revalida en segundo plano — se acaba el "pantalla en blanco + spinner" en cada
 * navegación. Las mutaciones invalidan las keys afectadas para reflejar los cambios.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Dentro de esta ventana no se vuelve a pedir (navegación instantánea);
      // pasada, se revalida en segundo plano mostrando primero el caché.
      staleTime: 15_000,
      // Cuánto sobrevive en memoria un dato sin usarse antes de descartarse.
      gcTime: 5 * 60_000,
      // No re-pedir solo por volver a la pestaña (evita pedidos sorpresa en mobile).
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});
