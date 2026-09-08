using CodexMultipleAccounts.App.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CodexMultipleAccounts.App.Navigation;

public enum WorkspacePage
{
    Accounts,
    Sessions,
    Usage,
    Settings,
    Documentation,
    About
}

public partial class WorkspaceNavigationViewModel : ObservableObject
{
    private readonly Action<TerminalSessionViewModel>? _openSession;

    public WorkspaceNavigationViewModel(Action<TerminalSessionViewModel>? openSession = null)
    {
        _openSession = openSession;
    }

    [ObservableProperty]
    private WorkspacePage _selectedPage = WorkspacePage.Accounts;

    public void Navigate(WorkspacePage page)
    {
        if (!Enum.IsDefined(page))
            throw new ArgumentOutOfRangeException(nameof(page), page, "Unknown workspace page.");

        SelectedPage = page;
    }

    public void OpenSession(TerminalSessionViewModel session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _openSession?.Invoke(session);
        Navigate(WorkspacePage.Accounts);
    }
}
