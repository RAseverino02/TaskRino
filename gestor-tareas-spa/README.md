# Gestor Tareas SPA

Cliente React de TaskRino construido con Vite. Incluye Context API para autenticación, Axios con refresh automático, React Router, rutas protegidas y una interfaz Kanban responsiva con identidad visual azul marino, turquesa y dorado.

## Desarrollo

```powershell
Copy-Item .env.example .env
npm install
npm run dev
```

`VITE_API_URL` debe terminar en `/api/v1`, por ejemplo `http://localhost:5080/api/v1`.

## Producción

Importar esta carpeta como Root Directory en Vercel Hobby y configurar `VITE_API_URL` con la API pública. `vercel.json` garantiza que las rutas del SPA regresen a `index.html`.

La URL pública se completa en el README raíz después del despliegue.
