import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true,
  },
  server: {
    // Lets `npm run dev` hit the ASP.NET Core backend directly, cookies and all,
    // without needing CORS. Production serves the built app from wwwroot instead.
    proxy: {
      '/api': 'http://localhost:5100',
    },
  },
})
