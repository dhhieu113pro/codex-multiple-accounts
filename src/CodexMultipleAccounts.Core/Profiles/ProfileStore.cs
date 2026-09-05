using System.Text.Json;
using CodexMultipleAccounts.Core.Storage;

namespace CodexMultipleAccounts.Core.Profiles;

public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly AppPaths _paths;

    public ProfileStore(AppPaths paths) => _paths = paths;

    public async Task<IReadOnlyList<CodexProfile>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_paths.ProfilesFile))
            return [];

        await using var stream = File.OpenRead(_paths.ProfilesFile);
        return await JsonSerializer.DeserializeAsync<List<CodexProfile>>(stream, JsonOptions, cancellationToken)
            ?? [];
    }

    public async Task SaveAsync(IEnumerable<CodexProfile> profiles, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_paths.RootDirectory);
        var temp = _paths.ProfilesFile + ".tmp";
        await using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await JsonSerializer.SerializeAsync(stream, profiles, JsonOptions, cancellationToken);
        }

        File.Move(temp, _paths.ProfilesFile, overwrite: true);
    }
}
