namespace Pr3.ConfigAndSecurity.Config;

public static class AppSettingsValidator
{
    public static void ValidateAndThrow(AppSettings settings, bool detailedErrors)
    {
        var errors = new List<string>();

        if (settings.Port <= 0 || settings.Port > 65535)
            errors.Add($"Port must be between 1 and 65535, got {settings.Port}.");

        if (settings.ReadLimitPerMinute <= 0)
            errors.Add($"ReadLimitPerMinute must be positive, got {settings.ReadLimitPerMinute}.");

        if (settings.CreateLimitPerMinute <= 0)
            errors.Add($"CreateLimitPerMinute must be positive, got {settings.CreateLimitPerMinute}.");

        var origins = settings.GetTrustedOriginsArray();
        if (origins.Length == 0)
        {
            errors.Add("TrustedOrigins must contain at least one origin.");
        }
        else
        {
            foreach (var origin in origins)
            {
                if (origin.Contains('*'))
                {
                    errors.Add($"Trusted origin '{origin}' contains invalid wildcard.");
                    continue;
                }

                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    errors.Add($"Trusted origin '{origin}' is not a valid absolute URI.");
                    continue;
                }

                if (uri.Scheme != "http" && uri.Scheme != "https")
                    errors.Add($"Trusted origin '{origin}' must use http or https scheme.");
            }
        }

        if (errors.Count > 0)
        {
            var header = detailedErrors
                ? "Configuration validation failed. Details:"
                : "Configuration validation failed.";

            var message = detailedErrors
                ? $"{header}\n- {string.Join("\n- ", errors)}"
                : header;

            throw new InvalidOperationException(message);
        }
    }
}
