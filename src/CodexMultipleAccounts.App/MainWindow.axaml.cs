using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CodexMultipleAccounts.App.Services;
using CodexMultipleAccounts.App.Terminal;
using CodexMultipleAccounts.App.ViewModels;
using CodexMultipleAccounts.Core.Profiles;

namespace CodexMultipleAccounts.App;

public sealed partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly List<ITerminalSession> _sessions = [];

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        viewModel.LaunchRequested += OnLaunchRequested;
        viewModel.DeleteConfirmationRequested += OnDeleteConfirmationRequested;
        Opened += OnOpened;
        Closed += OnClosed;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private async void OnLaunchRequested(object? sender, LaunchRequestEventArgs e)
    {
        try
        {
            if (OperatingSystem.IsWindows() && WindowsTerminalSession.IsSupported)
            {
                var session = new WindowsTerminalSession();
                var tab = new TabItem
                {
                    Header = e.Profile.Name,
                    Content = session.View
                };
                TerminalTabs.Items.Add(tab);
                TerminalTabs.SelectedItem = tab;
                _sessions.Add(session);
                session.Exited += (_, args) => Dispatcher.UIThread.Post(() =>
                {
                    _viewModel.StatusMessage = $"'{e.Profile.Name}' exited with code {args.ExitCode}.";
                });

                try
                {
                    await session.StartAsync(e.Spec);
                    _viewModel.StatusMessage = $"'{e.Profile.Name}' is running in an embedded isolated terminal.";
                    return;
                }
                catch
                {
                    session.Dispose();
                    _sessions.Remove(session);
                    TerminalTabs.Items.Remove(tab);
                }
            }

            var launcher = new ExternalTerminalLauncher();
            await launcher.LaunchAsync(e.Spec);
            _viewModel.StatusMessage = $"'{e.Profile.Name}' opened in an external isolated terminal.";
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = ex.Message;
        }
    }

    private async void OnDeleteConfirmationRequested(object? sender, DeleteProfileRequestEventArgs e)
    {
        var dialog = BuildDeleteDialog(e.Profile);
        var result = await dialog.ShowDialog<bool>(this);
        e.Complete(result);
    }

    private static Window BuildDeleteDialog(CodexProfile profile)
    {
        var dialog = new Window
        {
            Title = "Delete profile",
            Width = 440,
            Height = 210,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var delete = new Button { Content = "Delete", Classes = { "accent" }, MinWidth = 90 };
        var cancel = new Button { Content = "Cancel", MinWidth = 90 };
        delete.Click += (_, _) => dialog.Close(true);
        cancel.Click += (_, _) => dialog.Close(false);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { cancel, delete }
        };
        Grid.SetRow(actions, 1);

        dialog.Content = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            Margin = new Avalonia.Thickness(20),
            Children =
            {
                new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"Delete '{profile.Name}'?",
                            FontSize = 18,
                            FontWeight = FontWeight.SemiBold
                        },
                        new TextBlock
                        {
                            Text = "This removes the managed profile directory. The default ~/.codex account is never deleted.",
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                },
                actions
            }
        };
        return dialog;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.LaunchRequested -= OnLaunchRequested;
        _viewModel.DeleteConfirmationRequested -= OnDeleteConfirmationRequested;
        foreach (var session in _sessions)
            session.Dispose();
        _sessions.Clear();
    }
}
