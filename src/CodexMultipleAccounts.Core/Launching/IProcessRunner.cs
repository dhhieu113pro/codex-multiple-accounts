namespace CodexMultipleAccounts.Core.Launching;

public interface IProcessRunner
{
    int Start(AntigravityLaunchSpec spec);
    bool IsRunning(int processId);
    void Stop(int processId);
}
