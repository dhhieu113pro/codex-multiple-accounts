using Avalonia.Controls;
using CodexMultipleAccounts.Core.Launching;

namespace CodexMultipleAccounts.App.Terminal;

public sealed class TerminalExitedEventArgs(int exitCode) : EventArgs
{
    public int ExitCode { get; } = exitCode;
}

public interface ITerminalSession : IDisposable
{
    Control View { get; }
    string BackendName { get; }
    event EventHandler<TerminalExitedEventArgs>? Exited;
    Task StartAsync(LaunchSpec spec, CancellationToken cancellationToken = default);
    void Stop();
}
