namespace CodexMultipleAccounts.Core.Activation;

public sealed record GlobalActivationResult(bool HadPreviousDefault, string? BackupDirectory);
