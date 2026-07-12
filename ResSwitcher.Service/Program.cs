using System.Drawing;
using System.Windows.Forms;
using ResSwitcher.Core;

namespace ResSwitcher.Service;

internal static class Program
{
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    private static void Main()
    {
        // Prevent two copies from fighting over the display mode.
        _singleInstanceMutex = new Mutex(initiallyOwned: true, "Global\\ResSwitcher_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("ResSwitcher уже запущен (см. иконку в трее).", "ResSwitcher",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            using var context = new TrayApplicationContext();
            Application.Run(context);
        }
        finally
        {
            // Release the single-instance mutex cleanly so the next launch sees a
            // free mutex rather than an abandoned-mutex exception.
            _singleInstanceMutex.ReleaseMutex();
            _singleInstanceMutex.Dispose();
        }
    }
}

internal class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly GameProcessWatcher _processWatcher;
    private readonly ForegroundWindowWatcher _foregroundWatcher;
    private readonly ResolutionStateMachine _stateMachine;
    private AppConfig _config;
    private readonly FileSystemWatcher? _configWatcher;
    private readonly System.Threading.Timer _reloadDebounce;

    public TrayApplicationContext()
    {
        _config = AppConfig.Load();

        _processWatcher = new GameProcessWatcher(() => _config.ProcessNames, () => _config.PollIntervalMs);
        _foregroundWatcher = new ForegroundWindowWatcher();
        _stateMachine = new ResolutionStateMachine(_processWatcher, _foregroundWatcher, () => _config);

        _stateMachine.StateChanged += (_, state) => UpdateTrayText(state);
        _stateMachine.Log += (_, msg) => System.Diagnostics.Debug.WriteLine($"[ResSwitcher] {msg}");
        _stateMachine.ApplyFailed += (_, msg) => ShowBalloon("ResSwitcher — не удалось сменить разрешение", msg);

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = BuildTrayText(SwitcherState.Idle)
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("ResSwitcher", null, (_, _) => { }).Enabled = false;
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Перезагрузить config.json", null, (_, _) => ReloadConfig());
        menu.Items.Add("Открыть Settings", null, (_, _) => OpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => ExitApplication());
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => OpenSettings();

        // A single WriteAllText from Settings.exe typically raises the LastWrite
        // event more than once; debounce so we reload the file once, after writes
        // settle, instead of racing a half-written file on every raw event.
        _reloadDebounce = new System.Threading.Timer(_ => ReloadConfig(), null, Timeout.Infinite, Timeout.Infinite);

        // Watch config.json so changes saved from Settings.exe apply live
        // without requiring the user to restart the background service.
        try
        {
            _configWatcher = new FileSystemWatcher(AppContext.BaseDirectory, "config.json")
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            _configWatcher.Changed += (_, _) => _reloadDebounce.Change(300, Timeout.Infinite);
        }
        catch
        {
            // Non-fatal: live reload just won't work; tray menu item still allows manual reload.
        }
    }

    private void ShowBalloon(string title, string message)
    {
        try
        {
            _trayIcon.BalloonTipTitle = title;
            _trayIcon.BalloonTipText = message;
            _trayIcon.BalloonTipIcon = ToolTipIcon.Warning;
            _trayIcon.ShowBalloonTip(6000);
        }
        catch
        {
            // Best-effort notification; never let this crash the service.
        }
    }

    private void ReloadConfig()
    {
        try
        {
            _config = AppConfig.Load();
        }
        catch
        {
            // Keep the previous in-memory config if the file is mid-write/corrupt.
        }
    }

    private void OpenSettings()
    {
        try
        {
            var settingsPath = Path.Combine(AppContext.BaseDirectory, "Settings.exe");
            if (File.Exists(settingsPath))
            {
                System.Diagnostics.Process.Start(settingsPath);
            }
            else
            {
                MessageBox.Show("Settings.exe не найден рядом с ResSwitcher.exe.", "ResSwitcher",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch
        {
            // Ignore: worst case the user has to launch Settings.exe manually.
        }
    }

    private static string BuildTrayText(SwitcherState state) => state switch
    {
        SwitcherState.Idle => "ResSwitcher — ожидание игры",
        SwitcherState.GameActive => "ResSwitcher — игра активна (целевое разрешение)",
        SwitcherState.GameBackground => "ResSwitcher — игра свёрнута (системное разрешение)",
        _ => "ResSwitcher"
    };

    private void UpdateTrayText(SwitcherState state)
    {
        // NotifyIcon.Text has a 63-char limit.
        var text = BuildTrayText(state);
        _trayIcon.Text = text.Length > 63 ? text[..63] : text;
    }

    private void ExitApplication()
    {
        // Make sure we never leave the user's monitor stuck on the game resolution.
        try { DisplayManager.Revert(); } catch { /* best effort on shutdown */ }

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _configWatcher?.Dispose();
        _reloadDebounce.Dispose();
        _stateMachine.Dispose();
        _processWatcher.Dispose();
        _foregroundWatcher.Dispose();

        Application.Exit();
    }
}
