using CodexMultipleAccounts.Core.Profiles;

namespace CodexMultipleAccounts.Core.Launching;

public sealed class AntigravityProcessManager
{
    private readonly IProcessRunner _runner;
    private readonly Dictionary<Guid, int> _processIds = [];

    public AntigravityProcessManager(IProcessRunner? runner = null) => _runner = runner ?? new SystemProcessRunner();

    public bool IsRunning(Guid profileId)
    {
        if (!_processIds.TryGetValue(profileId, out var processId))
            return false;

        if (_runner.IsRunning(processId))
            return true;

        _processIds.Remove(profileId);
        return false;
    }

    public int Start(CodexProfile profile, AntigravityLaunchSpec spec)
    {
        if (profile.Provider != AccountProvider.Antigravity)
            throw new ArgumentException("Profile must use the Antigravity provider.", nameof(profile));

        if (IsRunning(profile.Id))
            return _processIds[profile.Id];

        var processId = _runner.Start(spec);
        _processIds[profile.Id] = processId;
        return processId;
    }

    public void Stop(Guid profileId)
    {
        if (!_processIds.TryGetValue(profileId, out var processId))
            return;

        _runner.Stop(processId);
        _processIds.Remove(profileId);
    }

    public int Restart(CodexProfile profile, AntigravityLaunchSpec spec)
    {
        Stop(profile.Id);
        return Start(profile, spec);
    }
}
