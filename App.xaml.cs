using System.Windows;

namespace WallpaperPlatform;

public partial class App : System.Windows.Application
{
    private SystemTrayHelper? _tray;
    private MainWindow?       _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _tray = new SystemTrayHelper();
        _tray.StartRequested += StartWallpaper;
        _tray.StopRequested  += StopWallpaper;
        _tray.ExitRequested  += Shutdown;
    }

    private void StartWallpaper()
    {
        if (_mainWindow != null) return;
        _mainWindow = new MainWindow();
        _mainWindow.Show();
        _tray?.SetRunMode(_mainWindow);
    }

    private void StopWallpaper()
    {
        if (_mainWindow == null) return;
        _mainWindow.Close();
        _mainWindow = null;
        _tray?.SetWaitMode();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}
