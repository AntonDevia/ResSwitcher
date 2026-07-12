using System.Runtime.InteropServices;

namespace ResSwitcher.Core;

public class ForegroundChangedEventArgs : EventArgs
{
    public int ProcessId { get; init; }
}

/// <summary>
/// Wraps SetWinEventHook(EVENT_SYSTEM_FOREGROUND) to notify whenever the
/// active window changes, so callers can tell whether the game or something
/// else (alt-tab target, another window) currently has focus.
/// </summary>
public class ForegroundWindowWatcher : IDisposable
{
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private readonly WinEventDelegate _procDelegate;
    private IntPtr _hook;

    public event EventHandler<ForegroundChangedEventArgs>? ForegroundChanged;

    public ForegroundWindowWatcher()
    {
        // Keep a strong reference to the delegate for the lifetime of the hook,
        // otherwise the GC can collect it and native code calls into freed memory.
        _procDelegate = OnWinEvent;
        _hook = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _procDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
    }

    private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero) return;
        GetWindowThreadProcessId(hwnd, out uint pid);
        ForegroundChanged?.Invoke(this, new ForegroundChangedEventArgs { ProcessId = (int)pid });
    }

    public static int GetForegroundProcessId()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return 0;
        GetWindowThreadProcessId(hwnd, out uint pid);
        return (int)pid;
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
