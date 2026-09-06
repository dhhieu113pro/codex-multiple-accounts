namespace CodexMultipleAccounts.Core.Launching;

public static class AntigravityExecutableLocator
{
    public static string Resolve(HostPlatform platform)
    {
        var configured = Environment.GetEnvironmentVariable("ANTIGRAVITY_EXECUTABLE");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return platform switch
        {
            HostPlatform.Windows => ResolveFirstExisting(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Antigravity", "Antigravity.exe"),
                "Antigravity.exe"),
            HostPlatform.MacOS => ResolveFirstExisting(
                "/Applications/Antigravity.app/Contents/MacOS/Antigravity",
                "antigravity"),
            HostPlatform.Linux => ResolveFirstExisting(
                "/usr/bin/antigravity",
                "/opt/Antigravity/antigravity",
                "antigravity"),
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };
    }

    private static string ResolveFirstExisting(params string[] candidates) =>
        candidates.FirstOrDefault(File.Exists) ?? candidates[^1];
}
