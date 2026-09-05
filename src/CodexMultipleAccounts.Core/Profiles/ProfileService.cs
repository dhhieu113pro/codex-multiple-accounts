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
            var profiles = (await _store.LoadAsync(cancellationToken)).ToList();
            var id = Guid.NewGuid();
            var home = _paths.GetCodexHome(id);
            Directory.CreateDirectory(home);
            var profile = new CodexProfile(id, name.Trim(), home);
            profiles.Add(profile);
            await _store.SaveAsync(profiles, cancellationToken);
            return profile;
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
            EnsureManagedProfileDirectory(profileDirectory);
            if (Directory.Exists(profileDirectory))
                Directory.Delete(profileDirectory, recursive: true);

            profiles.Remove(profile);
            await _store.SaveAsync(profiles, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void EnsureManagedProfileDirectory(string directory)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_paths.ProfilesDirectory));
        var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!target.StartsWith(root + Path.DirectorySeparatorChar, comparison))
            throw new InvalidOperationException("Refusing to delete a directory outside the managed profiles root.");
    }
}
