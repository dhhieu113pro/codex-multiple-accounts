namespace CodexMultipleAccounts.Core.Launching;

public sealed record AntigravityLaunchSpec(
    string Executable,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment,
    bool IndependentAuthenticationSupported);
