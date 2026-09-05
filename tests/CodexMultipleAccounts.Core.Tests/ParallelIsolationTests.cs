using System.Diagnostics;
using CodexMultipleAccounts.Core.Launching;
using CodexMultipleAccounts.Core.Profiles;
using Xunit;

namespace CodexMultipleAccounts.Core.Tests;

public sealed class ParallelIsolationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "cma-parallel-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Two_parallel_child_processes_receive_different_codex_homes()
    {
        Directory.CreateDirectory(_root);
        var first = new CodexProfile(Guid.NewGuid(), "Personal", Path.Combine(_root, "personal"));
        var second = new CodexProfile(Guid.NewGuid(), "Work", Path.Combine(_root, "work"));
        Directory.CreateDirectory(first.CodexHome);
        Directory.CreateDirectory(second.CodexHome);
        var launch = new CodexLaunchService("unused");
        var firstSpec = launch.Build(first, _root, []);
        var secondSpec = launch.Build(second, _root, []);

        var firstTask = CaptureCodexHomeAsync(firstSpec);
        var secondTask = CaptureCodexHomeAsync(secondSpec);
        var values = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(Path.GetFullPath(first.CodexHome), values[0]);
        Assert.Equal(Path.GetFullPath(second.CodexHome), values[1]);
        Assert.NotEqual(values[0], values[1]);
    }

    private static async Task<string> CaptureCodexHomeAsync(LaunchSpec spec)
    {
        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = spec.WorkingDirectory
        };

        if (OperatingSystem.IsWindows())
        {
            startInfo.FileName = "cmd.exe";
            startInfo.ArgumentList.Add("/d");
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add("echo %CODEX_HOME%");
        }
        else
        {
            startInfo.FileName = "/bin/sh";
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("printf '%s' \"$CODEX_HOME\"");
        }

        foreach (var pair in spec.Environment)
            startInfo.Environment[pair.Key] = pair.Value;

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start isolation test child process.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = (await outputTask).Trim();
        var error = await errorTask;
        Assert.True(process.ExitCode == 0, $"Child process failed with {process.ExitCode}: {error}");
        return output;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
