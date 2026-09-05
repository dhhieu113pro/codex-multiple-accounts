using System.Diagnostics;
using CodexMultipleAccounts.Core.Launching;

namespace CodexMultipleAccounts.App.Services;

public interface IExternalTerminalLauncher
{
    Task LaunchAsync(LaunchSpec spec, CancellationToken cancellationToken = default);
}

public sealed class ExternalTerminalLauncher : IExternalTerminalLauncher
{
    public Task LaunchAsync(LaunchSpec spec, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var command = ExternalTerminalCommandBuilder.Build(spec, ExternalTerminalCommandBuilder.CurrentPlatform());
        var startInfo = new ProcessStartInfo
        {
            FileName = command.FileName,
            WorkingDirectory = spec.WorkingDirectory,
            UseShellExecute = false
        };
        foreach (var argument in command.Arguments)
            startInfo.ArgumentList.Add(argument);

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to launch terminal '{command.FileName}'.");
        process.Dispose();
        return Task.CompletedTask;
    }
}
