using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using CodexMultipleAccounts.App.Native;

namespace CodexMultipleAccounts.App.Terminal;

internal sealed class WindowsTerminalNativeHost : NativeControlHost
{
    private const uint WM_SETFOCUS = 0x0007;
    private const uint WM_KILLFOCUS = 0x0008;
    private const uint WM_MOUSEACTIVATE = 0x0021;
    private const uint WM_WINDOWPOSCHANGED = 0x0047;
    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint WM_CHAR = 0x0102;
    private const uint WM_SYSKEYDOWN = 0x0104;
    private const uint WM_SYSKEYUP = 0x0105;
    private const uint WM_MOUSEWHEEL = 0x020A;
    private const uint SWP_NOSIZE = 0x0001;
    private const int WHEEL_DELTA = 120;
    private const ushort VK_SHIFT = 0x10;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_INSERT = 0x2D;
    private const ushort VK_C = 0x43;
    private const ushort VK_V = 0x56;

    internal event Action<string>? InputGenerated;
    internal event Action<int, int>? TerminalResized;
    internal event Action<int, int, int>? ScrollChanged;

    internal bool NativeCreated => _terminal != 0 && _hwnd != 0;

    internal Task WaitForNativeCreatedAsync(CancellationToken cancellationToken) =>
        NativeCreated ? Task.CompletedTask : _nativeCreated.Task.WaitAsync(cancellationToken);

    internal void SendOutput(string text)
    {
        if (_terminal != 0 && !string.IsNullOrEmpty(text))
            WindowsTerminal.TerminalSendOutput(_terminal, text);
    }

    internal void UserScroll(int viewTop)
    {
        if (_terminal == 0)
            return;
        var maxTop = Math.Max(0, Volatile.Read(ref _bufferSize) - Math.Max(0, Volatile.Read(ref _viewHeight)));
        var target = Math.Clamp(viewTop, 0, maxTop);
        Volatile.Write(ref _viewTop, target);
        WindowsTerminal.TerminalUserScroll(_terminal, target);
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        try
        {
            WindowsTerminal.EnsureResolver();
            WindowsTerminal.AvoidBuggyTSFConsoleFlagsOnce();
            WindowsTerminal.CreateTerminalChecked(parent.Handle, out _hwnd, out _terminal);
            _writeCallback = OnNativeWrite;
            _scrollCallback = OnScroll;
            WindowsTerminal.TerminalRegisterWriteCallback(_terminal, _writeCallback);
            WindowsTerminal.TerminalRegisterScrollCallback(_terminal, _scrollCallback);
            _subclass = WindowsTerminalSubclass.Attach(_hwnd, HandleWindowMessage);
            AttachTopLevel();
            ApplyDpi();
            _nativeCreated.TrySetResult();
            return new PlatformHandle(_hwnd, "HWND");
        }
        catch (Exception ex)
        {
            CleanupNativeTerminal();
            _nativeCreated.TrySetException(ex);
            return base.CreateNativeControlCore(parent);
        }
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (_terminal != 0 || _hwnd != 0)
        {
            CleanupNativeTerminal();
            return;
        }
        base.DestroyNativeControlCore(control);
    }

    private void OnNativeWrite(nint text)
    {
        if (text == 0)
            return;
        try
        {
            var value = Marshal.PtrToStringUni(text);
            if (!string.IsNullOrEmpty(value))
                InputGenerated?.Invoke(value);
        }
        finally
        {
            Marshal.FreeCoTaskMem(text);
        }
    }

    private void OnScroll(int viewTop, int viewHeight, int bufferSize)
    {
        Volatile.Write(ref _viewTop, viewTop);
        Volatile.Write(ref _viewHeight, viewHeight);
        Volatile.Write(ref _bufferSize, bufferSize);
        ScrollChanged?.Invoke(viewTop, viewHeight, bufferSize);
    }

    private nint? HandleWindowMessage(uint message, nuint wParam, nint lParam)
    {
        if (_terminal == 0)
            return null;
        return message switch
        {
            WM_SETFOCUS => HandleFocus(true),
            WM_KILLFOCUS => HandleFocus(false),
            WM_MOUSEACTIVATE => HandleMouseActivate(),
            WM_KEYDOWN or WM_SYSKEYDOWN => HandleKeyDown(wParam, lParam),
            WM_KEYUP or WM_SYSKEYUP => HandleKeyUp(wParam, lParam),
            WM_CHAR => HandleChar(wParam, lParam),
            WM_WINDOWPOSCHANGED => HandleWindowPositionChanged(lParam),
            WM_MOUSEWHEEL => HandleMouseWheel(wParam),
            _ => null
        };
    }

    private nint? HandleFocus(bool focused)
    {
        WindowsTerminal.TerminalSetFocused(_terminal, focused ? (byte)1 : (byte)0);
        return null;
    }

    private nint? HandleMouseActivate()
    {
        WindowsTerminalSubclass.Focus(_hwnd);
        return null;
    }

    private nint HandleKeyDown(nuint wParam, nint lParam)
    {
        UnpackKeyMessage(wParam, lParam, out var virtualKey, out var scanCode, out var flags);
        if (TryHandleShortcut(virtualKey))
            return 0;
        WindowsTerminal.TerminalSendKeyEvent(_terminal, virtualKey, scanCode, flags, 1);
        return 0;
    }

    private nint HandleKeyUp(nuint wParam, nint lParam)
    {
        UnpackKeyMessage(wParam, lParam, out var virtualKey, out var scanCode, out var flags);
        if (_consumedKeys.Remove(virtualKey))
        {
            _suppressNextChar = false;
            return 0;
        }
        WindowsTerminal.TerminalSendKeyEvent(_terminal, virtualKey, scanCode, flags, 0);
        return 0;
    }

    private nint HandleChar(nuint wParam, nint lParam)
    {
        if (_suppressNextChar)
        {
            _suppressNextChar = false;
            return 0;
        }
        UnpackKeyMessage(wParam, lParam, out var character, out var scanCode, out var flags);
        WindowsTerminal.TerminalSendCharEvent(_terminal, character, scanCode, flags);
        return 0;
    }

    private bool TryHandleShortcut(ushort virtualKey)
    {
        if (_consumedKeys.Contains(virtualKey))
            return true;
        var control = WindowsTerminalSubclass.IsKeyDown(VK_CONTROL);
        var shift = WindowsTerminalSubclass.IsKeyDown(VK_SHIFT);

        if (virtualKey == VK_C && control)
        {
            var hasSelection = WindowsTerminal.TerminalIsSelectionActive(_terminal) != 0;
            if (!shift && !hasSelection)
                return false;
            if (hasSelection)
                QueueClipboardOperation(CopySelectionAsync);
            ConsumeShortcut(virtualKey, true);
            return true;
        }
        if (virtualKey == VK_V && control && shift)
        {
            QueueClipboardOperation(PasteAsync);
            ConsumeShortcut(virtualKey, true);
            return true;
        }
        if (virtualKey == VK_INSERT && shift)
        {
            QueueClipboardOperation(PasteAsync);
            ConsumeShortcut(virtualKey, false);
            return true;
        }
        return false;
    }

    private async Task CopySelectionAsync()
    {
        if (_terminal == 0 || WindowsTerminal.TerminalIsSelectionActive(_terminal) == 0)
            return;
        var pointer = WindowsTerminal.TerminalGetSelection(_terminal);
        if (pointer == 0)
            return;
        string? text;
        try { text = Marshal.PtrToStringUni(pointer); }
        finally { Marshal.FreeCoTaskMem(pointer); }
        if (string.IsNullOrEmpty(text))
            return;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
            await clipboard.SetTextAsync(text);
    }

    private async Task PasteAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
            return;
#pragma warning disable CS0618
        var text = await clipboard.GetTextAsync();
#pragma warning restore CS0618
        if (!string.IsNullOrEmpty(text))
            InputGenerated?.Invoke(text);
    }

    private void ConsumeShortcut(ushort virtualKey, bool suppressCharacter)
    {
        _consumedKeys.Add(virtualKey);
        _suppressNextChar = suppressCharacter;
    }

    private static void QueueClipboardOperation(Func<Task> operation) =>
        Dispatcher.UIThread.Post(() => _ = RunClipboardOperationAsync(operation));

    private static async Task RunClipboardOperationAsync(Func<Task> operation)
    {
        try { await operation(); }
        catch { }
    }

    private nint? HandleWindowPositionChanged(nint lParam)
    {
        if (_resizeInProgress || lParam == 0)
            return null;
        var position = Marshal.PtrToStructure<WindowPos>(lParam);
        if ((position.Flags & SWP_NOSIZE) != 0 || position.Width <= 0 || position.Height <= 0)
            return null;
        _resizeInProgress = true;
        try
        {
            var dimensions = WindowsTerminal.TriggerResizeChecked(_terminal, position.Width, position.Height);
            TerminalResized?.Invoke(Math.Max(1, dimensions.X), Math.Max(1, dimensions.Y));
        }
        finally { _resizeInProgress = false; }
        return null;
    }

    private nint? HandleMouseWheel(nuint wParam)
    {
        var delta = unchecked((short)((wParam >> 16) & 0xFFFF));
        if (delta == 0)
            return null;
        _wheelDelta += delta;
        var steps = _wheelDelta / WHEEL_DELTA;
        if (steps == 0)
            return null;
        _wheelDelta -= steps * WHEEL_DELTA;
        var viewTop = Volatile.Read(ref _viewTop);
        var viewHeight = Volatile.Read(ref _viewHeight);
        var bufferSize = Volatile.Read(ref _bufferSize);
        if (viewHeight <= 0 || bufferSize <= viewHeight)
            return null;
        var scrollLines = (int)Math.Min(WindowsTerminalSubclass.GetWheelScrollLines(), int.MaxValue);
        var target = Math.Clamp(viewTop - (steps * scrollLines), 0, Math.Max(0, bufferSize - viewHeight));
        if (target != viewTop)
            UserScroll(target);
        return null;
    }

    private void AttachTopLevel()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (ReferenceEquals(_topLevel, topLevel))
            return;
        if (_topLevel is not null)
            _topLevel.PropertyChanged -= OnTopLevelPropertyChanged;
        _topLevel = topLevel;
        if (_topLevel is not null)
            _topLevel.PropertyChanged += OnTopLevelPropertyChanged;
    }

    private void OnTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == "RenderScaling")
            ApplyDpi();
    }

    private void ApplyDpi()
    {
        if (_terminal == 0)
            return;
        var scaling = _topLevel?.RenderScaling ?? TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        WindowsTerminal.TerminalDpiChanged(_terminal, Math.Max(96, (int)Math.Round(scaling * 96.0)));
    }

    private void CleanupNativeTerminal()
    {
        if (_topLevel is not null)
        {
            _topLevel.PropertyChanged -= OnTopLevelPropertyChanged;
            _topLevel = null;
        }
        _consumedKeys.Clear();
        _suppressNextChar = false;
        _subclass?.Dispose();
        _subclass = null;
        if (_terminal != 0)
        {
            try { WindowsTerminal.DestroyTerminal(_terminal); }
            finally { _terminal = 0; _hwnd = 0; }
        }
        else _hwnd = 0;
        _writeCallback = null;
        _scrollCallback = null;
    }

    private static void UnpackKeyMessage(nuint wParam, nint lParam, out ushort virtualKey, out ushort scanCode, out ushort flags)
    {
        var raw = unchecked((ulong)(long)lParam);
        var scanCodeAndFlags = (raw >> 16) & 0xFFFF;
        scanCode = (ushort)(scanCodeAndFlags & 0x00FF);
        flags = (ushort)(scanCodeAndFlags & 0xFF00);
        virtualKey = (ushort)wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowPos
    {
        public nint Hwnd;
        public nint HwndInsertAfter;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public uint Flags;
    }

    private readonly TaskCompletionSource _nativeCreated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly HashSet<ushort> _consumedKeys = [];
    private nint _terminal;
    private nint _hwnd;
    private WindowsTerminal.WriteCallback? _writeCallback;
    private WindowsTerminal.ScrollCallback? _scrollCallback;
    private WindowsTerminalSubclass? _subclass;
    private TopLevel? _topLevel;
    private int _viewTop;
    private int _viewHeight;
    private int _bufferSize;
    private int _wheelDelta;
    private bool _resizeInProgress;
    private bool _suppressNextChar;
}
