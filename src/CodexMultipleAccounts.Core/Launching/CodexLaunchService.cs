using CodexMultipleAccounts.Core.Profiles;

namespace CodexMultipleAccounts.Core.Launching;

public sealed class CodexLaunchService
{
    public CodexLaunchSpec Create(CodexProfile profile, string workingDirectory, IReadOnlyList<string>? arguments = null, string? executable = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new CodexLaunchSpec(
            string.IsNullOrWhiteSpace(executable) ? "codex" : executable,
            arguments ?? [],
            Path.GetFullPath(workingDirectory),
            new Dictionary<string, string> { ["CODEX_HOME"] = profile.CodexHome });
    }
}
