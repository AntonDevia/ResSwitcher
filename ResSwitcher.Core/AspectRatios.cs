namespace ResSwitcher.Core;

public record ResolutionPreset(int Width, int Height)
{
    public string Label => $"{Width} x {Height}";
}

public static class AspectRatios
{
    /// <summary>
    /// Fallback presets used only if the monitor/GPU enumeration comes back
    /// empty (should not normally happen). Prefer GetAvailablePresets, which
    /// only offers modes the display actually supports -- hardcoded values
    /// like 1440x810 are not guaranteed to exist on every monitor.
    /// </summary>
    public static readonly Dictionary<string, List<ResolutionPreset>> Presets = new()
    {
        ["4:3"] = new()
        {
            new ResolutionPreset(1024, 768),
            new ResolutionPreset(1280, 960),
            new ResolutionPreset(1440, 1080),
        },
        ["16:9"] = new()
        {
            new ResolutionPreset(1280, 720),
            new ResolutionPreset(1600, 900),
            new ResolutionPreset(1920, 1080),
        },
        ["16:10"] = new()
        {
            new ResolutionPreset(1280, 800),
            new ResolutionPreset(1440, 900),
            new ResolutionPreset(1920, 1200),
        },
    };

    /// <summary>
    /// Checks whether width/height match a known aspect ratio within a small
    /// tolerance (handles rounding, e.g. 1440x810 is exactly 16:9 but a user
    /// might type 1440x811).
    /// </summary>
    public static bool MatchesRatio(int width, int height, string ratioLabel, double tolerance = 0.01)
    {
        if (width <= 0 || height <= 0) return false;
        var (rw, rh) = ratioLabel switch
        {
            "4:3" => (4.0, 3.0),
            "16:9" => (16.0, 9.0),
            "16:10" => (16.0, 10.0),
            _ => (0.0, 0.0)
        };
        if (rw == 0) return false;

        double actual = (double)width / height;
        double expected = rw / rh;
        return Math.Abs(actual - expected) <= tolerance;
    }

    public static string? DetectRatio(int width, int height)
    {
        foreach (var key in Presets.Keys)
        {
            if (MatchesRatio(width, height, key))
                return key;
        }
        return null;
    }

    /// <summary>
    /// Returns resolution presets for the given aspect ratio, restricted to
    /// modes the current display actually reports as supported. Falls back
    /// to the static Presets list only if the display enumeration is empty.
    /// </summary>
    public static List<ResolutionPreset> GetAvailablePresets(string ratioLabel)
    {
        var supported = DisplayManager.EnumerateModes()
            .Select(m => new ResolutionPreset(m.Width, m.Height))
            .Distinct()
            .Where(p => MatchesRatio(p.Width, p.Height, ratioLabel))
            .OrderBy(p => p.Width)
            .ToList();

        return supported.Count > 0 ? supported : (Presets.TryGetValue(ratioLabel, out var fallback) ? fallback : new List<ResolutionPreset>());
    }
}
