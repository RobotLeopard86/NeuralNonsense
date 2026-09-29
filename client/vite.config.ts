import { defineConfig } from "vite"
import { svelte } from "@sveltejs/vite-plugin-svelte"
import mkcert from "vite-plugin-mkcert";
import tailwindcss from "@tailwindcss/vite"

// https://vite.dev/config/
export default defineConfig({
	plugins: [svelte(), tailwindcss(), mkcert()],
	server: {
		proxy: {
			"/hub": {
				target: "https://localhost:6171",
				ws: true,
				changeOrigin: true
			},
			"/api": {
				target: "https://localhost:6171",
				ws: true,
				changeOrigin: true
			}
		}
	}
})
