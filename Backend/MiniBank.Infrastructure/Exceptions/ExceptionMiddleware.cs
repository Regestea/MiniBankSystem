using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Infrastructure.Exceptions
{
    internal sealed class ExceptionMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (ValidationException ex) // FluentValidation — from MiniMediator pipeline
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
            catch (DomainException ex)
            {
                context.Response.StatusCode = (int)ex.StatusCode;
                context.Response.Headers["content-type"] = "application/json";
                var errorCode = ToUnderscoreCase(ex.GetType().Name.Replace("Exception", string.Empty));
                var response = new
                {
                    ErrorCode = errorCode,
                    Message = $"Error for {ex.Field} : {ex.Details}"
                };
                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);
            }
        }

        public static string ToUnderscoreCase(string? value)
            => string.Concat((value ?? string.Empty).Select((x, i) => i > 0 && char.IsUpper(x) && value is not null && !char.IsUpper(value[i - 1]) ? $"_{x}" : x.ToString())).ToLower();
    }
}
