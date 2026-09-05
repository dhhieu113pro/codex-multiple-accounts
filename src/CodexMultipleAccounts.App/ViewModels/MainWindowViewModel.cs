using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodexMultipleAccounts.App.Services;
using CodexMultipleAccounts.Core.Activation;
using CodexMultipleAccounts.Core.Launching;
using CodexMultipleAccounts.Core.Profiles;

namespace CodexMultipleAccounts.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly ProfileService _profiles;
    private readonly CodexLaunchService _launchService;
    private readonly GlobalActivationService _activationService;
    private readonly IExternalTerminalLauncher _externalTerminalLauncher;
    private readonly string _defaultCodexHome;

    public MainWindowViewModel(
        ProfileService profiles,
        CodexLaunchService launchService,
        GlobalActivationService activationService,
        IExternalTerminalLauncher externalTerminalLauncher,
        string defaultCodexHome)
    {
        _profiles = profiles;
        _launchService = launchService;
        _activationService = activationService;
        _externalTerminalLauncher = externalTerminalLauncher;
        _defaultCodexHome = defaultCodexHome;
        WorkingDirectory = Environment.CurrentDirectory;
    }

    public ObservableCollection<CodexProfile> Profiles { get; } = [];

    public event EventHandler<LaunchRequestEventArgs>? LaunchRequested;
    public event EventHandler<DeleteProfileRequestEventArgs>? DeleteConfirmationRequested;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RenameSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(LaunchIsolatedCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenExternallyCommand))]
    [NotifyCanExecuteChangedFor(nameof(ActivateGloballyCommand))]
    private CodexProfile? selectedProfile;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateProfileCommand))]
    private string newProfileName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RenameSelectedCommand))]
    private string renameProfileName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LaunchIsolatedCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenExternallyCommand))]
    private string workingDirectory;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private bool isBusy;

    partial void OnSelectedProfileChanged(CodexProfile? value)
    {
        RenameProfileName = value?.Name ?? string.Empty;
    }

    public async Task InitializeAsync()
    {
        await ReloadProfilesAsync();
        StatusMessage = Profiles.Count == 0
            ? "Create a profile or import your current ~/.codex account."
            : "Ready";
    }

    private bool CanCreateProfile() => !IsBusy && !string.IsNullOrWhiteSpace(NewProfileName);

    [RelayCommand(CanExecute = nameof(CanCreateProfile))]
    private async Task CreateProfileAsync()
    {
        await RunBusyAsync(async () =>
        {
            var created = await _profiles.CreateAsync(NewProfileName);
            NewProfileName = string.Empty;
            await ReloadProfilesAsync(created.Id);
            StatusMessage = $"Created isolated profile '{created.Name}'. Launch it to sign in with Codex.";
        });
    }

    [RelayCommand]
    private async Task ImportDefaultAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!Directory.Exists(_defaultCodexHome))
                throw new DirectoryNotFoundException($"Default Codex home '{_defaultCodexHome}' was not found.");

            var name = string.IsNullOrWhiteSpace(NewProfileName) ? "Default" : NewProfileName.Trim();
            var imported = await _profiles.ImportDefaultAsync(name, _defaultCodexHome);
            NewProfileName = string.Empty;
            await ReloadProfilesAsync(imported.Id);
            StatusMessage = $"Imported current Codex state into isolated profile '{imported.Name}'.";
        });
    }

    private bool CanRenameSelected() =>
        !IsBusy && SelectedProfile is not null && !string.IsNullOrWhiteSpace(RenameProfileName);

    [RelayCommand(CanExecute = nameof(CanRenameSelected))]
    private async Task RenameSelectedAsync()
    {
        var profile = SelectedProfile;
        if (profile is null)
            return;

        await RunBusyAsync(async () =>
        {
            var renamed = await _profiles.RenameAsync(profile.Id, RenameProfileName);
            await ReloadProfilesAsync(renamed.Id);
            StatusMessage = $"Renamed profile to '{renamed.Name}'.";
        });
    }

    private bool CanDeleteSelected() => !IsBusy && SelectedProfile is not null;

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteSelectedAsync()
    {
        var profile = SelectedProfile;
        if (profile is null)
            return;

        var request = new DeleteProfileRequestEventArgs(profile);
        DeleteConfirmationRequested?.Invoke(this, request);
        if (!await request.GetResultAsync())
            return;

        await RunBusyAsync(async () =>
        {
            await _profiles.DeleteAsync(profile.Id);
            await ReloadProfilesAsync();
            StatusMessage = $"Deleted profile '{profile.Name}'.";
        });
    }

    private bool CanLaunchIsolated() =>
        !IsBusy && SelectedProfile is not null && Directory.Exists(WorkingDirectory);

    [RelayCommand(CanExecute = nameof(CanLaunchIsolated))]
    private Task LaunchIsolatedAsync()
    {
        var profile = SelectedProfile;
        if (profile is null)
            return Task.CompletedTask;

        var spec = _launchService.Build(profile, WorkingDirectory, []);
        LaunchRequested?.Invoke(this, new LaunchRequestEventArgs(profile, spec));
        StatusMessage = $"Launching '{profile.Name}' with isolated CODEX_HOME.";
        return Task.CompletedTask;
    }

    private bool CanOpenExternally() =>
        !IsBusy && SelectedProfile is not null && Directory.Exists(WorkingDirectory);

    [RelayCommand(CanExecute = nameof(CanOpenExternally))]
    private async Task OpenExternallyAsync()
    {
        var profile = SelectedProfile;
        if (profile is null)
            return;

        await RunBusyAsync(async () =>
        {
            var spec = _launchService.Build(profile, WorkingDirectory, []);
            await _externalTerminalLauncher.LaunchAsync(spec);
            StatusMessage = $"Opened '{profile.Name}' in an external terminal with isolated CODEX_HOME.";
        });
    }

    private bool CanActivateGlobally() => !IsBusy && SelectedProfile is not null;

    [RelayCommand(CanExecute = nameof(CanActivateGlobally))]
    private async Task ActivateGloballyAsync()
    {
        var profile = SelectedProfile;
        if (profile is null)
            return;

        await RunBusyAsync(async () =>
        {
            var result = await _activationService.ActivateAsync(profile, _defaultCodexHome);
            await _profiles.SetGloballyActiveAsync(profile.Id);
            await ReloadProfilesAsync(profile.Id);
            StatusMessage = result.HadPreviousDefault
                ? $"'{profile.Name}' is globally active. Previous ~/.codex saved at {result.BackupDirectory}."
                : $"'{profile.Name}' is globally active.";
        });
    }

    [RelayCommand]
    private async Task RefreshAsync() => await RunBusyAsync(() => ReloadProfilesAsync(SelectedProfile?.Id));

    private async Task ReloadProfilesAsync(Guid? selectedId = null)
    {
        selectedId ??= SelectedProfile?.Id;
        var profiles = await _profiles.ListAsync();
        Profiles.Clear();
        foreach (var profile in profiles.OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase))
            Profiles.Add(profile);

        SelectedProfile = selectedId is null
            ? Profiles.FirstOrDefault()
            : Profiles.FirstOrDefault(profile => profile.Id == selectedId) ?? Profiles.FirstOrDefault();
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        NotifyCommandState();
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyCommandState();
        }
    }

    private void NotifyCommandState()
    {
        CreateProfileCommand.NotifyCanExecuteChanged();
        RenameSelectedCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
        LaunchIsolatedCommand.NotifyCanExecuteChanged();
        OpenExternallyCommand.NotifyCanExecuteChanged();
        ActivateGloballyCommand.NotifyCanExecuteChanged();
    }
}

public sealed class LaunchRequestEventArgs(CodexProfile profile, LaunchSpec spec) : EventArgs
{
    public CodexProfile Profile { get; } = profile;
    public LaunchSpec Spec { get; } = spec;
}

public sealed class DeleteProfileRequestEventArgs(CodexProfile profile) : EventArgs
{
    private readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CodexProfile Profile { get; } = profile;
    public void Complete(bool result) => _completion.TrySetResult(result);
    public Task<bool> GetResultAsync() => _completion.Task;
}
