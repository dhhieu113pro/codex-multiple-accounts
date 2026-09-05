using CodexMultipleAccounts.Core.Storage;

namespace CodexMultipleAccounts.Core.Profiles;

public sealed class ProfileService
{
    private readonly AppPaths _paths;
    private readonly ProfileStore _store;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ProfileService(AppPaths paths, ProfileStore store)
    {
        _paths = paths;
        _store = store;
    }

    public Task<IReadOnlyList<CodexProfile>> ListAsync(CancellationToken cancellationToken = default) =>
        _store.LoadAsync(cancellationToken);

    public async Task<CodexProfile> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await CreateCoreAsync(name.Trim(), cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CodexProfile> ImportDefaultAsync(
        string name,
        string defaultCodexHome,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCodexHome);
        if (!Directory.Exists(defaultCodexHome))
            throw new DirectoryNotFoundException($"Default Codex home '{defaultCodexHome}' does not exist.");

        await _gate.WaitAsync(cancellationToken);
        CodexProfile? profile = null;
        try
        {
            profile = await CreateCoreAsync(name.Trim(), cancellationToken);
            await SafeFileTree.CopyDirectoryAsync(defaultCodexHome, profile.CodexHome, cancellationToken);
            return profile;
        }
        catch
        {
            if (profile is not null)
            {
                var profiles = (await _store.LoadAsync(CancellationToken.None)).Where(item => item.Id != profile.Id).ToList();
                var profileDirectory = _paths.GetProfileDirectory(profile.Id);
                if (Directory.Exists(profileDirectory))
                    await SafeFileTree.DeleteManagedDirectoryAsync(profileDirectory, _paths.ProfilesDirectory, CancellationToken.None);
                await _store.SaveAsync(profiles, CancellationToken.None);
            }

            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CodexProfile> RenameAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var profiles = (await _store.LoadAsync(cancellationToken)).ToList();
            var index = profiles.FindIndex(profile => profile.Id == id);
            if (index < 0)
                throw new KeyNotFoundException($"Profile '{id}' was not found.");

            var renamed = profiles[index] with { Name = name.Trim() };
            profiles[index] = renamed;
            await _store.SaveAsync(profiles, cancellationToken);
            return renamed;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var profiles = (await _store.LoadAsync(cancellationToken)).ToList();
            var profile = profiles.SingleOrDefault(item => item.Id == id)
                ?? throw new KeyNotFoundException($"Profile '{id}' was not found.");

            var profileDirectory = _paths.GetProfileDirectory(profile.Id);
            if (Directory.Exists(profileDirectory))
                await SafeFileTree.DeleteManagedDirectoryAsync(profileDirectory, _paths.ProfilesDirectory, cancellationToken);

            profiles.Remove(profile);
            await _store.SaveAsync(profiles, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<CodexProfile> CreateCoreAsync(string name, CancellationToken cancellationToken)
    {
        var profiles = (await _store.LoadAsync(cancellationToken)).ToList();
        var id = Guid.NewGuid();
        var home = _paths.GetCodexHome(id);
        Directory.CreateDirectory(home);
        var profile = new CodexProfile(id, name, home);
        profiles.Add(profile);
        await _store.SaveAsync(profiles, cancellationToken);
        return profile;
    }
}
