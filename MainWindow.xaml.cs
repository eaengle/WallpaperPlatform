using System.IO;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;

namespace WallpaperPlatform;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // SystemParameters gives WPF logical units (DPI-aware); Screen.Bounds is physical pixels
        Left   = 0;
        Top    = 0;
        Width  = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;

        // Pass physical pixel dimensions for Win32 SetWindowPos
        var screen = System.Windows.Forms.Screen.PrimaryScreen!;
        bool attached = DesktopHelper.AttachToDesktop(hwnd, screen.Bounds.Width, screen.Bounds.Height);

        if (!attached)
            System.Windows.MessageBox.Show(
                "Could not attach to desktop WorkerW layer.\n" +
                "See %TEMP%\\WallpaperPlatform_diag.txt for details.",
                "WallpaperPlatform", MessageBoxButton.OK, MessageBoxImage.Warning);

        await InitWebViewAsync();
        LoadWallpaper("default");
    }

    private async Task InitWebViewAsync()
    {
        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WallpaperPlatform", "WebView2Cache");

        var env = await CoreWebView2Environment.CreateAsync(null, cacheDir);
        await WebView.EnsureCoreWebView2Async(env);

        WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
    }

    public void LoadWallpaper(string name)
    {
        var path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "wallpapers", name, "index.html");

        if (File.Exists(path))
            WebView.CoreWebView2.Navigate("file:///" + path.Replace('\\', '/'));
    }
}
