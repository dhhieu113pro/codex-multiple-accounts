using System.Diagnostics;
using Avalonia.Threading;
using CodexMultipleAccounts.App.ViewModels;
using CodexMultipleAccounts.Core.Launching;

namespace CodexMultipleAccounts.App.Terminal;

public sealed class ProcessTerminalLauncher
{
    public TerminalSessionViewModel Launch(string title, CodexLaunchSpec spec)
    {
        Process? process = null;
        TerminalSessionViewModel? vm = null;

        try
        {
            var psi = new ProcessStartInfo(spec.Executable)
            {
                WorkingDirectory = spec.WorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var argument in spec.Arguments)
                psi.ArgumentList.Add(argument);
            foreach (var entry in spec.Environment)
                psi.Environment[entry.Key] = entry.Value;

            process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            vm = new TerminalSessionViewModel(title, async input =>
            {
                if (process.HasExited)
                    return;

                await process.StandardInput.WriteLineAsync(input);
                await process.StandardInput.FlushAsync();
            });

            process.OutputDataReceived += (_, e) => AppendLine(vm, e.Data);
            process.ErrorDataReceived += (_, e) => AppendLine(vm, e.Data);

            if (!process.Start())
                throw new InvalidOperationException("Unable to start Codex.");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            _ = ObserveExitAsync(process, vm);
        }
        catch (Exception ex)
        {
            process?.Dispose();
            vm ??= new TerminalSessionViewModel(title);
            vm.Output = ex.Message;
        }

        return vm;
    }

    private static void AppendLine(TerminalSessionViewModel vm, string? line)
    {
        if (line is null)
            return;

        Dispatcher.UIThread.Post(() => vm.AppendOutput(line + Environment.NewLine));
    }

    private static async Task ObserveExitAsync(Process process, TerminalSessionViewModel vm)
    {
        try
        {
            await process.WaitForExitAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
                vm.AppendOutput($"{Environment.NewLine}Codex exited with code {process.ExitCode}.{Environment.NewLine}"));
        }
        finally
        {
            process.Dispose();
        }
    }
}