using CodexMultipleAccounts.App.Navigation;
using CodexMultipleAccounts.App.ViewModels;

namespace CodexMultipleAccounts.App.Tests;

public sealed class WorkspaceNavigationTests
{
    [Fact]
    public void Accounts_is_the_default_and_every_destination_can_be_selected()
    {
        var navigation = new WorkspaceNavigationViewModel();
        Assert.Equal(WorkspacePage.Accounts, navigation.SelectedPage);

        foreach (var page in Enum.GetValues<WorkspacePage>())
        {
            navigation.Navigate(page);
            Assert.Equal(page, navigation.SelectedPage);
        }
    }

    [Fact]
    public void Invalid_page_is_rejected_without_changing_selection()
    {
        var navigation = new WorkspaceNavigationViewModel();
        navigation.Navigate(WorkspacePage.Settings);
        Assert.Throws<ArgumentOutOfRangeException>(() => navigation.Navigate((WorkspacePage)999));
        Assert.Equal(WorkspacePage.Settings, navigation.SelectedPage);
    }

    [Fact]
    public void Opening_a_session_preserves_its_identity_and_returns_to_accounts()
    {
        TerminalSessionViewModel? selected = null;
        var navigation = new WorkspaceNavigationViewModel(session => selected = session);
        var session = new TerminalSessionViewModel("Work");
        navigation.Navigate(WorkspacePage.Sessions);

        navigation.OpenSession(session);

        Assert.Same(session, selected);
        Assert.Equal(WorkspacePage.Accounts, navigation.SelectedPage);
    }
}
