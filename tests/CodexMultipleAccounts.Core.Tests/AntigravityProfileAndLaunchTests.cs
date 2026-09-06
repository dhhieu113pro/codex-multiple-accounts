using System.Text.Json;
using CodexMultipleAccounts.Core.Launching;
using CodexMultipleAccounts.Core.Profiles;

namespace CodexMultipleAccounts.Core.Tests;

public sealed class AntigravityProfileAndLaunchTests
{
    [Fact]
    public void LegacyProfileJson_DefaultsToCodexProvider()
    {
        var id = Guid.NewGuid();
        var json = $$"""
        {"Id":"{{id}}","Name":"Legacy","CodexHome":"/tmp/legacy","LastUsedAt":null,"IsGloballyActive":false}
        """;

        var profile = JsonSerializer.Deserialize<CodexProfile>(json)!;

        Assert.Equal(AccountProvider.Codex, profile.Provider);
        Assert.Null(profile.AntigravityMode);
    }

    [Theory]
    [InlineData(AntigravityProfileMode.Full)]
    [InlineData(AntigravityProfileMode.Shared)]
    public async Task CreateAsync_CreatesManagedAntigravityProfile(AntigravityProfileMode mode)
    {
        using var temp = new TempDirectory();
        var profiles = new ProfileService(temp.Path, Path.Combine(temp.Path, "default-codex"));
        var service = new AntigravityProfileService(profiles);

        var profile = await service.CreateAsync("Work", mode);

        Assert.Equal(AccountProvider.Antigravity, profile.Provider);
        Assert.Equal(mode, profile.AntigravityMode);
        Assert.EndsWith("antigravity-home", profile.ProfileHome);
        Assert.True(Directory.Exists(profile.ProfileHome));
    }

    [Fact]
    public void WindowsLaunch_UsesChildProfileEnvironment()
    {
        using var temp = new TempDirectory();
        var profile = AntigravityProfile(temp.Path);
        var before = Environment.GetEnvironmentVariable("USERPROFILE");

        var spec = new AntigravityLaunchService().Create(profile, HostPlatform.Windows, "Antigravity.exe", temp.Path);

        Assert.Equal(profile.ProfileHome, spec.Environment["USERPROFILE"]);
        Assert.Equal(Path.Combine(profile.ProfileHome, "AppData", "Roaming"), spec.Environment["APPDATA"]);
        Assert.Equal(Path.Combine(profile.ProfileHome, "AppData", "Local"), spec.Environment["LOCALAPPDATA"]);
        Assert.Equal(before, Environment.GetEnvironmentVariable("USERPROFILE"));
        Assert.False(spec.IndependentAuthenticationSupported);
    }

    [Fact]
    public void LinuxLaunch_UsesXdgAndElectronDirectories()
    {
        using var temp = new TempDirectory();
        var profile = AntigravityProfile(temp.Path);

        var spec = new AntigravityLaunchService().Create(profile, HostPlatform.Linux, "/usr/bin/antigravity", temp.Path);

        Assert.Equal(profile.ProfileHome, spec.Environment["HOME"]);
        Assert.Equal(Path.Combine(profile.ProfileHome, ".config"), spec.Environment["XDG_CONFIG_HOME"]);
        Assert.Contains("--user-data-dir", spec.Arguments);
        Assert.Contains("--extensions-dir", spec.Arguments);
    }

    [Fact]
    public void MacLaunch_UsesHomeAndElectronDirectories()
    {
        using var temp = new TempDirectory();
        var profile = AntigravityProfile(temp.Path);

        var spec = new AntigravityLaunchService().Create(profile, HostPlatform.MacOS, "/Applications/Antigravity.app/Contents/MacOS/Antigravity", temp.Path);

        Assert.Equal(profile.ProfileHome, spec.Environment["HOME"]);
        Assert.Contains(Path.Combine(profile.ProfileHome, "Library", "Application Support", "Antigravity"), spec.Arguments);
    }

    private static CodexProfile AntigravityProfile(string root) =>
        new(Guid.NewGuid(), "AG", Path.Combine(root, "ag-home"), null, false, AccountProvider.Antigravity, AntigravityProfileMode.Full);
}
