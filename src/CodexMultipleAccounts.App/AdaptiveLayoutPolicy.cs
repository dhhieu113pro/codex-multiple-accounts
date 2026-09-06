namespace CodexMultipleAccounts.App;

public enum AdaptiveLayoutMode
{
    Wide,
    Compact,
    TerminalFirst,
}

public readonly record struct AdaptiveLayoutMetrics(
    AdaptiveLayoutMode Mode,
    double NavigationWidth,
    double AccountsWidth,
    bool ShowNavigationLabels,
    bool ShowAccountsPane,
    bool ShowAccountsToggle,
    bool UseAccountsOverlay);

public static class AdaptiveLayoutPolicy
{
    public const double WideBreakpoint = 1200;
    public const double TerminalFirstBreakpoint = 900;

    public static AdaptiveLayoutMetrics Resolve(double width, bool compactAccountsOpen)
    {
        if (width >= WideBreakpoint)
        {
            return new AdaptiveLayoutMetrics(
                AdaptiveLayoutMode.Wide,
                NavigationWidth: 156,
                AccountsWidth: 392,
                ShowNavigationLabels: true,
                ShowAccountsPane: true,
                ShowAccountsToggle: false,
                UseAccountsOverlay: false);
        }

        if (width >= TerminalFirstBreakpoint)
        {
            return new AdaptiveLayoutMetrics(
                AdaptiveLayoutMode.Compact,
                NavigationWidth: 64,
                AccountsWidth: 340,
                ShowNavigationLabels: false,
                ShowAccountsPane: true,
                ShowAccountsToggle: false,
                UseAccountsOverlay: false);
        }

        var accountsOpen = compactAccountsOpen;
        return new AdaptiveLayoutMetrics(
            AdaptiveLayoutMode.TerminalFirst,
            NavigationWidth: 56,
            AccountsWidth: accountsOpen ? 340 : 0,
            ShowNavigationLabels: false,
            ShowAccountsPane: accountsOpen,
            ShowAccountsToggle: true,
            UseAccountsOverlay: accountsOpen);
    }
}
