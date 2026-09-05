namespace CodexMultipleAccounts.Core.Storage;

public sealed class AppPaths
{
    public AppPaths(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        RootDirectory = Path.GetFullPath(rootDirectory);
        ProfilesDirectory = Path.Combine(RootDirectory, "profiles");
        ProfilesFile = Path.Combine(RootDirectory, "profiles.json");
    }

    public string RootDirectory { get; }
    public string ProfilesDirectory { get; }
    public string ProfilesFile { get; }

    public string GetProfileDirectory(Guid id) => Path.Combine(ProfilesDirectory, id.ToString("N"));

    public string GetCodexHome(Guid id) => Path.Combine(GetProfileDirectory(id), "codex-home");
}
