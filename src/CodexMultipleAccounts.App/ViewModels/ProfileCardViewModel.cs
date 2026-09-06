using CodexMultipleAccounts.Core.Profiles;

namespace CodexMultipleAccounts.App.ViewModels;

public sealed class ProfileCardViewModel(
    CodexProfile profile,
    string accent,
    string? subtitle = null,
    double fiveHourPercent = 0,
    double weeklyPercent = 0,
    string fiveHourLabel = "—",
    string weeklyLabel = "—",
    bool isRunning = false)
{
    public CodexProfile Profile { get; } = profile;
    public string Name => Profile.Name;
    public string CodexHome => Profile.CodexHome;
    public string ProfileHome => Profile.ProfileHome;
    public bool IsGloballyActive => Profile.IsGloballyActive;
    public bool IsCodex => Profile.Provider == AccountProvider.Codex;
    public bool IsAntigravity => Profile.Provider == AccountProvider.Antigravity;
    public bool IsRunning { get; } = isRunning;
    public string ProviderLabel => IsCodex ? "Codex" : "Antigravity";
    public string ModeLabel => IsAntigravity ? Profile.AntigravityMode?.ToString() ?? "Full" : "Isolated CODEX_HOME";
    public string RunningLabel => IsRunning ? "Running" : "Stopped";
    public string Accent { get; } = accent;
    public string Subtitle { get; } = subtitle ?? (IsCodex ? "Codex profile" : $"Antigravity {ModeLabel} profile");
    public double FiveHourPercent { get; } = fiveHourPercent;
    public double WeeklyPercent { get; } = weeklyPercent;
    public string FiveHourLabel { get; } = fiveHourLabel;
    public string WeeklyLabel { get; } = weeklyLabel;
}
