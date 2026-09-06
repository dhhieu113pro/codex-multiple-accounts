namespace CodexMultipleAccounts.Core.Profiles;

public sealed class AntigravityProfileService
{
    private readonly ProfileService _profiles;

    public AntigravityProfileService(ProfileService profiles) => _profiles = profiles;

    public async Task<IReadOnlyList<CodexProfile>> ListAsync() =>
        (await _profiles.ListAsync()).Where(x => x.Provider == AccountProvider.Antigravity).ToArray();

    public Task<CodexProfile> CreateAsync(string name, AntigravityProfileMode mode) =>
        _profiles.CreateManagedAsync(name, AccountProvider.Antigravity, mode, "antigravity-home");

    public Task DeleteAsync(Guid id) => _profiles.DeleteAsync(id);
}
