using CodexMultipleAccounts.Core.Launching;
using CodexMultipleAccounts.Core.Profiles;

namespace CodexMultipleAccounts.Core.Tests;

public sealed class AntigravityProcessManagerTests
{
    [Fact]
    public void StartStopRestart_TracksProfileProcess()
    {
        var runner = new FakeProcessRunner();
        var manager = new AntigravityProcessManager(runner);
        var profile = new CodexProfile(Guid.NewGuid(), "AG", "/tmp/ag", null, false, AccountProvider.Antigravity, AntigravityProfileMode.Full);
        var spec = new AntigravityLaunchSpec("antigravity", [], "/tmp", new Dictionary<string,string>(), false);

        var first = manager.Start(profile, spec);
        Assert.True(manager.IsRunning(profile.Id));
        Assert.Equal(first, manager.Start(profile, spec));

        var second = manager.Restart(profile, spec);
        Assert.NotEqual(first, second);
        Assert.True(manager.IsRunning(profile.Id));

        manager.Stop(profile.Id);
        Assert.False(manager.IsRunning(profile.Id));
    }

    private sealed class FakeProcessRunner : IProcessRunner
    {
        private int _next = 100;
        private readonly HashSet<int> _running = [];
        public int Start(AntigravityLaunchSpec spec) { var id = _next++; _running.Add(id); return id; }
        public bool IsRunning(int processId) => _running.Contains(processId);
        public void Stop(int processId) => _running.Remove(processId);
    }
}
