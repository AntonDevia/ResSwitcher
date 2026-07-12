using System.Runtime.InteropServices;

namespace ResSwitcher.Core;

public enum KeySendMode
{
    /// <summary>Real OS-level physical keystroke via SendInput. Goes to whatever window currently has input focus.</summary>
    Global,
    /// <summary>WM_KEYDOWN/WM_KEYUP posted straight to a specific window's message queue, bypassing focus.</summary>
    DirectToWindow
}

/// <summary>
/// Sends a synthetic F11 press, either as a real OS-level keystroke
/// (SendInput) or posted directly to a window's message queue
/// (PostMessage). Used for the optional F11 workaround: some game engines
/// render one stale/cropped frame after a resolution change is applied
/// while the game regains focus (e.g. alt-tabbing back in); toggling
/// fullscreen via F11 forces the engine to resync its viewport. Which
/// delivery mode actually works depends on the game engine, hence the
/// toggle -- Java/LWJGL titles in particular are inconsistent about which
/// one they react to.
/// </summary>
public static class KeySender
{
    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_F11 = 0x7A;
    private const ushort SCAN_F11 = 0x0057;

    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;

    // INPUT is a native union of MOUSEINPUT / KEYBDINPUT / HARDWAREINPUT.
    // The largest member (MOUSEINPUT) has 6 fields; on x64 the whole union
    // is padded to that size. Declaring only the KEYBDINPUT fields without
    // matching that layout makes SendInput silently misread/drop the event.
    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT
    {
        [FieldOffset(0)] public int type;
        [FieldOffset(8)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private static void SendGlobalKeyEvent(bool keyUp)
    {
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            ki = new KEYBDINPUT
            {
                wVk = VK_F11,
                wScan = SCAN_F11,
                dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            }
        };
        // INPUT is 40 bytes on x64 (8-byte type slot + 32-byte union payload).
        SendInput(1, new[] { input }, 40);
    }

    private static void SendDirectKeyEvent(IntPtr targetWindow)
    {
        if (targetWindow == IntPtr.Zero) return;
        PostMessage(targetWindow, WM_KEYDOWN, (IntPtr)VK_F11, IntPtr.Zero);
        PostMessage(targetWindow, WM_KEYUP, (IntPtr)VK_F11, IntPtr.Zero);
    }

    /// <summary>Simulates a single F11 press using the given delivery mode.</summary>
    public static void SendF11(KeySendMode mode)
    {
        if (mode == KeySendMode.Global)
        {
            SendGlobalKeyEvent(keyUp: false);
            SendGlobalKeyEvent(keyUp: true);
        }
        else
        {
            SendDirectKeyEvent(GetForegroundWindow());
        }
    }

    /// <summary>Presses F11 twice with a short pause in between (what fixes the crop manually).</summary>
    public static async Task SendDoubleF11Async(KeySendMode mode, int pauseMs = 400)
    {
        SendF11(mode);
        await Task.Delay(pauseMs);
        SendF11(mode);
    }
}
