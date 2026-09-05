using CodexMultipleAccounts.Core.Profiles;
using CodexMultipleAccounts.Core.Storage;
using Xunit;

namespace CodexMultipleAccounts.Core.Tests;

public sealed class ProfileServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "cma-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task CreateAsync_allocates_distinct_managed_codex_homes()
    {
        var service = CreateService();

        var first = await service.CreateAsync("Personal");
        var second = await service.CreateAsync("Work");

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.CodexHome, second.CodexHome);
        Assert.True(Directory.Exists(first.CodexHome));
        Assert.True(Directory.Exists(second.CodexHome));
        Assert.StartsWith(Path.GetFullPath(_root), Path.GetFullPath(first.CodexHome), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RenameAsync_preserves_id_and_codex_home()
    {
        var service = CreateService();
        var profile = await service.CreateAsync("Personal");

        var renamed = await service.RenameAsync(profile.Id, "Primary");

        Assert.Equal(profile.Id, renamed.Id);
        Assert.Equal(profile.CodexHome, renamed.CodexHome);
        Assert.Equal("Primary", renamed.Name);
    }

    [Fact]
    public async Task DeleteAsync_removes_managed_profile_without_touching_other_profiles()
    {
        var service = CreateService();
        var first = await service.CreateAsync("Personal");
        var second = await service.CreateAsync("Work");
        await File.WriteAllTextAsync(Path.Combine(first.CodexHome, "sentinel.txt"), "one");
        await File.WriteAllTextAsync(Path.Combine(second.CodexHome, "sentinel.txt"), "two");

        await service.DeleteAsync(first.Id);

        Assert.False(Directory.Exists(Path.GetDirectoryName(first.CodexHome)!));
        Assert.True(File.Exists(Path.Combine(second.CodexHome, "sentinel.txt")));
    }

    [Fact]
    public async Task Profiles_round_trip_through_json_metadata()
    {
        var firstService = CreateService();
        var created = await firstService.CreateAsync("Personal");

        var reloadedService = CreateService();
        var profiles = await reloadedService.ListAsync();

        var reloaded = Assert.Single(profiles);
        Assert.Equal(created.Id, reloaded.Id);
        Assert.Equal(created.Name, reloaded.Name);
        Assert.Equal(created.CodexHome, reloaded.CodexHome);
    }

    private ProfileService CreateService()
    {
        var paths = new AppPaths(_root);
        return new ProfileService(paths, new ProfileStore(paths));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
