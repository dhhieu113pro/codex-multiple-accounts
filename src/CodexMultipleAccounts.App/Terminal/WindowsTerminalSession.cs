using System.Text;
using System.Threading.Channels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using CodexMultipleAccounts.App.Native;
using CodexMultipleAccounts.Core.Launching;
using Porta.Pty;

namespace CodexMultipleAccounts.App.Terminal;

public sealed class WindowsTerminalSession : ITerminalSession
{
    private readonly Grid _view = new();
    private readonly WindowsTerminalNativeHost _host = new();
    private readonly ScrollBar _scrollBar = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Channel<string> _input = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    private IPtyConnection? _pty;
    private bool _syncingScrollBar;
    private int _started;
    private int _stopped;
    private int _exitRaised;

    public WindowsTerminalSession()
    {
        _view.ColumnDefinitions = new ColumnDefinitions("*,Auto");
        _view.Children.Add(_host);
        _scrollBar.Orientation = Avalonia.Layout.Orientation.Vertical;
        _scrollBar.AllowAutoHide = false;
        _scrollBar.IsVisible = true;
        _scrollBar.Minimum = 0;
        _scrollBar.Maximum = 0;
        _scrollBar.Value = 0;
        _scrollBar.IsEnabled = false;
        Grid.SetColumn(_scrollBar, 1);
        _view.Children.Add(_scrollBar);

        _host.InputGenerated += OnInputGenerated;
        _host.TerminalResized += OnTerminalResized;
        _host.ScrollChanged += OnScrollChanged;
        _scrollBar.PropertyChanged += OnScrollBarPropertyChanged;
    }

    public static bool IsSupported => WindowsTerminal.IsSupported;
    public Control View => _view;
    public string BackendName => "Windows Terminal";
    public event EventHandler<TerminalExitedEventArgs>? Exited;

    public async Task StartAsync(LaunchSpec spec, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The embedded Windows Terminal backend is available only on Windows.");
        if (!WindowsTerminal.IsSupported)
            throw new PlatformNotSupportedException("Microsoft.Terminal.Control.dll is not available for this architecture.");
        if (Interlocked.Exchange(ref _started, 1) != 0)
            throw new InvalidOperationException("The terminal session has already been started.");

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        await _host.WaitForNativeCreatedAsync(linked.Token).ConfigureAwait(false);

        var options = new PtyOptions
        {
            Name = spec.FileName,
            App = spec.FileName,
            CommandLine = spec.Arguments.ToArray(),
            Cwd = spec.WorkingDirectory,
            Cols = 100,
            Rows = 30,
            Environment = new Dictionary<string, string>(spec.Environment, StringComparer.Ordinal)
        };

        _pty = await PtyProvider.SpawnAsync(options, linked.Token).ConfigureAwait(false);
        _pty.ProcessExited += OnProcessExited;
        _ = ReadOutputAsync(_cts.Token);
        _ = WriteInputAsync(_cts.Token);
    }

    public void Stop()
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
            return;

        _input.Writer.TryComplete();
        _cts.Cancel();
        _host.InputGenerated -= OnInputGenerated;
        _host.TerminalResized -= OnTerminalResized;
        _host.ScrollChanged -= OnScrollChanged;
        _scrollBar.PropertyChanged -= OnScrollBarPropertyChanged;

        var pty = Interlocked.Exchange(ref _pty, null);
        if (pty is null)
            return;
        pty.ProcessExited -= OnProcessExited;
        try { pty.Kill(); }
        catch { }
        finally { pty.Dispose(); }
    }

    public void Dispose()
    {
        Stop();
        _cts.Dispose();
    }

    private async Task ReadOutputAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[0x10000];
        var chars = new char[Encoding.UTF8.GetMaxCharCount(buffer.Length)];
        var decoder = Encoding.UTF8.GetDecoder();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var pty = _pty;
                if (pty is null)
                    break;
                var read = await pty.ReaderStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    RaiseExited(pty.ExitCode);
                    break;
                }
                var charCount = decoder.GetChars(buffer, 0, read, chars, 0, flush: false);
                if (charCount > 0)
                    _host.SendOutput(new string(chars, 0, charCount));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested) { }
    }

    private async Task WriteInputAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var text in _input.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                if (string.IsNullOrEmpty(text))
                    continue;
                var pty = _pty;
                if (pty is null)
                    break;
                var bytes = Encoding.UTF8.GetBytes(text);
                await pty.WriterStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                await pty.WriterStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested) { }
    }

    private void OnInputGenerated(string text)
    {
        if (!string.IsNullOrEmpty(text))
            _input.Writer.TryWrite(text);
    }

    private void OnTerminalResized(int cols, int rows)
    {
        if (cols <= 0 || rows <= 0)
            return;
        try { _pty?.Resize(cols, rows); }
        catch (ObjectDisposedException) { }
    }

    private void OnScrollChanged(int viewTop, int viewHeight, int bufferSize) =>
        Dispatcher.UIThread.Post(() => SyncScrollBar(viewTop, viewHeight, bufferSize));

    private void SyncScrollBar(int viewTop, int viewHeight, int bufferSize)
    {
        if (Volatile.Read(ref _stopped) != 0)
            return;
        var viewport = Math.Max(0, viewHeight);
        var maxTop = Math.Max(0, bufferSize - viewport);
        _syncingScrollBar = true;
        try
        {
            _scrollBar.Minimum = 0;
            _scrollBar.Maximum = maxTop;
            _scrollBar.ViewportSize = viewport;
            _scrollBar.Value = Math.Clamp(viewTop, 0, maxTop);
            _scrollBar.IsEnabled = maxTop > 0;
        }
        finally { _syncingScrollBar = false; }
    }

    private void OnScrollBarPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_syncingScrollBar || e.Property != RangeBase.ValueProperty)
            return;
        _host.UserScroll((int)Math.Round(_scrollBar.Value));
    }

    private void OnProcessExited(object? sender, PtyExitedEventArgs e) => RaiseExited(e.ExitCode);

    private void RaiseExited(int exitCode)
    {
        if (Interlocked.Exchange(ref _exitRaised, 1) == 0)
            Exited?.Invoke(this, new TerminalExitedEventArgs(exitCode));
    }
}
