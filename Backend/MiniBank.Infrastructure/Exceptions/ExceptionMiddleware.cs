using System.Text.Json;
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
