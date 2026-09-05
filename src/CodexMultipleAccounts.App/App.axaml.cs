using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CodexMultipleAccounts.App.Services;
using CodexMultipleAccounts.App.ViewModels;
using CodexMultipleAccounts.Core.Activation;
using CodexMultipleAccounts.Core.Launching;
using CodexMultipleAccounts.Core.Profiles;
using CodexMultipleAccounts.Core.Storage;

namespace CodexMultipleAccounts.App;

public sealed class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var runtimePaths = RuntimePaths.Create();
            var paths = new AppPaths(runtimePaths.AppDataRoot);
            var profileService = new ProfileService(paths, new ProfileStore(paths));
            var launchService = new CodexLaunchService();
            var activationService = new GlobalActivationService(profileService);
            var externalLauncher = new ExternalTerminalLauncher();
            var viewModel = new MainWindowViewModel(
                profileService,
                launchService,
                activationService,
                externalLauncher,
                runtimePaths.DefaultCodexHome);

            desktop.MainWindow = new MainWindow(viewModel);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
