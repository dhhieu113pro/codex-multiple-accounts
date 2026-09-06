using CodexMultipleAccounts.Core.Profiles;

namespace CodexMultipleAccounts.Core.Launching;

public sealed class AntigravityLaunchService
{
    public AntigravityLaunchSpec Create(CodexProfile profile, HostPlatform platform, string executablePath, string? workingDirectory = null)
    {
        if (profile.Provider != AccountProvider.Antigravity)
            throw new ArgumentException("Profile must use the Antigravity provider.", nameof(profile));

        var home = Path.GetFullPath(profile.ProfileHome);
        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var args = new List<string>();

        switch (platform)
        {
            case HostPlatform.Windows:
                env["USERPROFILE"] = home;
                env["APPDATA"] = Path.Combine(home, "AppData", "Roaming");
                env["LOCALAPPDATA"] = Path.Combine(home, "AppData", "Local");
                break;
            case HostPlatform.Linux:
                env["HOME"] = home;
                env["XDG_CONFIG_HOME"] = Path.Combine(home, ".config");
                env["XDG_CACHE_HOME"] = Path.Combine(home, ".cache");
                env["XDG_DATA_HOME"] = Path.Combine(home, ".local", "share");
                env["XDG_STATE_HOME"] = Path.Combine(home, ".local", "state");
                AddDataArguments(args, Path.Combine(home, ".config", "Antigravity"), Path.Combine(home, ".antigravity", "extensions"));
                break;
            case HostPlatform.MacOS:
                env["HOME"] = home;
                AddDataArguments(args, Path.Combine(home, "Library", "Application Support", "Antigravity"), Path.Combine(home, ".antigravity", "extensions"));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
        }

        return new AntigravityLaunchSpec(
            executablePath,
            args,
            Path.GetFullPath(workingDirectory ?? Environment.CurrentDirectory),
            env,
            IndependentAuthenticationSupported: false);
    }

    public static HostPlatform CurrentPlatform() =>
        OperatingSystem.IsWindows() ? HostPlatform.Windows :
        OperatingSystem.IsMacOS() ? HostPlatform.MacOS :
        HostPlatform.Linux;

    private static void AddDataArguments(List<string> args, string userDataDirectory, string extensionsDirectory)
    {
        args.Add("--user-data-dir");
        args.Add(userDataDirectory);
        args.Add("--extensions-dir");
        args.Add(extensionsDirectory);
    }
}
