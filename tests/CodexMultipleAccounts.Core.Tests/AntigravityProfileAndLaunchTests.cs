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
        {"Id":"{{id}}","Name":"Legacy","CodexHome":"/tmp/legacy","LastUsedAt":null}
        """;

        var profile = JsonSerializer.Deserialize<CodexProfile>(json)!;

        Assert.Equal(AccountProvider.Codex, profile.Provider);
        Assert.Null(profile.AntigravityMode);
    }

    [Fact]
    public async Task CreateAsync_CreatesManagedAntigravityProfile()
    {
        using var temp = new TempDirectory();
        var profiles = new ProfileService(temp.Path, Path.Combine(temp.Path, "default-codex"));
        var service = new AntigravityProfileService(profiles);

        var profile = await service.CreateAsync("Work");

        Assert.Equal(AccountProvider.Antigravity, profile.Provider);
        Assert.Equal(AntigravityProfileMode.Full, profile.AntigravityMode);
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
        Assert.Contains("--user-data-dir", spec.Arguments);
        Assert.Contains(Path.Combine(profile.ProfileHome, "AppData", "Local", "Antigravity", "User Data"), spec.Arguments);
        Assert.Contains("--extensions-dir", spec.Arguments);
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

    [Theory]
    [InlineData(HostPlatform.Windows)]
    [InlineData(HostPlatform.Linux)]
    [InlineData(HostPlatform.MacOS)]
    public void LaunchSpec_PreservesWorkingDirectoryAndExecutable_OnEverySupportedPlatform(HostPlatform platform)
    {
        using var temp = new TempDirectory();
        var profile = AntigravityProfile(temp.Path);
        var executable = platform == HostPlatform.Windows ? "Agy.exe" : "antigravity";

        var spec = new AntigravityLaunchService().Create(profile, platform, executable, temp.Path);

        Assert.Equal(executable, spec.Executable);
        Assert.Equal(Path.GetFullPath(temp.Path), spec.WorkingDirectory);
        Assert.Equal(profile.ProfileHome, platform == HostPlatform.Windows
            ? spec.Environment["USERPROFILE"]
            : spec.Environment["HOME"]);
        Assert.False(spec.IndependentAuthenticationSupported);
    }

    private static CodexProfile AntigravityProfile(string root) =>
        new(Guid.NewGuid(), "AG", Path.Combine(root, "ag-home"), null, AccountProvider.Antigravity, AntigravityProfileMode.Full);
}
