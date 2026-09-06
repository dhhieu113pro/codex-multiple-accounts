namespace CodexMultipleAccounts.Core.Profiles;

public sealed record CodexProfile(
    Guid Id,
    string Name,
    string CodexHome,
    DateTimeOffset? LastUsedAt,
    bool IsGloballyActive,
    AccountProvider Provider = AccountProvider.Codex,
    AntigravityProfileMode? AntigravityMode = null)
{
    public string ProfileHome => CodexHome;
}
