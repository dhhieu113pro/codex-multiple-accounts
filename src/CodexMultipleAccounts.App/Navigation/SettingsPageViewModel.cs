using CodexMultipleAccounts.Core.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodexMultipleAccounts.App.Navigation;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly AppSettingsService _service;
    private readonly Action<AppSettings> _apply;
    private AppSettings _saved;

    public SettingsPageViewModel(AppSettingsService service, AppSettings initial, Action<AppSettings> apply)
    {
        _service = service;
        _saved = initial;
        _apply = apply;
        Restore(initial);
    }

    [ObservableProperty] private string _defaultWorkspace = string.Empty;
    [ObservableProperty] private string _codexExecutable = "codex";
    [ObservableProperty] private string _antigravityExecutable = string.Empty;
    [ObservableProperty] private AppAppearance _appearance;
    [ObservableProperty] private string _status = string.Empty;
    [ObservableProperty] private bool _isSaving;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving) return;
        IsSaving = true;
        try
        {
            var settings = new AppSettings(DefaultWorkspace, CodexExecutable, AntigravityExecutable, Appearance);
            await _service.SaveAsync(settings);
            _saved = await _service.LoadAsync();
            Restore(_saved);
            _apply(_saved);
            Status = "Settings saved. New launches will use the updated configuration.";
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Status = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Reset()
    {
        Restore(_service.Defaults());
        Status = "Defaults restored in the form. Select Save to apply them.";
    }

    [RelayCommand]
    private void Revert()
    {
        Restore(_saved);
        Status = "Unsaved changes discarded.";
    }

    private void Restore(AppSettings settings)
    {
        DefaultWorkspace = settings.DefaultWorkspace;
        CodexExecutable = settings.CodexExecutable;
        AntigravityExecutable = settings.AntigravityExecutable;
        Appearance = settings.Appearance;
    }
}
