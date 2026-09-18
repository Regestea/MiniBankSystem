import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Mobile web output is bundled into the Capacitor Android shell (`npx cap sync`).
export default defineConfig({
  plugins: [react()],
  server: { port: 5173 },
  build: { outDir: "dist" },
});
