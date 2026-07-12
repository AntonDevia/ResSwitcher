using System.Diagnostics;

namespace ResSwitcher.Core;

public class GameProcessEventArgs : EventArgs
{
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = "";
}

/// <summary>
/// Polls the process list for any of the configured game executable names.
/// Polling (rather than WMI eventing) keeps this dependency-free and robust
/// across privilege boundaries (some launchers run elevated).
/// </summary>
public class GameProcessWatcher : IDisposable
{
    private readonly System.Threading.Timer _timer;
    private readonly Func<List<string>> _getProcessNames;
    private readonly Func<int> _getPollIntervalMs;
    private int _pollIntervalMs;

    // Cache the lowercased name set, rebuilt only when the config list instance
    // changes (ReloadConfig swaps in a new List). Avoids re-projecting/hashing
    // the names on every poll tick.
    private List<string>? _cachedNamesSource;
    private HashSet<string> _cachedNames = new();

    public bool IsGameRunning { get; private set; }
    public int GamePid { get; private set; }
    public string GameProcessName { get; private set; } = "";

    public event EventHandler<GameProcessEventArgs>? GameStarted;
    public event EventHandler<GameProcessEventArgs>? GameStopped;

    public GameProcessWatcher(Func<List<string>> getProcessNames, Func<int> getPollIntervalMs)
    {
        _getProcessNames = getProcessNames;
        _getPollIntervalMs = getPollIntervalMs;
        _pollIntervalMs = Math.Max(200, getPollIntervalMs());
        _timer = new System.Threading.Timer(Poll, null, _pollIntervalMs, _pollIntervalMs);
    }

    private void Poll(object? state)
    {
        try
        {
            var names = GetNameSet();

            int foundPid = 0;
            string foundName = "";
            // Dispose every enumerated Process: each holds a native handle, and
            // leaking one per process per tick steadily grows the handle count
            // over a long-running session.
            var all = Process.GetProcesses();
            try
            {
                foreach (var proc in all)
                {
                    if (foundPid == 0)
                    {
                        try
                        {
                            if (names.Contains(proc.ProcessName.ToLowerInvariant()))
                            {
                                foundPid = proc.Id;
                                foundName = proc.ProcessName;
                            }
                        }
                        catch
                        {
                            // Process may have exited mid-enumeration; ignore.
                        }
                    }
                }
            }
            finally
            {
                foreach (var proc in all)
                    proc.Dispose();
            }

            bool found = foundPid != 0;
            if (found && !IsGameRunning)
            {
                IsGameRunning = true;
                GamePid = foundPid;
                GameProcessName = foundName;
                GameStarted?.Invoke(this, new GameProcessEventArgs { ProcessId = foundPid, ProcessName = foundName });
            }
            else if (!found && IsGameRunning)
            {
                var lastPid = GamePid;
                var lastName = GameProcessName;
                IsGameRunning = false;
                GamePid = 0;
                GameProcessName = "";
                GameStopped?.Invoke(this, new GameProcessEventArgs { ProcessId = lastPid, ProcessName = lastName });
            }

            // Pick up interval changes from config without restarting the watcher.
            int newInterval = Math.Max(200, _getPollIntervalMs());
            if (newInterval != _pollIntervalMs)
            {
                _pollIntervalMs = newInterval;
                _timer.Change(_pollIntervalMs, _pollIntervalMs);
            }
        }
        catch
        {
            // Never let a polling tick crash the background service.
        }
    }

    private HashSet<string> GetNameSet()
    {
        var source = _getProcessNames();
        if (!ReferenceEquals(source, _cachedNamesSource))
        {
            _cachedNamesSource = source;
            _cachedNames = source
                .Select(n => Path.GetFileNameWithoutExtension(n).ToLowerInvariant())
                .ToHashSet();
        }
        return _cachedNames;
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
