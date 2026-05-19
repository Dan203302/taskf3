using System.Collections.Concurrent;

namespace Pr3.ConfigAndSecurity.Middlewares;

public class RateLimitingMiddleware(RequestDelegate next, int readLimit, int createLimit)
{
    private readonly ConcurrentDictionary<string, (int count, DateTime resetTime)> _store = new();

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = context.Request.Method;
        var clientId = GetClientIdentifier(context);

        int limit;
        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) || path.Contains("/create"))
            limit = createLimit;
        else
            limit = readLimit;

        var now = DateTime.UtcNow;
        var entry = _store.GetOrAdd(clientId, _ => (0, now.AddMinutes(1)));

        if (now >= entry.resetTime)
        {
            entry = (0, now.AddMinutes(1));
            _store[clientId] = entry;
        }

        if (entry.count >= limit)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Append("Retry-After", ((int)(entry.resetTime - now).TotalSeconds).ToString());
            await context.Response.WriteAsync("Rate limit exceeded.");
            return;
        }

        _store[clientId] = (entry.count + 1, entry.resetTime);
        await next(context);
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        return $"{ip}:{path}";
    }
}
