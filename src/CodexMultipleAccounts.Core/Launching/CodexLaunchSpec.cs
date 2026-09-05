namespace CodexMultipleAccounts.Core.Launching;
public sealed record CodexLaunchSpec(string Executable,IReadOnlyList<string> Arguments,string WorkingDirectory,IReadOnlyDictionary<string,string> Environment);
