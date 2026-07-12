using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResSwitcher.Core;

public class AppConfig
{
    public List<string> ProcessNames { get; set; } = new()
    {
        "stalzone.exe",
        "stalzonew.exe",
        "stalcraft.exe",
        "stalcraftw.exe"
    };

    public string AspectRatio { get; set; } = "4:3";

    public int TargetWidth { get; set; } = 1024;

    public int TargetHeight { get; set; } = 768;

    /// <summary>0 = use current monitor refresh rate automatically.</summary>
    public int RefreshRate { get; set; } = 0;

    public bool RevertOnFocusLoss { get; set; } = true;

    public int PollIntervalMs { get; set; } = 1000;

    public bool StartWithWindows { get; set; } = false;

    /// <summary>
    /// Workaround for engines that render a stale/cropped frame right after
    /// alt-tabbing back into the game following a resolution change. Sends
    /// F11 twice (toggle fullscreen off then on) to force the game to
    /// resync its viewport -- the same thing a user would do manually.
    /// </summary>
    public bool DoubleF11Fix { get; set; } = false;

    /// <summary>
    /// Delay in milliseconds between the resolution change and the first F11
    /// press, when DoubleF11Fix is enabled. Needs to be long enough for the
    /// game to actually render its first (cropped) frame in the new
    /// resolution -- pressing F11 too early can hit the game mid-transition
    /// and make things worse instead of better. Tune per-game/per-machine.
    /// </summary>
    public int DoubleF11DelayMs { get; set; } = 1500;

    /// <summary>
    /// How the F11 key press is delivered. "Global" (SendInput) simulates a
    /// real physical keystroke and goes to whatever window has OS input
    /// focus. "DirectToWindow" (PostMessage) posts WM_KEYDOWN/WM_KEYUP
    /// straight to the game's window, bypassing focus entirely. Which one
    /// actually works is engine-specific -- Java/LWJGL titles in particular
    /// are inconsistent, hence this being user-toggleable.
    /// </summary>
    public KeySendMode DoubleF11Mode { get; set; } = KeySendMode.Global;

    [JsonIgnore]
    public static string ConfigPath =>
        Path.Combine(AppContext.BaseDirectory, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                var fresh = new AppConfig();
                fresh.Save();
                return fresh;
            }

            var json = File.ReadAllText(ConfigPath);
            var cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
            return cfg ?? new AppConfig();
        }
        catch
        {
            // Corrupt or unreadable config: fall back to safe defaults instead of crashing.
            return new AppConfig();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }
}
