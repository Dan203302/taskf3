namespace Pr3.ConfigAndSecurity.Config;

public static class ConfigurationPriority
{
    public static void ApplyPriority(WebApplicationBuilder builder, string[] args)
    {
        builder.Configuration.AddEnvironmentVariables(prefix: "APP_");
        builder.Configuration.AddCommandLine(args);
    }
}
