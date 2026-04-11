using System.Threading;
using System.Windows.Threading;

namespace ProPresenterTimer;

/// <summary>Polls APIs every 500ms and refreshes local interpolation every 100ms (matches Swift).</summary>
public sealed class TimerPollService : IDisposable
{
    private readonly Func<AppSettings> _getSettings;
    private readonly Action _onDisplayChanged;

    private DispatcherTimer? _networkTimer;
    private DispatcherTimer? _displayTimer;
    private CancellationTokenSource _fetchCts = new();
    private int _fetchBusy;

    private double _centerSeconds;
    private DateTime _centerSnapshot = DateTime.MinValue;

    private double _sideSeconds;
    private DateTime _sideSnapshot = DateTime.MinValue;

    private double _audioSeconds;
    private DateTime _audioSnapshot = DateTime.MinValue;

    public string CenterTimer { get; private set; } = "00:00:00";
    public string SideTimer { get; private set; } = "00:00:00";
    public string SideAudioTimer { get; private set; } = "00:00:00";

    public TimerPollService(Func<AppSettings> getSettings, Action onDisplayChanged)
    {
        _getSettings = getSettings;
        _onDisplayChanged = onDisplayChanged;
    }

    public void Start()
    {
        StopTimers();
        _fetchCts.Cancel();
        _fetchCts.Dispose();
        _fetchCts = new CancellationTokenSource();

        _networkTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(500) };
        _networkTimer.Tick += async (_, _) => await RunFetchAsync().ConfigureAwait(true);
        _networkTimer.Start();
        _ = RunFetchAsync();

        _displayTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(100) };
        _displayTimer.Tick += (_, _) => RefreshDisplayFromLocal();
        _displayTimer.Start();
    }

    public void Stop()
    {
        StopTimers();
        _fetchCts.Cancel();
    }

    private void StopTimers()
    {
        _networkTimer?.Stop();
        _networkTimer = null;
        _displayTimer?.Stop();
        _displayTimer = null;
    }

    private void RefreshDisplayFromLocal()
    {
        var settings = _getSettings();
        var now = DateTime.UtcNow;

        if (settings.CenterScreenEnabled && _centerSnapshot != DateTime.MinValue)
        {
            var elapsed = (now - _centerSnapshot).TotalSeconds;
            var adjusted = Math.Max(0, _centerSeconds - elapsed);
            CenterTimer = TimerApi.FormatSeconds(adjusted);
        }
        else if (!settings.CenterScreenEnabled)
            CenterTimer = "00:00:00";

        if (_sideSnapshot != DateTime.MinValue)
        {
            var elapsed = (now - _sideSnapshot).TotalSeconds;
            var adjusted = Math.Max(0, _sideSeconds - elapsed);
            SideTimer = TimerApi.FormatSeconds(adjusted);
        }

        if (settings.SideAudioEnabled && _audioSnapshot != DateTime.MinValue)
        {
            var elapsed = (now - _audioSnapshot).TotalSeconds;
            var adjusted = Math.Max(0, _audioSeconds - elapsed);
            SideAudioTimer = TimerApi.FormatSeconds(adjusted);
        }
        else if (!settings.SideAudioEnabled)
            SideAudioTimer = "00:00:00";

        _onDisplayChanged();
    }

    private async Task RunFetchAsync()
    {
        if (Interlocked.CompareExchange(ref _fetchBusy, 1, 0) != 0)
            return;
        try
        {
            await RunFetchCoreAsync().ConfigureAwait(true);
        }
        finally
        {
            Interlocked.Exchange(ref _fetchBusy, 0);
        }
    }

    private async Task RunFetchCoreAsync()
    {
        var settings = _getSettings();
        var timeout = Math.Max(1, settings.TimeoutMs);
        var ct = _fetchCts.Token;

        var sideTask = TimerApi.FetchProPresenterVideoAsync(settings.SideScreenIP, timeout, ct);
        Task<string>? centerTask = null;
        if (settings.CenterScreenEnabled)
            centerTask = TimerApi.FetchCountdownAsync(settings.CenterScreenIP, settings.CenterScreenUses, timeout, ct);
        Task<string>? audioTask = null;
        if (settings.SideAudioEnabled)
            audioTask = TimerApi.FetchProPresenterAudioAsync(settings.EffectiveAudioBase, timeout, ct);

        var pending = new List<Task> { sideTask };
        if (centerTask != null) pending.Add(centerTask);
        if (audioTask != null) pending.Add(audioTask);

        try
        {
            await Task.WhenAll(pending);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var snapshot = DateTime.UtcNow;
        var side = await sideTask;
        _sideSeconds = TimerApi.SecondsFromTimeString(side);
        _sideSnapshot = snapshot;

        if (settings.CenterScreenEnabled && centerTask != null)
        {
            var c = await centerTask;
            _centerSeconds = TimerApi.SecondsFromTimeString(c);
            _centerSnapshot = snapshot;
        }
        else
            _centerSnapshot = DateTime.MinValue;

        if (settings.SideAudioEnabled && audioTask != null)
        {
            var a = await audioTask;
            _audioSeconds = TimerApi.SecondsFromTimeString(a);
            _audioSnapshot = snapshot;
        }
        else
            _audioSnapshot = DateTime.MinValue;

        RefreshDisplayFromLocal();
    }

    public void Dispose()
    {
        Stop();
        _fetchCts.Dispose();
    }
}
