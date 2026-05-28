using System.Text.Json;
using Microsoft.Web.WebView2.Wpf;

namespace WallpaperPlatform;

/// <summary>
/// Fires named scene events to the active wallpaper on C#-controlled schedules.
/// Each EventDef specifies a random interval window; the bridge picks a new random
/// delay after every fire, so events never feel metronomic.
/// </summary>
public sealed class WallpaperEventBridge : IDisposable
{
    public sealed record EventDef(
        string        Name,
        TimeSpan      MinInterval,
        TimeSpan      MaxInterval,
        Func<object?>? DataFactory = null);

    private readonly WebView2                    _webView;
    private readonly Random                      _rng    = new();
    private readonly PeriodicTimer               _ticker;
    private readonly CancellationTokenSource     _cts    = new();
    private readonly List<(EventDef Def, long NextMs)> _schedule = new();

    public WallpaperEventBridge(WebView2 webView, IEnumerable<EventDef> events)
    {
        _webView = webView;
        _ticker  = new PeriodicTimer(TimeSpan.FromSeconds(1));

        long nowMs = Environment.TickCount64;
        foreach (var def in events)
            _schedule.Add((def, nowMs + RandomDelayMs(def)));
    }

    public void Start() => _ = RunAsync(_cts.Token);

    /// <summary>Fire an event immediately from C# (manual trigger / testing).</summary>
    public void PostEvent(string name, object? data = null)
    {
        var payload = JsonSerializer.Serialize(new { type = "event", name, data });
        _webView.Dispatcher.Invoke(() =>
            _webView.CoreWebView2?.PostWebMessageAsString(payload));
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (await _ticker.WaitForNextTickAsync(ct))
        {
            long nowMs = Environment.TickCount64;
            for (int i = 0; i < _schedule.Count; i++)
            {
                var (def, nextMs) = _schedule[i];
                if (nowMs < nextMs) continue;

                PostEvent(def.Name, def.DataFactory?.Invoke());
                _schedule[i] = (def, nowMs + RandomDelayMs(def));
            }
        }
    }

    private long RandomDelayMs(EventDef def)
    {
        var span = def.MaxInterval - def.MinInterval;
        return (long)(def.MinInterval.TotalMilliseconds + _rng.NextDouble() * span.TotalMilliseconds);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _ticker.Dispose();
    }
}
