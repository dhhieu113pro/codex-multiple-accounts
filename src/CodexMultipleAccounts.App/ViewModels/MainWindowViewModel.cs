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
    private readonly CodexLaunchService _launch = new();
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
        var testing = new CodexProfile(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Testing", Path.Combine(home, ".codex-profiles", "testing"), DateTimeOffset.Now.AddHours(-2), false);

        Profiles.Add(personal);
        Profiles.Add(work);
        Profiles.Add(testing);

        ProfileCards.Add(new ProfileCardViewModel(personal, Accents[0], "me@example.com", 78, 62, "78%", "62%"));
        ProfileCards.Add(new ProfileCardViewModel(work, Accents[1], "work@example.com", 91, 74, "91%", "74%"));
        ProfileCards.Add(new ProfileCardViewModel(testing, Accents[2], "test@example.com", 43, 28, "43%", "28%"));

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
        Status = "Three isolated Codex profiles ready — launch them in parallel or activate one globally.";
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
            ProfileCards.Add(new ProfileCardViewModel(profile, Accents[index % Accents.Length]));
        }
    }

    [RelayCommand]
    private async Task Create()
    {
        var created = await _profiles.CreateAsync("Profile " + (Profiles.Count + 1));
        SelectedProfile = created;
        await ReloadAsync();
        Status = "Isolated profile created.";
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
    private void Launch()
    {
        LaunchProfile(SelectedProfileCard);
    }

    [RelayCommand]
    private void LaunchProfile(ProfileCardViewModel? card)
    {
        if (card is null)
            return;

        Select(card);
        var profile = card.Profile;
        var spec = _launch.Create(profile, Environment.CurrentDirectory);
        var session = _embedded.Launch(profile.Name, spec);
        Sessions.Add(session);
        SelectedSession = session;
        Status = $"Launched {profile.Name} with isolated CODEX_HOME.";
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void External()
    {
        ExternalProfile(SelectedProfileCard);
    }

    [RelayCommand]
    private void ExternalProfile(ProfileCardViewModel? card)
    {
        if (card is null)
            return;

        Select(card);
        var profile = card.Profile;
        _external.Launch(_launch.Create(profile, Environment.CurrentDirectory));
        Status = $"Opened {profile.Name} externally.";
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task Activate()
    {
        await ActivateProfile(SelectedProfileCard);
    }

    [RelayCommand]
    private async Task ActivateProfile(ProfileCardViewModel? card)
    {
        if (card is null)
            return;

        Select(card);
        var profile = card.Profile;
        await _activation.ActivateAsync(profile);
        await ReloadAsync();
        Status = $"{profile.Name} is now globally active. Existing default state was backed up.";
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
