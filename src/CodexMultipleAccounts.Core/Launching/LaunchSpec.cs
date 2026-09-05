namespace CodexMultipleAccounts.Core.Launching;

public sealed record LaunchSpec(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment);
