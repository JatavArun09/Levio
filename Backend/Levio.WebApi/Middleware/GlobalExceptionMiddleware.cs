using Levio.WebApi.DTOs;
using Levio.SharedKernel.Domain;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace Levio.WebApi.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted) throw;
                if (ex is UnauthorizedAccessException or KeyNotFoundException or ArgumentException or DomainException or ValidationException)
                    _logger.LogWarning("Request rejected: {Message}", ex.Message);
                else
                    _logger.LogError(ex, "Unhandled exception occurred");

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            var response = new ErrorResponseDto
            {
                Timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case ValidationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Request validation failed.";
                    break;
                case ResourceForbiddenException:
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    response.Message = exception.Message;
                    break;
                case RequestConflictException:
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    response.Message = exception.Message;
                    break;
                case DomainException:
                    response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                    response.Message = exception.Message;
                    break;
                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = exception.Message;
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = exception.Message;
                    break;

                case ArgumentException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = exception.Message;
                    break;

                default:
                    response.StatusCode =
                        (int)HttpStatusCode.InternalServerError;

                    response.Message =
                        "An unexpected error occurred.";
                    break;
            }

            context.Response.ContentType =
                "application/json";

            context.Response.StatusCode =
                response.StatusCode;

            if (context.Request.Path.StartsWithSegments("/api/v1"))
            {
                var errors = exception is ValidationException validation
                    ? validation.Errors.Select(e => e.ErrorMessage).ToArray() : new[] { response.Message };
                await context.Response.WriteAsJsonAsync(new { success = false, message = response.Message, data = (object?)null, errors });
                return;
            }

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
    }
}
