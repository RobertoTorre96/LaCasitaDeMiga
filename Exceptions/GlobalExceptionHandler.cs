using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace LaCasitaDeMiga.Exceptions {
    public class GlobalExceptionHandler : IExceptionHandler {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) {
            _logger = logger;
        }
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken) {


            // 1. Tu patrón de coincidencia (Pattern Matching) ultra limpio
            var (statusCode, title) = exception switch {
                NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
                AlreadyExistsException => (StatusCodes.Status409Conflict, "Conflicto: El recurso ya existe"),
                ValidationException => (StatusCodes.Status400BadRequest, "Error de validación"),
                BadRequestException => (StatusCodes.Status400BadRequest, "Solicitud incorrecta"),
                Google.Apis.Auth.InvalidJwtException => (StatusCodes.Status401Unauthorized, "Token de Google inválido o expiró"),

                // Cualquier otro error no controlado (como fallos de Postgres/Docker) cae aquí
                _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
            };

            // 2. LOGGEO INTELIGENTE: Decidimos qué nivel de log usar según el código de estado
            if (statusCode == StatusCodes.Status500InternalServerError) {
                // Si es un error 500, SÍ queremos ver todo el desastre y el Stack Trace en consola
                _logger.LogError(exception, "CRÍTICO: Ocurrió una excepción no controlada en el servidor: {Message}", exception.Message);
            } else {
                // Si es un 404, 400, etc., solo registramos una advertencia (Warning) limpia de una sola línea
                _logger.LogWarning("Controlado ({StatusCode}): {Message}", statusCode, exception.Message);
            }

            // 3. Armamos el formato estándar ProblemDetails protegiendo mensajes del sistema (Error 500)
            var problemDetails = new ProblemDetails {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError
                    ? "Ocurrió un error inesperado en el servidor. Por favor, intente más tarde."
                    : exception.Message,
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
            };

            // 4. Seteamos el código de estado en la respuesta HTTP
            httpContext.Response.StatusCode = statusCode;

            // 5. Enviamos el JSON de vuelta al cliente (Postman / Frontend)
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            // Retornamos true para avisarle a .NET que ya manejamos la excepción y el pipeline terminó
            return true;


        }
    }
}
