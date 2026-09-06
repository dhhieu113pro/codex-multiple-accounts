using Avalonia.Threading;
using CodexMultipleAccounts.App.ViewModels;
using CodexMultipleAccounts.Core.Launching;
using Porta.Pty;

namespace CodexMultipleAccounts.App.Terminal;

public sealed class ProcessTerminalLauncher
{
    private const int InitialCols = 120;
    private const int InitialRows = 30;

    public TerminalSessionViewModel Launch(string title, CodexLaunchSpec spec)
    {
        spec.Environment.TryGetValue("CODEX_HOME", out var codexHome);

        IPtyConnection? terminal = null;
        var cols = InitialCols;
        var rows = InitialRows;
        TerminalSessionViewModel? vm = null;
        vm = new TerminalSessionViewModel(title, async input =>
        {
            var active = terminal;
            if (active is null || vm is null || !vm.IsRunning)
                return;

            var bytes = System.Text.Encoding.UTF8.GetBytes(input + "\r");
            await WriteInputAsync(active, bytes);
        }, codexHome);

        vm.TerminalModel.UserInput += (_, e) =>
        {
            var active = terminal;
            if (active is null || !vm.IsRunning || e.Data.Length == 0)
                return;

            _ = WriteInputAsync(active, e.Data.ToArray());
        };

        vm.TerminalModel.SizeChanged += (_, e) =>
        {
            if (e.Cols <= 0 || e.Rows <= 0)
                return;

            cols = e.Cols;
            rows = e.Rows;

            var active = terminal;
            if (active is not null && vm.IsRunning)
                TryResize(active, cols, rows);
        };

        _ = StartAsync(vm, spec, connection =>
        {
            terminal = connection;
            TryResize(connection, cols, rows);
        });
        return vm;
    }

    private static async Task StartAsync(
        TerminalSessionViewModel vm,
        CodexLaunchSpec spec,
        Action<IPtyConnection> onStarted)
    {
        IPtyConnection? terminal = null;
        try
        {
            var options = new PtyOptions
            {
                Name = vm.Title,
                Cols = InitialCols,
                Rows = InitialRows,
                Cwd = spec.WorkingDirectory,
                App = spec.Executable,
                CommandLine = spec.Arguments.ToArray(),
                Environment = new Dictionary<string, string>(spec.Environment)
            };

            terminal = await PtyProvider.SpawnAsync(options, CancellationToken.None);
            onStarted(terminal);

            terminal.ProcessExited += (_, e) =>
                Dispatcher.UIThread.Post(() => vm.MarkExited(e.ExitCode));

            await PumpOutputAsync(terminal, vm);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                vm.IsRunning = false;
                vm.AppendOutput($"Unable to start embedded PTY: {ex.Message}\r\nUse Open externally as a fallback.\r\n");
            });
        }
        finally
        {
            terminal?.Dispose();
        }
    }

    private static async Task PumpOutputAsync(IPtyConnection terminal, TerminalSessionViewModel vm)
    {
        var buffer = new byte[8192];
        while (vm.IsRunning)
        {
            var read = await terminal.ReaderStream.ReadAsync(buffer);
            if (read == 0)
                break;

            var copy = buffer[..read].ToArray();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                vm.Output = string.Empty;
                vm.TerminalModel.Feed(copy, copy.Length);
            });
        }
    }

    private static async Task WriteInputAsync(IPtyConnection terminal, byte[] bytes)
    {
        try
        {
            await terminal.WriterStream.WriteAsync(bytes);
            await terminal.WriterStream.FlushAsync();
        }
        catch (ObjectDisposedException)
        {
            // The PTY exited between the input event and the write.
        }
        catch (IOException)
        {
            // The PTY closed between the input event and the write.
        }
    }

    private static void TryResize(IPtyConnection terminal, int cols, int rows)
    {
        try
        {
            terminal.Resize(cols, rows);
        }
        catch (ObjectDisposedException)
        {
            // The PTY exited between the resize event and this call.
        }
        catch (IOException)
        {
            // The PTY closed between the resize event and this call.
        }
    }
}
