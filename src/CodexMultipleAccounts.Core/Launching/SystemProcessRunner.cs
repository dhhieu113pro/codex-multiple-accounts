using System.Diagnostics;

namespace CodexMultipleAccounts.Core.Launching;

public sealed class SystemProcessRunner : IProcessRunner
{
    public int Start(AntigravityLaunchSpec spec)
    {
        var startInfo = new ProcessStartInfo(spec.Executable)
        {
            UseShellExecute = false,
            WorkingDirectory = spec.WorkingDirectory
        };

        foreach (var argument in spec.Arguments)
            startInfo.ArgumentList.Add(argument);

        foreach (var pair in spec.Environment)
            startInfo.Environment[pair.Key] = pair.Value;

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Antigravity.");
        return process.Id;
    }

    public bool IsRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public void Stop(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (ArgumentException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}
