namespace CodexMultipleAccounts.Core.Settings;

public enum AppAppearance
{
    System,
    Light,
    Dark
}

public sealed record AppSettings(
    string DefaultWorkspace,
    string CodexExecutable = "codex",
    string AntigravityExecutable = "",
    AppAppearance Appearance = AppAppearance.System);
