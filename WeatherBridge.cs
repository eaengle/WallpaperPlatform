using System.Text.Json;
using Microsoft.Web.WebView2.Wpf;

namespace WallpaperPlatform;

public sealed class WeatherBridge : IDisposable
{
    private readonly WebView2             _webView;
    private readonly PeriodicTimer        _timer;
    private readonly CancellationTokenSource _cts = new();
    private readonly Random               _rng  = new();

    public WeatherBridge(WebView2 webView)
    {
        _webView = webView;
        _timer   = new PeriodicTimer(TimeSpan.FromSeconds(10));
    }

    public void Start()
    {
        Post(); // fire immediately on load
        _ = RunAsync(_cts.Token);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (await _timer.WaitForNextTickAsync(ct))
            Post();
    }

    private void Post()
    {
        var payload = JsonSerializer.Serialize(new
        {
            type  = "weather",
            snow  = Math.Round(_rng.NextDouble() * 2.7 + 0.3, 3),  // 0.3 – 3.0
            wind  = Math.Round(_rng.NextDouble() * 1.5,       3),  // 0.0 – 1.5
            windX = Math.Round(_rng.NextDouble() * 2.0 - 1.0, 3),  // -1.0 – 1.0
        });

        _webView.Dispatcher.Invoke(() =>
            _webView.CoreWebView2?.PostWebMessageAsString(payload));
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _timer.Dispose();
    }
}
