namespace Pr3.ConfigAndSecurity.Config;

public enum AppMode
{
    Study,
    Production
}

public class AppSettings
{
    public const string SectionName = "AppSettings";

    public AppMode Mode { get; set; } = AppMode.Study;

    public string TrustedOrigins { get; set; } = string.Empty;

    public int ReadLimitPerMinute { get; set; } = 60;

    public int CreateLimitPerMinute { get; set; } = 10;

    public int Port { get; set; } = 5000;

    public string[] GetTrustedOriginsArray() =>
        TrustedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
