namespace ResSwitcher.Core;

public enum SwitcherState
{
    Idle,
    GameActive,
    GameBackground
}

/// <summary>
/// Drives resolution changes from two signals: whether the configured game
/// process is running, and which process currently owns the foreground window.
///
///   IDLE            game not running, system resolution
///   GAME_ACTIVE      game running AND focused -> target resolution applied
///   GAME_BACKGROUND  game running but NOT focused -> reverted to system resolution
///
/// Transitions are debounced so rapid alt-tab flicker doesn't cause a storm
/// of ChangeDisplaySettingsEx calls.
/// </summary>
public class ResolutionStateMachine : IDisposable
{
    private readonly GameProcessWatcher _processWatcher;
    private readonly ForegroundWindowWatcher _foregroundWatcher;
    private readonly Func<AppConfig> _getConfig;
    private readonly object _lock = new();

    private readonly System.Threading.Timer _debounceTimer;
    private const int DebounceMs = 250;

    public SwitcherState State { get; private set; } = SwitcherState.Idle;

    public event EventHandler<SwitcherState>? StateChanged;
    public event EventHandler<string>? Log;
    public event EventHandler<string>? ApplyFailed;

    public ResolutionStateMachine(
        GameProcessWatcher processWatcher,
        ForegroundWindowWatcher foregroundWatcher,
        Func<AppConfig> getConfig)
    {
        _processWatcher = processWatcher;
        _foregroundWatcher = foregroundWatcher;
        _getConfig = getConfig;

        // One reusable debounce timer, rescheduled via Change() on each signal --
        // an alt-tab storm just keeps pushing the deadline out rather than
        // allocating (and disposing) a fresh Timer per event.
        _debounceTimer = new System.Threading.Timer(_ => Evaluate(), null, Timeout.Infinite, Timeout.Infinite);

        _processWatcher.GameStarted += (_, e) => OnSignal();
        _processWatcher.GameStopped += (_, e) => OnGameStopped();
        _foregroundWatcher.ForegroundChanged += (_, e) => OnSignal();
    }

    private void OnSignal()
    {
        lock (_lock)
        {
            _debounceTimer.Change(DebounceMs, Timeout.Infinite);
        }
    }

    private void OnGameStopped()
    {
        lock (_lock)
        {
            // Cancel any pending debounced Evaluate; the game is gone.
            _debounceTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        // Always revert immediately on process exit, no debounce -- we want
        // the desktop back to normal as fast as possible.
        ForceIdle();
    }

    private void Evaluate()
    {
        lock (_lock)
        {
            if (!_processWatcher.IsGameRunning)
            {
                ForceIdle();
                return;
            }

            int fgPid = ForegroundWindowWatcher.GetForegroundProcessId();
            bool gameFocused = fgPid == _processWatcher.GamePid;

            if (gameFocused && State != SwitcherState.GameActive)
            {
                ApplyTarget();
            }
            else if (!gameFocused && State != SwitcherState.GameBackground)
            {
                var cfg = _getConfig();
                if (cfg.RevertOnFocusLoss)
                {
                    RevertToSystem(SwitcherState.GameBackground);
                }
            }
        }
    }

    private void ApplyTarget()
    {
        var cfg = _getConfig();
        var result = DisplayManager.Apply(cfg.TargetWidth, cfg.TargetHeight, cfg.RefreshRate);
        Log?.Invoke(this, $"Apply {cfg.TargetWidth}x{cfg.TargetHeight} -> {result}");

        if (result != ApplyResult.Success && result != ApplyResult.RestartRequired)
        {
            ApplyFailed?.Invoke(this, $"Не удалось применить {cfg.TargetWidth}x{cfg.TargetHeight} ({result}) — монитор не поддерживает этот режим.");
        }
        else if (cfg.DoubleF11Fix)
        {
            // Optional user-enabled workaround: some engines render one
            // stale/cropped frame after regaining focus post resolution
            // change. Toggling fullscreen off/on via F11 (same as a manual
            // double press) forces a viewport resync. Delivery mode is
            // user-toggleable (Global/SendInput vs DirectToWindow/PostMessage)
            // since which one an engine reacts to is engine-specific. The
            // delay is also user-tunable: too short and F11 lands mid-transition
            // (before the game even renders its first cropped frame), making
            // things worse instead of better.
            int delay = Math.Max(0, cfg.DoubleF11DelayMs);
            var mode = cfg.DoubleF11Mode;
            _ = Task.Delay(delay).ContinueWith(_ => KeySender.SendDoubleF11Async(mode));
        }

        SetState(SwitcherState.GameActive);
    }

    private void RevertToSystem(SwitcherState next)
    {
        var result = DisplayManager.Revert();
        Log?.Invoke(this, $"Revert -> {result}");
        SetState(next);
    }

    private void ForceIdle()
    {
        if (State != SwitcherState.Idle)
        {
            RevertToSystem(SwitcherState.Idle);
        }
    }

    private void SetState(SwitcherState state)
    {
        if (State == state) return;
        State = state;
        StateChanged?.Invoke(this, state);
    }

    public void Dispose()
    {
        _debounceTimer.Dispose();
    }
}
