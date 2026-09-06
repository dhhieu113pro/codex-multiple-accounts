using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using CodexMultipleAccounts.App.Branding;

namespace CodexMultipleAccounts.App;

public partial class MainWindow : Window
{
    private bool _layoutReady;
    private bool _compactAccountsOpen;

    public MainWindow()
    {
        InitializeComponent();
        Icon = BrandAssets.CreateWindowIcon();
        BrandRow.Children[0] = new Image
        {
            Source = BrandAssets.CreateBitmap(),
            Width = 32,
            Height = 32,
            Stretch = Avalonia.Media.Stretch.Uniform
        };
        _layoutReady = true;
        Opened += (_, _) => ApplyAdaptiveLayout(Bounds.Width);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (_layoutReady && change.Property == BoundsProperty && change.NewValue is Rect bounds && bounds.Width > 0)
        {
            ApplyAdaptiveLayout(bounds.Width);
        }
    }

    private void ToggleAccountsPane(object? sender, RoutedEventArgs e)
    {
        _compactAccountsOpen = !_compactAccountsOpen;
        ApplyAdaptiveLayout(Bounds.Width);
    }

    private void CloseAccountsOverlay(object? sender, PointerPressedEventArgs e)
    {
        if (!_compactAccountsOpen)
        {
            return;
        }

        _compactAccountsOpen = false;
        ApplyAdaptiveLayout(Bounds.Width);
    }

    private void ApplyAdaptiveLayout(double width)
    {
        var layout = AdaptiveLayoutPolicy.Resolve(width, _compactAccountsOpen);

        RootShellGrid.ColumnDefinitions[0].Width = new GridLength(layout.NavigationWidth);
        WorkspaceShell.ColumnDefinitions[0].Width = layout.UseAccountsOverlay
            ? new GridLength(0)
            : new GridLength(layout.AccountsWidth);

        BrandText.IsVisible = layout.ShowNavigationLabels;
        NavAccountsText.IsVisible = layout.ShowNavigationLabels;
        NavSessionsText.IsVisible = layout.ShowNavigationLabels;
        NavUsageText.IsVisible = layout.ShowNavigationLabels;
        NavSettingsText.IsVisible = layout.ShowNavigationLabels;
        NavDocsText.IsVisible = layout.ShowNavigationLabels;
        NavAboutText.IsVisible = layout.ShowNavigationLabels;

        var navigationAlignment = layout.ShowNavigationLabels
            ? HorizontalAlignment.Left
            : HorizontalAlignment.Center;

        BrandRow.HorizontalAlignment = navigationAlignment;
        NavAccountsButton.HorizontalContentAlignment = navigationAlignment;
        NavSessionsButton.HorizontalContentAlignment = navigationAlignment;
        NavUsageButton.HorizontalContentAlignment = navigationAlignment;
        NavSettingsButton.HorizontalContentAlignment = navigationAlignment;
        NavDocsButton.HorizontalContentAlignment = navigationAlignment;
        NavAboutButton.HorizontalContentAlignment = navigationAlignment;

        AccountsPane.IsVisible = layout.ShowAccountsPane;
        AccountsToggleButton.IsVisible = layout.ShowAccountsToggle;
        AccountsOverlayScrim.IsVisible = layout.UseAccountsOverlay;

        if (layout.UseAccountsOverlay)
        {
            Grid.SetColumn(AccountsPane, 0);
            Grid.SetColumnSpan(AccountsPane, 2);
            AccountsPane.Width = layout.AccountsWidth;
            AccountsPane.HorizontalAlignment = HorizontalAlignment.Left;
            AccountsPane.Margin = new Thickness(10);
            AccountsPane.BorderThickness = new Thickness(1);
            AccountsPane.CornerRadius = new CornerRadius(12);
        }
        else
        {
            Grid.SetColumn(AccountsPane, 0);
            Grid.SetColumnSpan(AccountsPane, 1);
            AccountsPane.Width = double.NaN;
            AccountsPane.HorizontalAlignment = HorizontalAlignment.Stretch;
            AccountsPane.Margin = new Thickness(0);
            AccountsPane.BorderThickness = new Thickness(0, 0, 1, 0);
            AccountsPane.CornerRadius = new CornerRadius(0);
        }

        TerminalHost.Margin = layout.Mode == AdaptiveLayoutMode.TerminalFirst
            ? new Thickness(10)
            : new Thickness(14);

        if (layout.Mode != AdaptiveLayoutMode.TerminalFirst)
        {
            _compactAccountsOpen = false;
        }
    }
}
