import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api': {
        // Allow API URL to be configurable via environment variable
        // Default to localhost for development, can be overridden with VITE_API_URL
        target: process.env.VITE_API_URL || 'http://127.0.0.1:8081',
        changeOrigin: true,
        headers: {
          'X-API-KEY': 'tuclaveenbase64'
        }
      },
    },
  },
})
