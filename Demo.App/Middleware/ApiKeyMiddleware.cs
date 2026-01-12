using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Demo.App.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _apiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _apiKey = config["Api:ApiKey"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate for API endpoints
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            // Allow CORS preflight through
            if (string.Equals(context.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (string.IsNullOrEmpty(_apiKey))
            {
                // No API key configured - reject
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API key not configured");
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var providedKey) || string.IsNullOrEmpty(providedKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing API key");
                return;
            }

            if (!string.Equals(providedKey.ToString(), _apiKey, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid API key");
                return;
            }
        }

        await _next(context);
    }
}
