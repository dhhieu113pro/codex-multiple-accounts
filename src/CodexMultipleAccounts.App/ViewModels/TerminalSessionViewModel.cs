using AvaloniaTerminal;
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
        TerminalModel = new TerminalControlModel(new TerminalOptions
        {
            Cols = 120,
            Rows = 30,
            ReflowOnResize = false
        });
    }

    public string Title { get; }
    public string CodexHome { get; }
    public DateTimeOffset StartedAt { get; }
    public TerminalControlModel TerminalModel { get; }
    public string StartedAtText => $"Session started at {StartedAt:HH:mm:ss}";
    public string CodexHomeText => string.IsNullOrWhiteSpace(CodexHome) ? "CODEX_HOME" : $"CODEX_HOME: {CodexHome}";

    [ObservableProperty]
    private string _output = "Starting Codex…";

    [ObservableProperty]
    private string _input = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private bool _isRunning = true;

    public void AppendOutput(string text)
    {
        Output = string.IsNullOrEmpty(Output) || Output == "Starting Codex…" ? text : Output + text;
        TerminalModel.Feed(text);
    }

    public void MarkExited(int exitCode)
    {
        IsRunning = false;
        AppendOutput($"\r\nCodex exited with code {exitCode}.\r\n");
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        if (_sendInput is null || !IsRunning || string.IsNullOrEmpty(Input))
            return;

        var text = Input;
        Input = string.Empty;
        await _sendInput(text);
    }

    private bool CanSend() => _sendInput is not null && IsRunning;
}
