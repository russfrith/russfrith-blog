import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 5173,
    strictPort: true,
    // Windows bind-mount file-change events don't reliably reach the Linux
    // dev container - usePolling is Vite's equivalent of Angular's --poll.
    watch: { usePolling: true, interval: 1000 },
    proxy: { '/api': { target: 'http://api:8080', changeOrigin: true } },
  },
})
