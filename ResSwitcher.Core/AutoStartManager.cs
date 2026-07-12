using System.Diagnostics;
using Microsoft.Win32;

namespace ResSwitcher.Core;

/// <summary>
/// Registers/unregisters ResSwitcher to launch at logon via the
/// HKCU "Run" registry key. No admin rights required (unlike Task Scheduler
/// with elevated triggers), and survives Explorer restarts.
/// </summary>
public static class AutoStartManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ResSwitcher";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is not null;
    }

    public static void Enable(string exePath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        key?.SetValue(ValueName, $"\"{exePath}\"");
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    /// <summary>Path to the sibling ResSwitcher.exe (background service), relative to this exe's folder.</summary>
    public static string? ResolveServiceExePath()
    {
        var dir = AppContext.BaseDirectory;
        var candidate = Path.Combine(dir, "ResSwitcher.exe");
        return File.Exists(candidate) ? candidate : null;
    }
}
