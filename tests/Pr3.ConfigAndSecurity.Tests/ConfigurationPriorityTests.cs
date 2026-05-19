using Pr3.ConfigAndSecurity.Config;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace Pr3.ConfigAndSecurity.Tests;

public class ConfigurationPriorityTests
{
    [Fact]
    public void FileSettings_AreUsed_WhenNoOverride()
    {
        var settings = LoadSettings(new string[] { });

        Assert.Equal(AppMode.Study, settings.Mode);
        Assert.Equal(5000, settings.Port);
        Assert.Equal(60, settings.ReadLimitPerMinute);
    }

    [Fact]
    public void EnvironmentVariables_Override_FileSettings()
    {
        var settings = LoadSettings(new string[] { }, env =>
        {
            env["AppSettings:Port"] = "8080";
            env["AppSettings:Mode"] = "Production";
        });

        Assert.Equal(8080, settings.Port);
        Assert.Equal(AppMode.Production, settings.Mode);
    }

    [Fact]
    public void CommandLineArgs_Override_EnvAndFile()
    {
        var settings = LoadSettings(new[]
        {
            "--AppSettings:Port=9090",
            "--AppSettings:Mode=Production"
        }, env =>
        {
            env["AppSettings:Port"] = "8080";
        });

        Assert.Equal(9090, settings.Port);
        Assert.Equal(AppMode.Production, settings.Mode);
    }

    [Fact]
    public void Validation_Fails_On_InvalidPort()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var settings = new AppSettings { Port = 70000, TrustedOrigins = "http://localhost" };
            AppSettingsValidator.ValidateAndThrow(settings, true);
        });

        Assert.Contains("Port", ex.Message);
    }

    [Fact]
    public void Validation_Fails_On_InvalidOrigin()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var settings = new AppSettings { Port = 5000, TrustedOrigins = "not-a-valid-url" };
            AppSettingsValidator.ValidateAndThrow(settings, true);
        });

        Assert.Contains("not a valid absolute URI", ex.Message);
    }

    [Fact]
    public void Validation_Fails_On_WildcardInOrigin()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var settings = new AppSettings { Port = 5000, TrustedOrigins = "http://*.example.com" };
            AppSettingsValidator.ValidateAndThrow(settings, true);
        });

        Assert.Contains("wildcard", ex.Message);
    }

    [Fact]
    public void Validation_Fails_On_EmptyOrigins()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var settings = new AppSettings { Port = 5000, TrustedOrigins = "" };
            AppSettingsValidator.ValidateAndThrow(settings, true);
        });

        Assert.Contains("at least one origin", ex.Message);
    }

    private static AppSettings LoadSettings(string[] args, Action<Dictionary<string, string?>>? envSetup = null)
    {
        var defaults = new Dictionary<string, string?>
        {
            ["AppSettings:Mode"] = "Study",
            ["AppSettings:TrustedOrigins"] = "http://localhost:3000",
            ["AppSettings:Port"] = "5000",
            ["AppSettings:ReadLimitPerMinute"] = "60",
            ["AppSettings:CreateLimitPerMinute"] = "10"
        };

        var env = new Dictionary<string, string?>();
        envSetup?.Invoke(env);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(defaults)
            .AddInMemoryCollection(env)
            .AddCommandLine(args)
            .Build();

        return config.GetSection(AppSettings.SectionName).Get<AppSettings>() ?? new AppSettings();
    }
}
