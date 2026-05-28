using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;

namespace WallpaperPlatform;

public partial class MainWindow : Window
{
    private WeatherBridge?       _weather;
    private WallpaperEventBridge? _events;

    public MainWindow()
    {
        InitializeComponent();

        // SystemParameters gives WPF logical units (DPI-aware); Screen.Bounds is physical pixels
        Left   = 0;
        Top    = 0;
        Width  = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;

        Loaded += OnLoaded;
        Closed += (_, _) => { _weather?.Dispose(); _events?.Dispose(); };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;

        var screen   = System.Windows.Forms.Screen.PrimaryScreen!;
        bool attached = DesktopHelper.AttachToDesktop(hwnd, screen.Bounds.Width, screen.Bounds.Height);

        if (!attached)
            System.Windows.MessageBox.Show(
                "Could not attach to desktop WorkerW layer.\n" +
                "See %TEMP%\\WallpaperPlatform_diag.txt for details.",
                "WallpaperPlatform", MessageBoxButton.OK, MessageBoxImage.Warning);

        await InitWebViewAsync();
        LoadWallpaper("cabin-snow");
    }

    private async Task InitWebViewAsync()
    {
        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WallpaperPlatform", "WebView2Cache");

        var env = await CoreWebView2Environment.CreateAsync(null, cacheDir);
        await WebView.EnsureCoreWebView2Async(env);

        WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        WebView.CoreWebView2.Settings.AreDevToolsEnabled            = false;

        // Start weather bridge once the wallpaper page has loaded.
        // Re-fires on each LoadWallpaper call so the bridge stays aligned with
        // whichever scene is active.
        WebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess) return;

        _weather?.Dispose();
        _weather = new WeatherBridge(WebView);
        _weather.Start();

        _events?.Dispose();
        _events = null;
        if (CurrentWallpaperEvents.Length > 0)
        {
            _events = new WallpaperEventBridge(WebView,
                CurrentWallpaperEvents.Select(e => new WallpaperEventBridge.EventDef(
                    e.Name,
                    TimeSpan.FromSeconds(e.MinSeconds),
                    TimeSpan.FromSeconds(e.MaxSeconds)
                ))
            );
            _events.Start();
        }
    }

    public void FireEvent(string name) => _events?.PostEvent(name);

    public string             CurrentWallpaper       { get; private set; } = "cabin-snow";
    public WallpaperEventDef[] CurrentWallpaperEvents { get; private set; } = [];

    public void LoadWallpaper(string name)
    {
        var dir  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wallpapers", name);
        var path = Path.Combine(dir, "index.html");
        if (!File.Exists(path)) return;

        CurrentWallpaper       = name;
        CurrentWallpaperEvents = LoadManifestEvents(dir);
        WebView.CoreWebView2.Navigate("file:///" + path.Replace('\\', '/'));
    }

    private static WallpaperEventDef[] LoadManifestEvents(string wallpaperDir)
    {
        var manifestPath = Path.Combine(wallpaperDir, "manifest.json");
        if (!File.Exists(manifestPath)) return [];
        try
        {
            var manifest = JsonSerializer.Deserialize<WallpaperManifest>(File.ReadAllText(manifestPath));
            return manifest?.Events ?? [];
        }
        catch { return []; }
    }
}
