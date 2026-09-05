using CodexMultipleAccounts.Core.Activation;
using CodexMultipleAccounts.Core.Profiles;
using CodexMultipleAccounts.Core.Storage;
using Xunit;

namespace CodexMultipleAccounts.Core.Tests;

public sealed class GlobalActivationServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "cma-activation-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ActivateAsync_backs_up_default_before_replacing_it()
    {
        var defaultHome = Path.Combine(_root, ".codex");
        var profileHome = Path.Combine(_root, "profile");
        Directory.CreateDirectory(defaultHome);
        Directory.CreateDirectory(profileHome);
        await File.WriteAllTextAsync(Path.Combine(defaultHome, "state.txt"), "original");
        await File.WriteAllTextAsync(Path.Combine(profileHome, "state.txt"), "selected");
        var profile = new CodexProfile(Guid.NewGuid(), "Work", profileHome);
        var service = new GlobalActivationService();

        var result = await service.ActivateAsync(profile, defaultHome);

        Assert.Equal("selected", await File.ReadAllTextAsync(Path.Combine(defaultHome, "state.txt")));
        Assert.True(result.HadPreviousDefault);
        Assert.Equal("original", await File.ReadAllTextAsync(Path.Combine(result.BackupDirectory!, "state.txt")));
    }

    [Fact]
    public async Task RestoreBackupAsync_restores_previous_default_state()
    {
        var defaultHome = Path.Combine(_root, ".codex");
        var profileHome = Path.Combine(_root, "profile");
        Directory.CreateDirectory(defaultHome);
        Directory.CreateDirectory(profileHome);
        await File.WriteAllTextAsync(Path.Combine(defaultHome, "state.txt"), "original");
        await File.WriteAllTextAsync(Path.Combine(profileHome, "state.txt"), "selected");
        var service = new GlobalActivationService();
        var result = await service.ActivateAsync(new CodexProfile(Guid.NewGuid(), "Work", profileHome), defaultHome);

        await service.RestoreBackupAsync(defaultHome, result.BackupDirectory!);

        Assert.Equal("original", await File.ReadAllTextAsync(Path.Combine(defaultHome, "state.txt")));
    }

    [Fact]
    public async Task Failed_staging_copy_leaves_existing_default_untouched()
    {
        var defaultHome = Path.Combine(_root, ".codex");
        Directory.CreateDirectory(defaultHome);
        await File.WriteAllTextAsync(Path.Combine(defaultHome, "state.txt"), "original");
        var missingProfile = new CodexProfile(Guid.NewGuid(), "Missing", Path.Combine(_root, "missing"));
        var service = new GlobalActivationService();

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => service.ActivateAsync(missingProfile, defaultHome));

        Assert.Equal("original", await File.ReadAllTextAsync(Path.Combine(defaultHome, "state.txt")));
    }

    [Fact]
    public async Task Metadata_failure_after_promotion_restores_previous_default()
    {
        var defaultHome = Path.Combine(_root, ".codex");
        Directory.CreateDirectory(defaultHome);
        await File.WriteAllTextAsync(Path.Combine(defaultHome, "state.txt"), "original");

        var paths = new AppPaths(Path.Combine(_root, "manager"));
        var profiles = new ProfileService(paths, new ProfileStore(paths));
        var profile = await profiles.CreateAsync("Work");
        await File.WriteAllTextAsync(Path.Combine(profile.CodexHome, "state.txt"), "selected");

        File.Delete(paths.ProfilesFile);
        Directory.CreateDirectory(paths.ProfilesFile);
        var service = new GlobalActivationService(profiles);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ActivateAsync(profile, defaultHome));

        Assert.Equal("original", await File.ReadAllTextAsync(Path.Combine(defaultHome, "state.txt")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
