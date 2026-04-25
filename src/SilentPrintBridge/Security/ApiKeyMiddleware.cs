namespace SilentPrintBridge.Security;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, Services.ConfigService configService)
    {
        var config = configService.GetConfig();

        // Skip API key check if not required
        if (!config.Server.RequireApiKey)
        {
            await _next(context);
            return;
        }

        // Skip for health endpoint
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        // Check for API key header
        if (!context.Request.Headers.TryGetValue("X-SilentPrintBridge-Key", out var providedKey))
        {
            _logger.LogWarning("API key required but not provided. Path: {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                error = "API key required",
                errorCode = "API_KEY_REQUIRED"
            });
            return;
        }

        // Validate API key
        if (providedKey != config.Server.ApiKey)
        {
            _logger.LogWarning("Invalid API key provided. Path: {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                error = "Invalid API key",
                errorCode = "INVALID_API_KEY"
            });
            return;
        }

        await _next(context);
    }
}
