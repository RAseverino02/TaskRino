using Microsoft.AspNetCore.Mvc;

namespace GestorTareas.Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (UnauthorizedAccessException exception)
        {
            await WriteProblem(context, StatusCodes.Status401Unauthorized, "No autorizado", exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error no controlado procesando {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblem(context, StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado",
                "No pudimos completar la solicitud. Inténtalo nuevamente.");
        }
    }

    private static async Task WriteProblem(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = title, Detail = detail, Instance = context.Request.Path
        });
    }
}
