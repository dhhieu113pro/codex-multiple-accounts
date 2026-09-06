using System.Collections.ObjectModel;
using CodexMultipleAccounts.App.Terminal;
using CodexMultipleAccounts.Core.Activation;
using CodexMultipleAccounts.Core.Launching;
using CodexMultipleAccounts.Core.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodexMultipleAccounts.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private static readonly string[] Accents = ["#24C98A", "#2F7CF6", "#9A45F5", "#F59E42"];

    private readonly ProfileService _profiles;
    private readonly AntigravityProfileService _antigravityProfiles;
    private readonly CodexLaunchService _launch = new();
    private readonly AntigravityLaunchService _antigravityLaunch = new();
    private readonly AntigravityProcessManager _antigravityProcesses = new();
    private readonly GlobalActivationService _activation;
    private readonly ProcessTerminalLauncher _embedded = new();
    private readonly ExternalTerminalLauncher _external = new();

    public ObservableCollection<CodexProfile> Profiles { get; } = [];
    public ObservableCollection<ProfileCardViewModel> ProfileCards { get; } = [];
    public ObservableCollection<TerminalSessionViewModel> Sessions { get; } = [];

    [ObservableProperty]
    private CodexProfile? _selectedProfile;

    [ObservableProperty]
    private ProfileCardViewModel? _selectedProfileCard;

    [ObservableProperty]
    private TerminalSessionViewModel? _selectedSession;

    [ObservableProperty]
    private string _status = "Create or import a profile to begin.";

    public MainWindowViewModel(string root, string defaultHome)
    {
        _profiles = new ProfileService(root, defaultHome);
        _antigravityProfiles = new AntigravityProfileService(_profiles);
        _activation = new GlobalActivationService(_profiles, root, defaultHome);

        if (Environment.GetEnvironmentVariable("CODEX_MULTIPLE_ACCOUNTS_SCREENSHOT") == "1")
            LoadScreenshotDemo();
        else
            _ = ReloadAsync();
    }

    private void LoadScreenshotDemo()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var personal = new CodexProfile(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Personal", Path.Combine(home, ".codex-profiles", "personal"), DateTimeOffset.Now, true);
        var work = new CodexProfile(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Work", Path.Combine(home, ".codex-profiles", "work"), DateTimeOffset.Now.AddMinutes(-18), false);
        var antigravity = new CodexProfile(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Antigravity Work", Path.Combine(home, ".antigravity-profiles", "work"), DateTimeOffset.Now.AddHours(-2), false, AccountProvider.Antigravity, AntigravityProfileMode.Full);

        Profiles.Add(personal);
        Profiles.Add(work);
        Profiles.Add(antigravity);

        ProfileCards.Add(new ProfileCardViewModel(personal, Accents[0], "me@example.com", 78, 62, "78%", "62%"));
        ProfileCards.Add(new ProfileCardViewModel(work, Accents[1], "work@example.com", 91, 74, "91%", "74%"));
        ProfileCards.Add(new ProfileCardViewModel(antigravity, Accents[2], "Antigravity Full profile", isRunning: true));

        SelectedProfileCard = ProfileCards[0];

        var session = new TerminalSessionViewModel("Personal", codexHome: personal.CodexHome)
        {
            Output = $"""
                     Welcome to Codex (Personal)
                     Using CODEX_HOME: {personal.CodexHome}

                     CODEX
                     Your AI pair programmer

                     /help     Show available commands
                     /status   Show current workspace status
                     /clear    Clear the conversation
                     /exit     Exit Codex

                     >
                     """
        };
        Sessions.Add(session);
        SelectedSession = session;
        Status = "Codex accounts are independently isolated. Antigravity profiles isolate filesystem state, but current Antigravity authentication still shares the OS credential store.";
    }

    private async Task ReloadAsync()
    {
        var selectedId = SelectedProfileCard?.Profile.Id ?? SelectedProfile?.Id;

        Profiles.Clear();
        foreach (var profile in await _profiles.ListAsync())
            Profiles.Add(profile);

        RebuildCards();
        SelectedProfileCard = ProfileCards.FirstOrDefault(x => x.Profile.Id == selectedId) ?? ProfileCards.FirstOrDefault();
    }

    private void RebuildCards()
    {
        ProfileCards.Clear();
        for (var index = 0; index < Profiles.Count; index++)
        {
            var profile = Profiles[index];
            var running = profile.Provider == AccountProvider.Antigravity && _antigravityProcesses.IsRunning(profile.Id);
            ProfileCards.Add(new ProfileCardViewModel(profile, Accents[index % Accents.Length], isRunning: running));
        }
    }

    [RelayCommand]
    private async Task Create()
    {
        var created = await _profiles.CreateAsync("Codex " + (Profiles.Count(x => x.Provider == AccountProvider.Codex) + 1));
        SelectedProfile = created;
        await ReloadAsync();
        Status = "Isolated Codex profile created.";
    }

    [RelayCommand]
    private async Task CreateAntigravityFull()
    {
        var created = await _antigravityProfiles.CreateAsync("Antigravity " + (Profiles.Count(x => x.Provider == AccountProvider.Antigravity) + 1), AntigravityProfileMode.Full);
        SelectedProfile = created;
        await ReloadAsync();
        Status = "Antigravity Full profile created. Filesystem state is isolated; OS credential-store authentication is shared.";
    }

    [RelayCommand]
    private async Task CreateAntigravityShared()
    {
        var created = await _antigravityProfiles.CreateAsync("Antigravity Shared " + (Profiles.Count(x => x.Provider == AccountProvider.Antigravity) + 1), AntigravityProfileMode.Shared);
        SelectedProfile = created;
        await ReloadAsync();
        Status = "Antigravity Shared profile created. Filesystem state is isolated; OS credential-store authentication is shared.";
    }

    [RelayCommand]
    private async Task Import()
    {
        var imported = await _profiles.ImportDefaultAsync("Imported " + (Profiles.Count + 1));
        SelectedProfile = imported;
        await ReloadAsync();
        Status = "Current Codex home imported without modifying it.";
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Launch() => LaunchProfile(SelectedProfileCard);

    [RelayCommand]
    private void LaunchProfile(ProfileCardViewModel? card)
    {
        if (card is null)
            return;

        Select(card);
        if (card.IsAntigravity)
        {
            StartAntigravity(card, restart: false);
            return;
        }

        var profile = card.Profile;
        var spec = _launch.Create(profile, Environment.CurrentDirectory);
        var session = _embedded.Launch(profile.Name, spec);
        Sessions.Add(session);
        SelectedSession = session;
        Status = $"Launched {profile.Name} with isolated CODEX_HOME.";
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void External() => ExternalProfile(SelectedProfileCard);

    [RelayCommand]
    private void ExternalProfile(ProfileCardViewModel? card)
    {
        if (card is null)
            return;

        Select(card);
        if (card.IsAntigravity)
        {
            StartAntigravity(card, restart: false);
            return;
        }

        var profile = card.Profile;
        _external.Launch(_launch.Create(profile, Environment.CurrentDirectory));
        Status = $"Opened {profile.Name} externally.";
    }

    [RelayCommand]
    private void StopAntigravityProfile(ProfileCardViewModel? card)
    {
        if (card?.IsAntigravity != true)
            return;

        _antigravityProcesses.Stop(card.Profile.Id);
        RebuildCards();
        Status = $"Stopped {card.Name}.";
    }

    [RelayCommand]
    private void RestartAntigravityProfile(ProfileCardViewModel? card)
    {
        if (card?.IsAntigravity != true)
            return;

        StartAntigravity(card, restart: true);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task Activate() => await ActivateProfile(SelectedProfileCard);

    [RelayCommand]
    private async Task ActivateProfile(ProfileCardViewModel? card)
    {
        if (card is null)
            return;

        Select(card);
        if (!card.IsCodex)
        {
            Status = "Global activation is only available for Codex. Antigravity uses filesystem-isolated launch profiles.";
            return;
        }

        var profile = card.Profile;
        await _activation.ActivateAsync(profile);
        await ReloadAsync();
        Status = $"{profile.Name} is now globally active. Existing default state was backed up.";
    }

    private void StartAntigravity(ProfileCardViewModel card, bool restart)
    {
        var profile = card.Profile;
        var platform = AntigravityLaunchService.CurrentPlatform();
        var executable = AntigravityExecutableLocator.Resolve(platform);
        var spec = _antigravityLaunch.Create(profile, platform, executable, Environment.CurrentDirectory);

        if (restart)
            _antigravityProcesses.Restart(profile, spec);
        else
            _antigravityProcesses.Start(profile, spec);

        RebuildCards();
        SelectedProfileCard = ProfileCards.FirstOrDefault(x => x.Profile.Id == profile.Id);
        Status = $"{(restart ? "Restarted" : "Started")} {profile.Name}. Filesystem state is isolated; Antigravity's OS credential-store login is shared across profiles.";
    }

    private void Select(ProfileCardViewModel card)
    {
        SelectedProfileCard = card;
        SelectedProfile = card.Profile;
    }

    private bool HasSelection() => SelectedProfileCard is not null;

    partial void OnSelectedProfileCardChanged(ProfileCardViewModel? value)
    {
        SelectedProfile = value?.Profile;
        LaunchCommand.NotifyCanExecuteChanged();
        ExternalCommand.NotifyCanExecuteChanged();
        ActivateCommand.NotifyCanExecuteChanged();
    }
}
