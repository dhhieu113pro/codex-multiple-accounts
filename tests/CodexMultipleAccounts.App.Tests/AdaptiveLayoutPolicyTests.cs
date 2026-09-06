using CodexMultipleAccounts.App;

namespace CodexMultipleAccounts.App.Tests;

public class AdaptiveLayoutPolicyTests
{
    [Theory]
    [InlineData(1600, AdaptiveLayoutMode.Wide, 156, 392, true, true, false)]
    [InlineData(1200, AdaptiveLayoutMode.Wide, 156, 392, true, true, false)]
    [InlineData(1199, AdaptiveLayoutMode.Compact, 64, 340, false, true, false)]
    [InlineData(900, AdaptiveLayoutMode.Compact, 64, 340, false, true, false)]
    [InlineData(899, AdaptiveLayoutMode.TerminalFirst, 56, 0, false, false, true)]
    [InlineData(760, AdaptiveLayoutMode.TerminalFirst, 56, 0, false, false, true)]
    public void Resolve_returns_expected_shell_metrics(
        double width,
        AdaptiveLayoutMode expectedMode,
        double expectedNavigationWidth,
        double expectedAccountsWidth,
        bool expectedNavigationLabels,
        bool expectedAccountsVisible,
        bool expectedAccountsToggleVisible)
    {
        var layout = AdaptiveLayoutPolicy.Resolve(width, compactAccountsOpen: false);

        Assert.Equal(expectedMode, layout.Mode);
        Assert.Equal(expectedNavigationWidth, layout.NavigationWidth);
        Assert.Equal(expectedAccountsWidth, layout.AccountsWidth);
        Assert.Equal(expectedNavigationLabels, layout.ShowNavigationLabels);
        Assert.Equal(expectedAccountsVisible, layout.ShowAccountsPane);
        Assert.Equal(expectedAccountsToggleVisible, layout.ShowAccountsToggle);
    }

    [Fact]
    public void Resolve_opens_accounts_as_overlay_in_terminal_first_mode()
    {
        var layout = AdaptiveLayoutPolicy.Resolve(820, compactAccountsOpen: true);

        Assert.Equal(AdaptiveLayoutMode.TerminalFirst, layout.Mode);
        Assert.True(layout.ShowAccountsPane);
        Assert.True(layout.UseAccountsOverlay);
        Assert.Equal(340, layout.AccountsWidth);
        Assert.True(layout.ShowAccountsToggle);
    }

    [Fact]
    public void Resolve_ignores_overlay_flag_when_layout_has_room_for_accounts()
    {
        var layout = AdaptiveLayoutPolicy.Resolve(1100, compactAccountsOpen: true);

        Assert.Equal(AdaptiveLayoutMode.Compact, layout.Mode);
        Assert.True(layout.ShowAccountsPane);
        Assert.False(layout.UseAccountsOverlay);
        Assert.False(layout.ShowAccountsToggle);
    }
}
