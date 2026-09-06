using CodexMultipleAccounts.Core.Launching;
using CodexMultipleAccounts.Core.Profiles;

namespace CodexMultipleAccounts.Core.Tests;

public sealed class ProfileAndLaunchTests
{
    [Fact]
    public async Task CreateAsync_CreatesDedicatedCodexHome()
    {
        using var temp = new TempDirectory();
        var service = new ProfileService(temp.Path, Path.Combine(temp.Path, "default-codex"));
        var profile = await service.CreateAsync("Personal");
        Assert.Equal("Personal", profile.Name);
        Assert.StartsWith(Path.Combine(temp.Path, "profiles"), profile.CodexHome);
        Assert.True(Directory.Exists(profile.CodexHome));
    }

    [Fact]
    public async Task ImportDefaultAsync_CopiesOpaqueFiles()
    {
        using var temp = new TempDirectory();
        var defaultHome = Path.Combine(temp.Path, "default-codex");
        Directory.CreateDirectory(defaultHome);
        await File.WriteAllTextAsync(Path.Combine(defaultHome, "auth.json"), "opaque-secret");
        var service = new ProfileService(temp.Path, defaultHome);
        var profile = await service.ImportDefaultAsync("Imported");
        Assert.Equal("opaque-secret", await File.ReadAllTextAsync(Path.Combine(profile.CodexHome, "auth.json")));
    }

    [Fact]
    public async Task ImportDefaultAsync_RemovesPartialProfile_WhenCopyFails()
    {
        using var temp = new TempDirectory();
        var defaultHome = Path.Combine(temp.Path, "default-codex");
        Directory.CreateDirectory(defaultHome);
        var profilesRoot = Path.Combine(temp.Path, "profiles");
        var service = new ProfileService(temp.Path, defaultHome, (_, target) =>
        {
            Directory.CreateDirectory(target);
            File.WriteAllText(Path.Combine(target, "partial.txt"), "partial");
            throw new IOException("simulated copy failure");
        });

        await Assert.ThrowsAsync<IOException>(() => service.ImportDefaultAsync("Broken"));

        Assert.Empty(await service.ListAsync());
        Assert.Empty(Directory.EnumerateDirectories(profilesRoot));
    }

    [Fact]
    public async Task LaunchSpec_UsesChildOnlyCodexHome()
    {
        using var temp = new TempDirectory();
        var profile = new CodexProfile(Guid.NewGuid(), "Work", Path.Combine(temp.Path, "work"), null, false);
        var before = Environment.GetEnvironmentVariable("CODEX_HOME");
        var spec = new CodexLaunchService().Create(profile, temp.Path, ["--help"]);
        Assert.Equal(profile.CodexHome, spec.Environment["CODEX_HOME"]);
        Assert.Equal(before, Environment.GetEnvironmentVariable("CODEX_HOME"));
        Assert.Equal("codex", spec.Executable);
    }
}

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() { Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cma-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(Path); }
    public string Path { get; }
    public void Dispose() { if (Directory.Exists(Path)) Directory.Delete(Path, true); }
}
