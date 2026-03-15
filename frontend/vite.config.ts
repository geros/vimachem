import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api/parties': {
        target: 'http://localhost:5100',
        changeOrigin: true,
      },
      '/api/catalog': {
        target: 'http://localhost:5200',
        changeOrigin: true,
      },
      '/api/lending': {
        target: 'http://localhost:5300',
        changeOrigin: true,
      },
      '/api/events': {
        target: 'http://localhost:5400',
        changeOrigin: true,
      },
    },
  },
})
