import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Mobile web output is bundled into the Capacitor Android shell (`npx cap sync`).
// PORT is set by Aspire (AddViteApp) — fall back to 5173 for plain `npm run dev`
// so the API CORS defaults (3000/5173) keep working in both modes.
const port = Number(process.env.PORT ?? 5173);

export default defineConfig({
  plugins: [react()],
  server: { port, strictPort: false },
  preview: { port },
  build: { outDir: "dist" },
});
