import path from "node:path";
import react from "@vitejs/plugin-react-swc";
import { defineConfig } from "vite";

// https://vite.dev/config/
export default defineConfig({
	plugins: [react()],
	server: {
		port: 3280,
	},
	preview: {
		port: 3280,
	},
	resolve: {
		alias: {
			"@": path.resolve(__dirname, "./src"),
			"@modules": path.resolve(__dirname, "./src/modules"),
			"@shared": path.resolve(__dirname, "./src/shared"),
			"@store": path.resolve(__dirname, "./src/store"),
			"@routes": path.resolve(__dirname, "./src/routes"),
			"@utils": path.resolve(__dirname, "./src/utils"),
			"@teamsaibot/teamsaibot-rpcs": path.resolve(__dirname, "../ts-artifacts/"),
		},
	},
});
