using Pr3.ConfigAndSecurity.Config;

namespace Pr3.ConfigAndSecurity.Middlewares;

public class OriginValidationMiddleware(RequestDelegate next, AppSettings settings)
{
    private readonly HashSet<string> _trustedOrigins = new(
        settings.GetTrustedOriginsArray().Select(o => o.ToLowerInvariant()));

    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();

        if (!string.IsNullOrEmpty(origin) && !_trustedOrigins.Contains(origin.ToLowerInvariant()))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Untrusted origin.");
            return;
        }

        await next(context);
    }
}
