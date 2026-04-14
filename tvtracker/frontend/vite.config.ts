import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'


export default defineConfig({
  server: {
    proxy: {
      '/api': {
        // Change 'localhost' to 'backend' (the service name in docker-compose)
        target: 'http://backend:5000', 
        changeOrigin: true,
        secure: false,
      }
    }
  }
})