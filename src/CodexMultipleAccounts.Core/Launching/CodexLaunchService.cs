using CodexMultipleAccounts.Core.Profiles;

namespace CodexMultipleAccounts.Core.Launching;

public sealed class CodexLaunchService
{
    private readonly string _executable;

    public CodexLaunchService(string executable = "codex")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        _executable = executable;
    }

    public LaunchSpec Build(
        CodexProfile profile,
        string workingDirectory,
        IReadOnlyList<string>? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        return new LaunchSpec(
            _executable,
            arguments ?? [],
            Path.GetFullPath(workingDirectory),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["CODEX_HOME"] = Path.GetFullPath(profile.CodexHome)
            });
    }
}
