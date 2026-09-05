namespace CodexMultipleAccounts.App.Services;

public sealed record RuntimePaths(string AppDataRoot, string DefaultCodexHome)
{
    public static RuntimePaths Create()
    {
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userHome))
            throw new InvalidOperationException("Unable to resolve the current user's home directory.");

        string appDataRoot;
        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            appDataRoot = Path.Combine(local, "CodexMultipleAccounts");
        }
        else if (OperatingSystem.IsMacOS())
        {
            appDataRoot = Path.Combine(userHome, "Library", "Application Support", "CodexMultipleAccounts");
        }
        else
        {
            var xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            var dataRoot = string.IsNullOrWhiteSpace(xdg)
                ? Path.Combine(userHome, ".local", "share")
                : xdg;
            appDataRoot = Path.Combine(dataRoot, "codex-multiple-accounts");
        }

        return new RuntimePaths(
            Path.GetFullPath(appDataRoot),
            Path.GetFullPath(Path.Combine(userHome, ".codex")));
    }
}
