using CodexMultipleAccounts.Core.Profiles;

namespace CodexMultipleAccounts.App.ViewModels;

public sealed class ProfileCardViewModel(
    CodexProfile profile,
    string accent,
    string subtitle = "Codex profile",
    double fiveHourPercent = 0,
    double weeklyPercent = 0,
    string fiveHourLabel = "—",
    string weeklyLabel = "—")
{
    public CodexProfile Profile { get; } = profile;
    public string Name => Profile.Name;
    public string CodexHome => Profile.CodexHome;
    public bool IsGloballyActive => Profile.IsGloballyActive;
    public string Accent { get; } = accent;
    public string Subtitle { get; } = subtitle;
    public double FiveHourPercent { get; } = fiveHourPercent;
    public double WeeklyPercent { get; } = weeklyPercent;
    public string FiveHourLabel { get; } = fiveHourLabel;
    public string WeeklyLabel { get; } = weeklyLabel;
}
