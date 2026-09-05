using CodexMultipleAccounts.Core.Launching;
using Xunit;

namespace CodexMultipleAccounts.Core.Tests;

public sealed class ExternalTerminalCommandBuilderTests
{
    private static readonly LaunchSpec Spec = new(
        "codex",
        ["--model", "gpt-5"],
        Path.GetFullPath(Path.Combine("workspace", "project with spaces")),
        new Dictionary<string, string> { ["CODEX_HOME"] = Path.GetFullPath(Path.Combine("profiles", "work account")) });

    [Fact]
    public void Windows_command_runs_codex_in_Windows_Terminal_with_profile_environment()
    {
        var command = ExternalTerminalCommandBuilder.Build(Spec, TerminalPlatform.Windows);

        Assert.Equal("wt.exe", command.FileName);
        Assert.Contains("CODEX_HOME", command.Arguments);
        Assert.Contains(Spec.Environment["CODEX_HOME"], command.Arguments);
        Assert.Contains("codex", command.Arguments);
    }

    [Fact]
    public void Linux_command_uses_x_terminal_emulator_and_env()
    {
        var command = ExternalTerminalCommandBuilder.Build(Spec, TerminalPlatform.Linux);

        Assert.Equal("x-terminal-emulator", command.FileName);
        Assert.Contains("env", command.Arguments);
        Assert.Contains($"CODEX_HOME={Spec.Environment["CODEX_HOME"]}", command.Arguments);
        Assert.Contains("codex", command.Arguments);
    }

    [Fact]
    public void Mac_command_uses_Terminal_via_osascript_and_quotes_profile_path()
    {
        var command = ExternalTerminalCommandBuilder.Build(Spec, TerminalPlatform.MacOS);

        Assert.Equal("osascript", command.FileName);
        Assert.Contains("Terminal", command.Arguments);
        Assert.Contains("CODEX_HOME", command.Arguments);
        Assert.Contains("codex", command.Arguments);
    }
}
