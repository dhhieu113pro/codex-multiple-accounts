using CodexMultipleAccounts.Core.Launching;
using CodexMultipleAccounts.Core.Profiles;
using Xunit;

namespace CodexMultipleAccounts.Core.Tests;

public sealed class CodexLaunchServiceTests
{
    [Fact]
    public void Build_uses_profile_specific_codex_home_without_mutating_process_environment()
    {
        var before = Environment.GetEnvironmentVariable("CODEX_HOME");
        var first = new CodexProfile(Guid.NewGuid(), "Personal", Path.GetFullPath("profile-a"));
        var second = new CodexProfile(Guid.NewGuid(), "Work", Path.GetFullPath("profile-b"));
        var service = new CodexLaunchService("codex");

        var firstSpec = service.Build(first, Path.GetTempPath(), ["--model", "gpt-5"]);
        var secondSpec = service.Build(second, Path.GetTempPath(), []);

        Assert.Equal(first.CodexHome, firstSpec.Environment["CODEX_HOME"]);
        Assert.Equal(second.CodexHome, secondSpec.Environment["CODEX_HOME"]);
        Assert.NotEqual(firstSpec.Environment["CODEX_HOME"], secondSpec.Environment["CODEX_HOME"]);
        Assert.Equal(before, Environment.GetEnvironmentVariable("CODEX_HOME"));
        Assert.Equal("codex", firstSpec.FileName);
        Assert.Equal(["--model", "gpt-5"], firstSpec.Arguments);
    }
}
