namespace CodexMultipleAccounts.Core.Profiles;

public sealed record CodexProfile(
    Guid Id,
    string Name,
    string CodexHome,
    DateTimeOffset? LastUsedUtc = null,
    bool IsGloballyActive = false);
