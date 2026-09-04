using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Infrastructure.Exceptions
{
    internal sealed class ExceptionMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (ValidationException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.Headers["content-type"] = "application/json";

                var response = new
                {
                    ErrorCode = "validation_failed",
                    Errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers["content-type"] = "application/json";

                var response = new
                {
                    ErrorCode = "unauthorized",
                    Message = "User is not authenticated."
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Format exception occurred");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.Headers["content-type"] = "application/json";

                var response = new
                {
                    ErrorCode = "invalid_format",
                    Message = "The request contains an invalid format."
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (DbUpdateConcurrencyException)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.Headers["content-type"] = "application/json";

                var response = new
                {
                    ErrorCode = "concurrency_conflict",
                    Message = "The resource was modified by another operation. Please retry."
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update exception");

                var statusCode = StatusCodes.Status409Conflict;
                var errorCode = "database_error";
                var message = "A database error occurred. Please retry.";

                if (ex.InnerException is Npgsql.NpgsqlException pgEx && pgEx.SqlState == "23505")
                {
                    errorCode = "unique_violation";
                    message = "A resource with the same key already exists.";
                }

                context.Response.StatusCode = statusCode;
                context.Response.Headers["content-type"] = "application/json";

                var response = new { ErrorCode = errorCode, Message = message };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain exception: {Field}", ex.Field);
                context.Response.StatusCode = (int)ex.StatusCode;
                context.Response.Headers["content-type"] = "application/json";
                var errorCode = ToUnderscoreCase(ex.GetType().Name.Replace("Exception", string.Empty));
                var response = new
                {
                    ErrorCode = errorCode,
                    Message = ex.Message
                };
                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.Headers["content-type"] = "application/json";

                var response = new
                {
                    ErrorCode = "internal_server_error",
                    Message = "An unexpected error occurred. Please try again later."
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }

        public static string ToUnderscoreCase(string? value)
            => string.Concat((value ?? string.Empty).Select((x, i) => i > 0 && char.IsUpper(x) && value is not null && !char.IsUpper(value[i - 1]) ? $"_{x}" : x.ToString())).ToLower();
    }
}
