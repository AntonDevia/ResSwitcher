using System.Runtime.InteropServices;

namespace ResSwitcher.Core;

public record DisplayMode(int Width, int Height, int RefreshRate, int BitsPerPel)
{
    public override string ToString() => $"{Width}x{Height} @ {RefreshRate}Hz";
}

public enum ApplyResult
{
    Success,
    RestartRequired,
    BadMode,
    BadFlags,
    Failed,
    NoModeFound
}

/// <summary>
/// Thin wrapper around the Win32 display-settings API. Applies and reverts
/// resolution changes on the primary monitor without touching the registry
/// (CDS_FULLSCREEN / no CDS_UPDATEREGISTRY), so a crash never leaves the
/// system stuck on a weird resolution.
/// </summary>
public static class DisplayManager
{
    private const int ENUM_CURRENT_SETTINGS = -1;
    private const int DM_PELSWIDTH = 0x00080000;
    private const int DM_PELSHEIGHT = 0x00100000;
    private const int DM_DISPLAYFREQUENCY = 0x00400000;
    private const int DM_BITSPERPEL = 0x00040000;

    private const int CDS_FULLSCREEN = 0x4;
    private const int CDS_TEST = 0x2;

    private const int DISP_CHANGE_SUCCESSFUL = 0;
    private const int DISP_CHANGE_RESTART = 1;
    private const int DISP_CHANGE_FAILED = -1;
    private const int DISP_CHANGE_BADMODE = -2;
    private const int DISP_CHANGE_NOTUPDATED = -3;
    private const int DISP_CHANGE_BADFLAGS = -4;
    private const int DISP_CHANGE_BADPARAM = -5;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;

        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;

        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(
        string? lpszDeviceName,
        ref DEVMODE lpDevMode,
        IntPtr hwnd,
        int dwflags,
        IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(
        string? lpszDeviceName,
        IntPtr lpDevMode,
        IntPtr hwnd,
        int dwflags,
        IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(
        string? lpszDeviceName,
        int iModeNum,
        ref DEVMODE lpDevMode);

    private static DEVMODE NewDevMode()
    {
        var dm = new DEVMODE();
        dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
        return dm;
    }

    /// <summary>Returns the resolution/refresh the system is normally set to (registry default, mode -1).</summary>
    public static DisplayMode GetCurrentSettings()
    {
        var dm = NewDevMode();
        EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref dm);
        return new DisplayMode(dm.dmPelsWidth, dm.dmPelsHeight, dm.dmDisplayFrequency, dm.dmBitsPerPel);
    }

    /// <summary>Enumerates every mode the active adapter/monitor reports as supported.</summary>
    public static List<DisplayMode> EnumerateModes()
    {
        var modes = new List<DisplayMode>();
        var dm = NewDevMode();
        int i = 0;
        while (EnumDisplaySettings(null, i, ref dm))
        {
            modes.Add(new DisplayMode(dm.dmPelsWidth, dm.dmPelsHeight, dm.dmDisplayFrequency, dm.dmBitsPerPel));
            i++;
        }
        return modes;
    }

    /// <summary>
    /// Applies a temporary resolution change (not written to the registry, no dialog).
    /// If refreshRate is 0, keeps the monitor's current refresh rate.
    /// </summary>
    public static ApplyResult Apply(int width, int height, int refreshRate = 0)
    {
        var current = NewDevMode();
        EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref current);

        int effectiveRefresh = refreshRate > 0 ? refreshRate : current.dmDisplayFrequency;
        int effectiveBpp = current.dmBitsPerPel > 0 ? current.dmBitsPerPel : 32;

        // Enumerate once; the list is identical for both checks below.
        var modes = EnumerateModes();

        // Verify the exact mode exists before attempting it; some GPUs reject
        // arbitrary combinations even when width/height individually are valid.
        bool exists = modes.Any(m =>
            m.Width == width && m.Height == height &&
            (refreshRate <= 0 || m.RefreshRate == effectiveRefresh));

        if (!exists)
        {
            // Fall back to any refresh rate at that resolution.
            var candidate = modes.FirstOrDefault(m => m.Width == width && m.Height == height);
            if (candidate is null)
                return ApplyResult.NoModeFound;
            effectiveRefresh = candidate.RefreshRate;
        }

        var dm = NewDevMode();
        dm.dmPelsWidth = width;
        dm.dmPelsHeight = height;
        dm.dmDisplayFrequency = effectiveRefresh;
        dm.dmBitsPerPel = effectiveBpp;
        dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY | DM_BITSPERPEL;

        int result = ChangeDisplaySettingsEx(null, ref dm, IntPtr.Zero, CDS_FULLSCREEN, IntPtr.Zero);
        return MapResult(result);
    }

    /// <summary>Reverts to the user's normal desktop resolution (registry default).</summary>
    public static ApplyResult Revert()
    {
        int result = ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
        return MapResult(result);
    }

    private static ApplyResult MapResult(int result) => result switch
    {
        DISP_CHANGE_SUCCESSFUL => ApplyResult.Success,
        DISP_CHANGE_RESTART => ApplyResult.RestartRequired,
        DISP_CHANGE_BADMODE => ApplyResult.BadMode,
        DISP_CHANGE_BADFLAGS => ApplyResult.BadFlags,
        _ => ApplyResult.Failed
    };
}
