import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Todo lo que empiece con /api se reenvía a la API .NET (Kestrel).
    // Así el navegador ve un solo origen en desarrollo.
    proxy: {
      '/api': {
        target: 'http://localhost:5223',
        changeOrigin: true,
      },
    },
  },
})
