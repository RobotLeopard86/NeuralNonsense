import {svelte} from '@sveltejs/vite-plugin-svelte'
import tailwindcss from '@tailwindcss/vite'
import {defineConfig} from 'vite'
import mkcert from 'vite-plugin-mkcert';

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    svelte(), tailwindcss(), mkcert({autoUpgrade: true, savePath: '../.certs'})
  ],
  server: {
    proxy: {
      '/portal':
          {target: 'https://localhost:6171', ws: true, changeOrigin: true},
    }
  }
})
