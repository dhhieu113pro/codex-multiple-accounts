using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodexMultipleAccounts.App.ViewModels;

public partial class TerminalSessionViewModel : ObservableObject
{
    private readonly Func<string, Task>? _sendInput;

    public TerminalSessionViewModel(string title, Func<string, Task>? sendInput = null, string? codexHome = null)
    {
        Title = title;
        CodexHome = codexHome ?? string.Empty;
        StartedAt = DateTimeOffset.Now;
        _sendInput = sendInput;
    }

    public string Title { get; }
    public string CodexHome { get; }
    public DateTimeOffset StartedAt { get; }
    public string StartedAtText => $"Session started at {StartedAt:HH:mm:ss}";
    public string CodexHomeText => string.IsNullOrWhiteSpace(CodexHome) ? "CODEX_HOME" : $"CODEX_HOME: {CodexHome}";

    [ObservableProperty]
    private string _output = "Starting Codex…";

    [ObservableProperty]
    private string _input = string.Empty;

    public void AppendOutput(string text)
    {
        Output = string.IsNullOrEmpty(Output) || Output == "Starting Codex…"
            ? text
            : Output + text;
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        if (_sendInput is null || string.IsNullOrEmpty(Input))
            return;

        var text = Input;
        Input = string.Empty;
        await _sendInput(text);
    }

    private bool CanSend() => _sendInput is not null;
}
