import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// When running inside Docker the API container is reachable as http://api:5000.
// Locally (npm run dev) it runs on http://localhost:5209.
const API_TARGET = process.env.VITE_API_TARGET ?? 'http://localhost:5209'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: true, // needed so Docker can expose the port
    proxy: {
      '/auth':      { target: API_TARGET, changeOrigin: true },
      '/accounts':  { target: API_TARGET, changeOrigin: true },
      '/transfers': { target: API_TARGET, changeOrigin: true },
      '/admin':     { target: API_TARGET, changeOrigin: true },
    },
  },
})
