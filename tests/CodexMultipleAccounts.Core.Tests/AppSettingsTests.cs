using CodexMultipleAccounts.Core.Settings;
using CodexMultipleAccounts.Core.Launching;
using CodexMultipleAccounts.Core.Profiles;

namespace CodexMultipleAccounts.Core.Tests;

public sealed class AppSettingsTests
{
    [Fact]
    public async Task Settings_round_trip_without_changing_profile_catalog()
    {
        using var temp = new TempDirectory();
        var workspace = Path.Combine(temp.Path, "project");
        Directory.CreateDirectory(workspace);
        var service = new AppSettingsService(temp.Path);
        var settings = service.Defaults() with { DefaultWorkspace = workspace, Appearance = AppAppearance.Light };
        await service.SaveAsync(settings);
        Assert.Equal(settings, await new AppSettingsService(temp.Path).LoadAsync());
        Assert.False(File.Exists(Path.Combine(temp.Path, "profiles.json")));
    }

    [Fact]
    public async Task Invalid_workspace_does_not_replace_saved_settings()
    {
        using var temp = new TempDirectory();
        var service = new AppSettingsService(temp.Path);
        var original = service.Defaults();
        await service.SaveAsync(original);
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => service.SaveAsync(original with { DefaultWorkspace = Path.Combine(temp.Path, "missing") }));
        Assert.Equal(original, await service.LoadAsync());
    }

    [Fact]
    public async Task Invalid_executable_path_is_rejected()
    {
        using var temp = new TempDirectory();
        var service = new AppSettingsService(temp.Path);
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.SaveAsync(service.Defaults() with { CodexExecutable = Path.Combine(temp.Path, "missing-cli") }));
    }

    [Fact]
    public async Task Malformed_settings_report_an_actionable_error()
    {
        using var temp = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(temp.Path, "settings.json"), "{broken");
        await Assert.ThrowsAsync<InvalidDataException>(() => new AppSettingsService(temp.Path).LoadAsync());
    }

    [Fact]
    public void Configured_codex_executable_and_workspace_are_child_only()
    {
        using var temp = new TempDirectory();
        var profile = new CodexProfile(Guid.NewGuid(), "Work", Path.Combine(temp.Path, "home"), null);
        var before = Environment.GetEnvironmentVariable("CODEX_HOME");
        var spec = new CodexLaunchService().Create(profile, temp.Path, executable: "custom-codex");
        Assert.Equal("custom-codex", spec.Executable);
        Assert.Equal(Path.GetFullPath(temp.Path), spec.WorkingDirectory);
        Assert.Equal(profile.CodexHome, spec.Environment["CODEX_HOME"]);
        Assert.Equal(before, Environment.GetEnvironmentVariable("CODEX_HOME"));
    }
}
