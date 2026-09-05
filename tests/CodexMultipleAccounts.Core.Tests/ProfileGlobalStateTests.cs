using CodexMultipleAccounts.Core.Profiles;
using CodexMultipleAccounts.Core.Storage;
using Xunit;

namespace CodexMultipleAccounts.Core.Tests;

public sealed class ProfileGlobalStateTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "cma-global-state-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SetGloballyActiveAsync_marks_exactly_one_profile_and_persists()
    {
        var paths = new AppPaths(_root);
        var service = new ProfileService(paths, new ProfileStore(paths));
        var personal = await service.CreateAsync("Personal");
        var work = await service.CreateAsync("Work");

        await service.SetGloballyActiveAsync(personal.Id);
        await service.SetGloballyActiveAsync(work.Id);

        var reloaded = new ProfileService(paths, new ProfileStore(paths));
        var profiles = await reloaded.ListAsync();
        Assert.False(profiles.Single(profile => profile.Id == personal.Id).IsGloballyActive);
        Assert.True(profiles.Single(profile => profile.Id == work.Id).IsGloballyActive);
        Assert.Single(profiles, profile => profile.IsGloballyActive);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
