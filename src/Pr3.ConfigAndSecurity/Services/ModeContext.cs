using Pr3.ConfigAndSecurity.Config;

namespace Pr3.ConfigAndSecurity.Services;

public class ModeContext
{
    public AppMode Mode { get; }

    public bool IsStudy => Mode == AppMode.Study;

    public bool IsProduction => Mode == AppMode.Production;

    public ModeContext(AppSettings settings)
    {
        Mode = settings.Mode;
    }
}
