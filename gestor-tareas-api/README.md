# Gestor Tareas API

API REST en ASP.NET Core 8 para TaskRino. Usa EF Core + PostgreSQL, JWT Bearer con refresh tokens rotativos, autorización por membresía del proyecto, ProblemDetails y Swagger.

## Desarrollo

Desde la raíz del monorepo:

```powershell
docker compose up -d postgres
dotnet run --project gestor-tareas-api
```

La migración inicial está versionada en `Data/Migrations`. La API la aplica y ejecuta la semilla al iniciar.

## Configuración

Las claves usan el formato estándar de configuración de .NET. En producción se definen como variables de entorno; ver `.env.example`. `Storage__Provider=Local` sirve para desarrollo. En Render debe ser `S3` con Supabase Storage para persistencia.

## Rutas principales

- `/api/v1/auth/*`: registro, login, refresh y logout.
- `/api/v1/proyectos/*`: proyectos, miembros y roles.
- `/api/v1/proyectos/{id}/tareas`: tareas paginadas y filtradas.
- `/api/v1/tareas/*`: detalle, edición, estado, comentarios y adjuntos.
- `/swagger`: documentación interactiva con botón Authorize.
- `/health`: comprobación de disponibilidad.

Las URLs de producción se completan en el README raíz después del despliegue.
